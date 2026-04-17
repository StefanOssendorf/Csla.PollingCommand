using Csla;

namespace Sample.Shared;

public class Foos : BusinessListBase<Foos, Foo> {
    [CreateChild]
    private async Task Create(int number, [Inject] IChildDataPortal<Foo> fooPortal) {
        using (SuppressListChangedEvents) {
            for (int i = 0; i < number; i++) {
                Add(await fooPortal.CreateChildAsync(number - 1));
            }
        }
    }
}