namespace Ossendorf.Csla.PollingCommand.Server;

internal interface IWaitingCommands {
    IAsyncEnumerable<QueuedCommand> ReadQueued(CancellationToken cancellationToken);
}
