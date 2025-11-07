namespace Ossendorf.Csla.PollingCommand.Server;

internal interface ICommandStarter {
    ValueTask<Guid> Start(Type commandType, byte[] serializedParameters, byte[] principal);
}