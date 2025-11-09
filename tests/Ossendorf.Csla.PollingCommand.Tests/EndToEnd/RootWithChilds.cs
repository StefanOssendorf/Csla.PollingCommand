using Csla;

namespace Ossendorf.Csla.PollingCommand.Tests.EndToEnd;

[CslaImplementProperties]
public partial class RootWithChilds : BusinessBase<RootWithChilds> {

    public partial string Id { get; private set; }
    public partial Childs Childs { get; private set; }

    [Create]
    private async Task Create([Inject] IChildDataPortal<Childs> childsPortal) {
        Id = "SomeId";
        Childs = await childsPortal.CreateChildAsync();
    }
}