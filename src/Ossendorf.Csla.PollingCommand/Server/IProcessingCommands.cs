namespace Ossendorf.Csla.PollingCommand.Server;

internal interface IProcessingCommands {
    bool IsBeingProcessed(Guid correlationId);
}
