using Microsoft.Extensions.Hosting;

namespace Ossendorf.Csla.PollingCommand.Server;

internal class CommandExecutionHostedService : BackgroundService {
    private readonly ICommandExecutionProcessor _commandExecutionProcessor;

    public CommandExecutionHostedService(ICommandExecutionProcessor commandExecutionProcessor) {
        _commandExecutionProcessor = commandExecutionProcessor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

        try {
            await _commandExecutionProcessor.Process(stoppingToken);
        } catch (OperationCanceledException) {
            // stopping
        }
    }
}