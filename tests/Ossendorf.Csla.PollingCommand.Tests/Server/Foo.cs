using Csla;

namespace Ossendorf.Csla.PollingCommand.Tests.Server;

[CslaImplementProperties]
public partial class Foo : BusinessBase<Foo> {
    public partial string GuidAsString { get; private set; }

    [Create]
    private void Create() => GuidAsString = Guid.NewGuid().ToString();
}