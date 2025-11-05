using Csla;

namespace Ossendorf.Csla.PollingCommand.Tests;

public class ExceptionRaisingCommand : CommandBase<ExceptionRaisingCommand> {

    public const string ExceptionMessage = "This exception is thrown during command execution";

    [Execute]
    private async Task Execute() {
        await Task.Delay(TimeSpan.FromMilliseconds(50));
        throw new InvalidOperationException(ExceptionMessage);
    }
}