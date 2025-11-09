using Csla;

namespace Ossendorf.Csla.PollingCommand.Tests.EndToEnd;

[CslaImplementProperties]
public partial class Child : BusinessBase<Child> {
    public partial Guid Id { get; private set; }

    [CreateChild]
    private void CreateChild() => Id = Guid.NewGuid();
}