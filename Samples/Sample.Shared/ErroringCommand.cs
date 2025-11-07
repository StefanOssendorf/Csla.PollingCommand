using Csla;

namespace Sample.Shared;

public class ErroringCommand : CommandBase<ErroringCommand> {
    [Execute]
    private void Execute() => throw new InvalidOperationException("This is a test!");
}
