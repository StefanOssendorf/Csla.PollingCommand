using System.Security.Principal;

namespace Ossendorf.Csla.PollingCommand.Server;
internal interface ICommandStarter {
    ValueTask<Guid> Start(Type commandType, IReadOnlyCollection<object?> parameters, byte[] principal);
}