namespace Ossendorf.Csla.PollingCommand.Server;

internal interface ICommandExecutionProcessor {
    Task Process(CancellationToken cancellationToken);
}