using Csla;

namespace Ossendorf.Csla.PollingCommand.Tests.EndToEnd;

[CslaImplementProperties]
public partial class ReturnRootWithChildsCommand : CommandBase<ReturnRootWithChildsCommand> {
    public partial RootWithChilds Result { get; private set; }

    [Execute]
    private async Task Create([Inject] IDataPortal<RootWithChilds> rootPortal) => Result = await rootPortal.CreateAsync();
}