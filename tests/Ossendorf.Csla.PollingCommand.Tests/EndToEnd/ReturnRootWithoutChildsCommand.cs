using Csla;

namespace Ossendorf.Csla.PollingCommand.Tests.EndToEnd;

[CslaImplementProperties]
public partial class ReturnRootWithoutChildsCommand : CommandBase<ReturnRootWithoutChildsCommand> {

    public partial RootWithoutChilds Result { get; private set; }

    [Execute]
    private async Task Execute([Inject] IDataPortal<RootWithoutChilds> dp) => Result = await dp.CreateAsync();
}
