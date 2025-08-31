using Csla;

namespace Ossendorf.Csla.PollingCommand;

public interface IPollingCommand {
    Task<T> Execute<T>() where T : CommandBase<T>;
    Task<T> Execute<T>(params object[] executeParameters) where T : CommandBase<T>;
}

public class PollingCommandOptions {
    public TimeSpan Intervall { get; set; }
}