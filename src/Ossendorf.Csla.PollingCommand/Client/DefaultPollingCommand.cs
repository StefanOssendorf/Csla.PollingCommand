using Csla;
using Csla.Core;
using Csla.Serialization;
using Microsoft.Extensions.Options;
using Ossendorf.Csla.PollingCommand.Server;

namespace Ossendorf.Csla.PollingCommand.Client;

internal class DefaultPollingCommand : IPollingCommand {
    private readonly IDataPortal<InitiateCommandExecutionCommand> _initiatePortal;
    private readonly IDataPortal<PollStateOrResultCommand> _pollStateCommand;
    private readonly ISerializationFormatter _serializationFormatter;
    private readonly DefaultPollingOptions _options;
    private readonly PollingOptions _defaultPollingOptions;

    public DefaultPollingCommand(IDataPortal<InitiateCommandExecutionCommand> initiatePortal, IDataPortal<PollStateOrResultCommand> pollStateCommand, IOptions<DefaultPollingOptions> options, ISerializationFormatter serializationFormatter) {
        _initiatePortal = initiatePortal;
        _pollStateCommand = pollStateCommand;
        _serializationFormatter = serializationFormatter;
        _options = options.Value;
        _defaultPollingOptions = _options.ToPollingOptions();
    }

    public Task<T> Execute<T>() where T : CommandBase<T> => Execute<T>([]);
    public Task<T> Execute<T>(PollingOptions options) where T : CommandBase<T> => Execute<T>(options, []);
    public Task<T> Execute<T>(params object[] executeParameters) where T : CommandBase<T> => Execute<T>(_defaultPollingOptions, executeParameters);
    public async Task<T> Execute<T>(PollingOptions options, params object[] executeParameters) where T : CommandBase<T> {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(executeParameters);

        var serializedParameters = _serializationFormatter.Serialize(new MobileList<object?>(executeParameters));
        var pollingInterval = options.Interval ?? _options.Interval;
        var correlationId = (await _initiatePortal.InitiateExecution(typeof(T).AssemblyQualifiedName!, serializedParameters, pollingInterval)).CorrelationId;

        do {
            var result = await _pollStateCommand.PollStateOrResult(correlationId);
            if (result.IsFinished) {
                var commandResult = result.Result;
                if (commandResult.HasResult) {
                    return (T)_serializationFormatter.Deserialize(commandResult.Result);
                }
            } else if (!result.IsRunning) {
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