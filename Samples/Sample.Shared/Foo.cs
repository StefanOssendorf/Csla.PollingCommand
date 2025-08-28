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

[CslaImplementProperties]
public partial class FooCommand : CommandBase<FooCommand> {
    public partial Guid NewId { get; set; }

    [Execute]
    private async Task Execute() {
        await Task.Delay(TimeSpan.FromSeconds(5));
        NewId = Guid.NewGuid();
    }
}