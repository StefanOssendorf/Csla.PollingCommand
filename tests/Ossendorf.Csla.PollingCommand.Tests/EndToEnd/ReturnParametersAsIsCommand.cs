using Csla;

namespace Ossendorf.Csla.PollingCommand.Tests.EndToEnd;

[CslaImplementProperties]
public partial class ReturnParametersAsIsCommand : CommandBase<ReturnParametersAsIsCommand> {
    public partial RootWithChilds Result { get; private set; }

    [Execute]
    private void Execute(RootWithChilds data) => Result = data;
}