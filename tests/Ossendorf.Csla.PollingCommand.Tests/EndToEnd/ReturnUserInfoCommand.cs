using Csla;
using Csla.Core;

namespace Ossendorf.Csla.PollingCommand.Tests.EndToEnd;

[CslaImplementProperties]
public partial class ReturnUserInfoCommand : CommandBase<ReturnUserInfoCommand> {

    public partial bool IsAuthenticated { get; private set; }
    public partial string? Name { get; private set; }
    public partial MobileList<string>? Claims { get; private set; }

    [Execute]
    private void Execute() {
        var principal = ApplicationContext.Principal;
        var user = principal.Identity!;

        IsAuthenticated = user.IsAuthenticated;
        Name = user.Name;
        Claims = [.. principal.Claims.Select(c => c.Value)];
    }
}