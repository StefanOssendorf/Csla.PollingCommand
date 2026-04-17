using Csla;

namespace Ossendorf.Csla.PollingCommand.Tests.EndToEnd;

[CslaImplementProperties]
public partial class SimplePrimitiveResultCommandWithoutUser : CommandBase<SimplePrimitiveResultCommandWithoutUser> {

    public partial string Result { get; private set; }

    [Execute]
    private void Execute() => Result = Guid.NewGuid().ToString();
}