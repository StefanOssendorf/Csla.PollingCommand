using Csla;

namespace Ossendorf.Csla.PollingCommand.Tests.EndToEnd;

public class Childs : BusinessListBase<Childs, Child> {
    [CreateChild]
    private async Task CreateChild([Inject] IChildDataPortal<Child> childPortal) {
        for (int i = 0; i < 5; i++) {
            Add(await childPortal.CreateChildAsync());
        }
    }
}
