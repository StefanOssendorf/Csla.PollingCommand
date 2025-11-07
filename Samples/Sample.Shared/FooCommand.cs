using Csla;

namespace Sample.Shared;

[CslaImplementProperties]
public partial class FooCommand : CommandBase<FooCommand> {
    public partial Guid NewId { get; set; }

    public partial string UserName { get; set; }

    [Execute]
    private async Task Execute() {
        await Task.Delay(TimeSpan.FromSeconds(5));
        NewId = Guid.NewGuid();
        UserName = ApplicationContext.User.Identity?.Name ?? "<Unknown>";
    }
}
