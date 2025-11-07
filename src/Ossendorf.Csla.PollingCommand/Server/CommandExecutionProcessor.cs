using Csla;
using Csla.Core;
using Csla.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.ExceptionServices;
using System.Security.Principal;

namespace Ossendorf.Csla.PollingCommand.Server;

internal class CommandExecutionProcessor : ICommandExecutionProcessor {
    private readonly Lazy<System.Reflection.MethodInfo> _executeCommandMethodInfo = new(static () => typeof(CommandExecutionProcessor).GetMethod(nameof(ExecuteCommand), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!, LazyThreadSafetyMode.PublicationOnly);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IFinishCommands _finishCommands;
    private readonly IWaitingCommands _waitingCommands;

    public CommandExecutionProcessor(IServiceScopeFactory scopeFactory, IFinishCommands finishCommands, IWaitingCommands waitingCommands) {
        _scopeFactory = scopeFactory;
        _finishCommands = finishCommands;
        _waitingCommands = waitingCommands;
    }

    public async Task Process(CancellationToken cancellationToken) {
        await foreach (var command in _waitingCommands.ReadQueued(cancellationToken)) {
            _ = Process(command);
        }
    }

    private async Task Process(QueuedCommand command) {
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

        FinishedCommand commandResult;
        await using var scope = _scopeFactory.CreateAsyncScope();
        try {
            EnsurePrincipalOnScope(scope.ServiceProvider, command);

            var parameters = ((MobileList<object?>)scope.ServiceProvider.GetRequiredService<ISerializationFormatter>().Deserialize(command.SerializedParameters)) ?? [];

            var executeCommandMethod = GetGenericMethod(command.Command);
            commandResult = await ((ValueTask<FinishedCommand>)executeCommandMethod.Invoke(this, [scope.ServiceProvider, command.CorrelationId, parameters.ToArray()])!).ConfigureAwait(false);
        } catch (Exception e) {
            commandResult = FinishedCommand.Fail(command.CorrelationId, ExceptionDispatchInfo.Capture(e));
        }

        _finishCommands.Finish(commandResult);
    }

    private static void EnsurePrincipalOnScope(IServiceProvider sp, QueuedCommand command) {
        var deserializer = sp.GetRequiredService<ISerializationFormatter>();
        var contextManagerAccessor = sp.GetRequiredService<ApplicationContextAccessor>();
        var contextManager = contextManagerAccessor.GetContextManager();
        contextManager.SetUser((IPrincipal)deserializer.Deserialize(command.Principal));
    }

    private System.Reflection.MethodInfo GetGenericMethod(Type type) => _executeCommandMethodInfo.Value.MakeGenericMethod(type);

    private static async ValueTask<FinishedCommand> ExecuteCommand<T>(IServiceProvider serviceProvider, Guid correlationId, object[] parameters) where T : CommandBase<T> {
        var dp = serviceProvider.GetRequiredService<IDataPortal<T>>();

        try {
            return FinishedCommand.Success(correlationId, await dp.ExecuteAsync(parameters));
        } catch (DataPortalException exc) when (exc.InnerException is not null) {
            return CreateFailed(correlationId, exc.InnerException.InnerException ?? exc.InnerException);
        } catch (Exception exc) {
            return CreateFailed(correlationId, exc);
        }

        static FinishedCommand CreateFailed(Guid correlationId, Exception exc) => FinishedCommand.Fail(correlationId, ExceptionDispatchInfo.Capture(exc));
    }
}