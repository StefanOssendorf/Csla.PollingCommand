using Csla;

namespace Ossendorf.Csla.PollingCommand.Tests;

[CslaImplementProperties]
public partial class CommandWithBusinessObjectParameters : CommandBase<CommandWithBusinessObjectParameters> {

    public partial string Result { get; private set; }

    [Execute]
    private async Task Execute(Foo parameter1) => Result = parameter1.GuidAsString;
}