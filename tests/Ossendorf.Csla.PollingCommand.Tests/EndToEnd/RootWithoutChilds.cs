using Csla;

namespace Ossendorf.Csla.PollingCommand.Tests.EndToEnd;

[CslaImplementProperties]
public partial class RootWithoutChilds : BusinessBase<RootWithoutChilds> {
    public partial Guid Id { get; private set; }
    public partial DateTimeOffset Today { get; private set; }

    [Create]
    private void Create() {
        Id = Guid.NewGuid();
        Today = DateTimeOffset.Now;
    }
}
