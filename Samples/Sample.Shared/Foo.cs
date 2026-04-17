using Csla;

namespace Sample.Shared;

[CslaImplementProperties]
public partial class Foo : BusinessBase<Foo> {

    public partial string Random { get; set; }

    public partial Foos? Foos { get; private set; }

    [Create]
    private void Create() {
        Random = Guid.NewGuid().ToString();
    }

    [Create, CreateChild]
    private async Task CreateChild(int number, [Inject] IChildDataPortal<Foos> foosPortal) {
        Foos = await foosPortal.CreateChildAsync(number);
    }

    public override int GetHashCode() => Random.GetHashCode();
}