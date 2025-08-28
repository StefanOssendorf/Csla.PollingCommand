using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ossendorf.Csla.PollingCommand.Server;
internal class CommandExecutionHostedService : BackgroundService {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IFinishedCommands _finishedCommands;
    private readonly IWaitingCommands _waitingCommands;

    public CommandExecutionHostedService(IServiceScopeFactory scopeFactory, IFinishedCommands finishedCommands, IWaitingCommands waitingCommands) {
        _scopeFactory = scopeFactory;
        _finishedCommands = finishedCommands;
        _waitingCommands = waitingCommands;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {

        await Task.Yield();

        await foreach (var command in _waitingCommands.ReadQueued(stoppingToken)) {
            _ = Process(command);
        }
    }

    private async Task Process(QueuedCommand command) {
        _ = command;
        await Task.Yield();

        await using var scope = _scopeFactory.CreateAsyncScope().ConfigureAwait(false);
    }
}
