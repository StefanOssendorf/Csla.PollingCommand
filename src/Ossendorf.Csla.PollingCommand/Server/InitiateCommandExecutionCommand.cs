using Csla;
using Csla.Serialization;

namespace Ossendorf.Csla.PollingCommand.Server;

[CslaImplementProperties]
internal partial class InitiateCommandExecutionCommand : CommandBase<InitiateCommandExecutionCommand> {

    public partial Guid CorrelationId {
        get; private set;
    }

    [Execute]
    private async Task InitiateExecution(string fullTypeName, byte[] serializedParameters, [Inject] ICommandStarter commandStarter) {
        var type = Type.GetType(fullTypeName) ?? throw new InvalidOperationException($"Type '{fullTypeName}' could not be loaded. Please make sure the assembly is referenced and available.");

        var serializer = ApplicationContext.GetRequiredService<ISerializationFormatter>();
        CorrelationId = await commandStarter.Start(type, serializedParameters, serializer.Serialize(ApplicationContext.User)).ConfigureAwait(false);
    }
}