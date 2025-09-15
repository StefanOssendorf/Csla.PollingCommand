namespace Ossendorf.Csla.PollingCommand.Server;

internal record QueuedCommand(Type Command, IReadOnlyList<object?> Parameters, byte[] Principal) {
    public Guid CorrelationId { get; } = Guid.NewGuid();
}