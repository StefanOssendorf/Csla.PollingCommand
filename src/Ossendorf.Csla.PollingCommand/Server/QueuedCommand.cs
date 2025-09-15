namespace Ossendorf.Csla.PollingCommand.Server;

internal record QueuedCommand(Type Command, object?[] Parameters, byte[] Principal) {
    public Guid CorrelationId { get; } = Guid.NewGuid();
}