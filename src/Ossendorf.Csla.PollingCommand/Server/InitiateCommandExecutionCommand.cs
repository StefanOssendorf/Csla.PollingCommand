using Csla;
using Csla.Core;

namespace Ossendorf.Csla.PollingCommand.Server;

[CslaImplementProperties]
internal partial class InitiateCommandExecutionCommand : CommandBase<InitiateCommandExecutionCommand> {

    public partial Guid CorrelationId {
        get; private set;
    }

    [Execute]
    private async Task InitiateExecution(string fullTypeName, MobileList<object?>? parameters, [Inject] ICommandStarter commandStarter) {
        var type = Type.GetType(fullTypeName) ?? throw new InvalidOperationException($"Type '{fullTypeName}' could not be loaded. Please make sure the assembly is referenced and available.");

        CorrelationId = await commandStarter.Start(type, parameters).ConfigureAwait(false);
    }
}