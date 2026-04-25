using Csla;
using Csla.Core;
using Csla.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Principal;

namespace Ossendorf.Csla.PollingCommand.Server;

internal class CommandExecutionProcessor : ICommandExecutionProcessor {
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, Guid, object[], ValueTask<FinishedCommand>>> _executors = new();
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
        try {
            await using var scope = _scopeFactory.CreateAsyncScope();
            EnsurePrincipalOnScope(scope.ServiceProvider, command);

            var parameters = ((MobileList<object?>)scope.ServiceProvider.GetRequiredService<ISerializationFormatter>().Deserialize(command.SerializedParameters)) ?? [];

            commandResult = await GetExecutor(command.Command)(scope.ServiceProvider, command.CorrelationId, parameters.ToArray()).ConfigureAwait(false);
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

    private static Func<IServiceProvider, Guid, object[], ValueTask<FinishedCommand>> GetExecutor(Type commandType)
        => _executors.GetOrAdd(commandType, static t =>
            typeof(CommandExecutionProcessor)
                .GetMethod(nameof(ExecuteCommand), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(t)
                .CreateDelegate<Func<IServiceProvider, Guid, object[], ValueTask<FinishedCommand>>>());

    private static async ValueTask<FinishedCommand> ExecuteCommand<T>(IServiceProvider serviceProvider, Guid correlationId, object[] parameters) where T : CommandBase<T> {
        var dp = serviceProvider.GetRequiredService<IDataPortal<T>>();

        try {
            return FinishedCommand.Success(correlationId, await dp.ExecuteAsync(parameters), serviceProvider.GetRequiredService<ISerializationFormatter>());
        } catch (DataPortalException exc) when (exc.InnerException is not null) {
            return CreateFailed(correlationId, exc.InnerException.InnerException ?? exc.InnerException);
        } catch (Exception exc) {
            return CreateFailed(correlationId, exc);
        }

        static FinishedCommand CreateFailed(Guid correlationId, Exception exc) => FinishedCommand.Fail(correlationId, ExceptionDispatchInfo.Capture(exc));
    }
}
