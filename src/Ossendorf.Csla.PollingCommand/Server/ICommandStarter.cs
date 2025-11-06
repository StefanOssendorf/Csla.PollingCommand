namespace Ossendorf.Csla.PollingCommand.Server;

internal interface ICommandStarter {
    ValueTask<Guid> Start(Type commandType, IReadOnlyList<object?> parameters, byte[] principal);
}