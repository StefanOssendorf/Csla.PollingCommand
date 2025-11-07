using Csla;

namespace Sample.Shared;

[CslaImplementProperties]
public partial class Foo : BusinessBase<Foo> {

    public partial string Random { get; set; }

    [Create]
    private void Create() {
        Random = Guid.NewGuid().ToString();
    }
}