using Csla;
using Ossendorf.Csla.PollingCommand.Server;

namespace Ossendorf.Csla.PollingCommand.Client;

internal class DefaultPollingCommand : IPollingCommand {
    private readonly IDataPortal<InitiateCommandExecutionCommand> _initiatePortal;

    public DefaultPollingCommand(IDataPortal<InitiateCommandExecutionCommand> initiatePortal) {
        _initiatePortal = initiatePortal;
    }

    public Task<T> Execute<T>() where T : CommandBase<T> => Execute<T>(NoCommandParameters.Value);

    public async Task<T> Execute<T>(params object[] executeParameters) where T : CommandBase<T> {
        var correlationId = (await _initiatePortal.InitiateExecution(typeof(T).AssemblyQualifiedName!, [.. executeParameters])).CorrelationId;

        return default!;
    }
}