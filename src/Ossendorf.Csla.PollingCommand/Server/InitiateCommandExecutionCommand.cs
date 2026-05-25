using Csla;
using Csla.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ossendorf.Csla.PollingCommand.Server;

[CslaImplementProperties]
internal partial class InitiateCommandExecutionCommand : CommandBase<InitiateCommandExecutionCommand> {

    public partial Guid CorrelationId {
        get; private set;
    }

    [Execute]
    private async Task InitiateExecution(string fullTypeName, byte[] serializedParameters, TimeSpan pollingInterval, [Inject] ICommandStarter commandStarter, [Inject] IOptions<PollingCommandServerOptions> serverOptions, [Inject] ILogger<InitiateCommandExecutionCommand> logger) {
        var options = serverOptions.Value;
        if (!options.SuppressPollingIntervalTtlWarning && pollingInterval >= options.FinishedCommandTtl) {
            logger.PollingIntervalExceedsTtl(pollingInterval, options.FinishedCommandTtl, fullTypeName);
        }

        var type = Type.GetType(fullTypeName) ?? throw new InvalidOperationException($"Type '{fullTypeName}' could not be loaded. Please make sure the assembly is referenced and available.");

        var serializer = ApplicationContext.GetRequiredService<ISerializationFormatter>();
        CorrelationId = await commandStarter.Start(type, serializedParameters, serializer.Serialize(ApplicationContext.User)).ConfigureAwait(false);
    }
}