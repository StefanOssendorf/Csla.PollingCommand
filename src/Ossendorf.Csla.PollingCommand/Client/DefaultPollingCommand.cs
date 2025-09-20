using Csla;
using Microsoft.Extensions.Options;
using Ossendorf.Csla.PollingCommand.Server;

namespace Ossendorf.Csla.PollingCommand.Client;

internal class DefaultPollingCommand : IPollingCommand {
    private readonly IDataPortal<InitiateCommandExecutionCommand> _initiatePortal;
    private readonly IDataPortal<PollStateOrResultCommand> _pollStateCommand;
    private readonly DefaultPollingOptions _options;

    public DefaultPollingCommand(IDataPortal<InitiateCommandExecutionCommand> initiatePortal, IDataPortal<PollStateOrResultCommand> pollStateCommand, IOptions<DefaultPollingOptions> options) {
        _initiatePortal = initiatePortal;
        _pollStateCommand = pollStateCommand;
        _options = options.Value;
    }

    public Task<T> Execute<T>() where T : CommandBase<T> => Execute<T>([]);
    public Task<T> Execute<T>(PollingOptions options) where T : CommandBase<T> => Execute<T>(options, []);

    public Task<T> Execute<T>(params object[] executeParameters) where T : CommandBase<T> => Execute<T>(_options.ToPollingOptions(), executeParameters);
    public async Task<T> Execute<T>(PollingOptions options, params object[] executeParameters) where T : CommandBase<T> {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(executeParameters);

        var correlationId = (await _initiatePortal.InitiateExecution(typeof(T).AssemblyQualifiedName!, [.. executeParameters])).CorrelationId;

        do {
            var result = await _pollStateCommand.PollStateOrResult(correlationId);
            if (result.IsFinished) {
                var commandResult = result.Result;
                if (commandResult.HasResult) {
                    return (T)commandResult.Result;
                }

                throw commandResult.Error;
            }else if (!result.IsRunning) {
                throw new InvalidOperationException($"Should never happen!");
            }

            await WaitPollInterval(options).ConfigureAwait(false);

        } while (true);
    }

    private async ValueTask WaitPollInterval(PollingOptions options) {
        var interval = options.Interval ?? _options.Interval;
        await Task.Delay(interval).ConfigureAwait(false);
    }
}
