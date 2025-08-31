namespace Ossendorf.Csla.PollingCommand.Server;

internal record QueuedCommand(Type Command, object?[] Parameters) {
    public Guid CorrelationId { get; } = Guid.NewGuid();
}
