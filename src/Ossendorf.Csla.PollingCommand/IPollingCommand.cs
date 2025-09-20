using Csla;
using Ossendorf.Csla.PollingCommand.Client;

namespace Ossendorf.Csla.PollingCommand;

public interface IPollingCommand {
    Task<T> Execute<T>() where T : CommandBase<T>;
    Task<T> Execute<T>(PollingOptions options) where T : CommandBase<T>;
    Task<T> Execute<T>(params object[] executeParameters) where T : CommandBase<T>;
    Task<T> Execute<T>(PollingOptions options, params object[] executeParameters) where T : CommandBase<T>;
}