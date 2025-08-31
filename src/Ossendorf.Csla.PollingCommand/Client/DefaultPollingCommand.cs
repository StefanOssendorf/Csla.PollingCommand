using Csla;
using Ossendorf.Csla.PollingCommand.Server;

namespace Ossendorf.Csla.PollingCommand.Client;

internal class DefaultPollingCommand : IPollingCommand {
    private readonly IDataPortal<InitiateCommandExecutionCommand> _initiatePortal;
    private readonly IDataPortal<PollStateOrResultCommand> _pollStateCommand;

    public DefaultPollingCommand(IDataPortal<InitiateCommandExecutionCommand> initiatePortal, IDataPortal<PollStateOrResultCommand> pollStateCommand) {
        _initiatePortal = initiatePortal;
        _pollStateCommand = pollStateCommand;
    }

    public Task<T> Execute<T>() where T : CommandBase<T> => Execute<T>(NoCommandParameters.Value);

    public async Task<T> Execute<T>(params object[] executeParameters) where T : CommandBase<T> {
        var correlationId = (await _initiatePortal.InitiateExecution(typeof(T).AssemblyQualifiedName!, [.. executeParameters])).CorrelationId;

        do {
            var result = await _pollStateCommand.PollStateOrResult(correlationId);
            if (result.IsRunning) {
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                continue;
            } else if (result.IsFinished) {
                var commandResult = result.Result;
                if (commandResult.HasResult) {
                    return (T)commandResult.Result;
                }

                throw commandResult.Error;
            } else {
                throw new InvalidOperationException($"Should never happen!");
            }
        } while (true);
    }
}