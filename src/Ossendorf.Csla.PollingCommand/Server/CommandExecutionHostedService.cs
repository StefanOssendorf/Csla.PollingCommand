using Csla;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ossendorf.Csla.PollingCommand.Server;
internal class CommandExecutionHostedService : BackgroundService {
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

    private async ValueTask Process(QueuedCommand command) {
        _ = command;
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

        using var scope = _scopeFactory.CreateAsyncScope();
        var parameters = command.Parameters;

        var executeCommandMethod = GetGenericMethod(command.Command);
        var commandResult = await ((ValueTask<FinishedCommand>)executeCommandMethod.Invoke(this, [scope.ServiceProvider, command.CorrelationId, parameters])!).ConfigureAwait(false);

        _finishCommands.Finish(commandResult);
    }

    private System.Reflection.MethodInfo GetGenericMethod(Type type) => _executeCommandMethodInfo.Value.MakeGenericMethod(type);

    private readonly Lazy<System.Reflection.MethodInfo> _executeCommandMethodInfo = new(static () => typeof(CommandExecutionHostedService).GetMethod(nameof(ExecuteCommand), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!, LazyThreadSafetyMode.PublicationOnly);

    private static async ValueTask<FinishedCommand> ExecuteCommand<T>(IServiceProvider serviceProvider, Guid correlationId, object[] parameters) where T : CommandBase<T> {
        var dp = serviceProvider.GetRequiredService<IDataPortal<T>>();

        try {
            return FinishedCommand.Success(correlationId, await dp.ExecuteAsync(parameters));
        } catch (Exception exc) {
            return FinishedCommand.Fail(correlationId, exc);
        }
    }
}
