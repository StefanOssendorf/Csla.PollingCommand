namespace Ossendorf.Csla.PollingCommand.Server;

internal record QueuedCommand(Type Command, byte[] SerializedParameters, byte[] Principal) {
    public Guid CorrelationId { get; } = Guid.NewGuid();
}