using Csla;
using Csla.Core;
using Csla.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Security.Principal;

namespace Ossendorf.Csla.PollingCommand.Server;
internal class CommandExecutionHostedService : BackgroundService {
    private readonly Lazy<System.Reflection.MethodInfo> _executeCommandMethodInfo = new(static () => typeof(CommandExecutionHostedService).GetMethod(nameof(ExecuteCommand), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!, LazyThreadSafetyMode.PublicationOnly);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IFinishCommands _finishCommands;
    private readonly IWaitingCommands _waitingCommands;

    public CommandExecutionHostedService(IServiceScopeFactory scopeFactory, IFinishCommands finishCommands, IWaitingCommands waitingCommands) {
        _scopeFactory = scopeFactory;
        _finishCommands = finishCommands;
        _waitingCommands = waitingCommands;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await Task.Yield();

        try {
            await foreach (var command in _waitingCommands.ReadQueued(stoppingToken)) {
                _ = Process(command);
            }
        } catch (OperationCanceledException) {
            // stopping
        }
    }

    private async Task Process(QueuedCommand command) {
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

        FinishedCommand commandResult;
        await using var scope = _scopeFactory.CreateAsyncScope();
        try {
            EnsurePrincipalOnScope(scope.ServiceProvider, command);

            var parameters = command.Parameters;

            var executeCommandMethod = GetGenericMethod(command.Command);
             commandResult = await ((ValueTask<FinishedCommand>)executeCommandMethod.Invoke(this, [scope.ServiceProvider, command.CorrelationId, parameters])!).ConfigureAwait(false);
        } catch (Exception e) {
            commandResult = FinishedCommand.Fail(command.CorrelationId, e);
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
        } catch (Exception exc) {
            return FinishedCommand.Fail(correlationId, exc);
        }
    }
}