using System.Diagnostics.CodeAnalysis;

namespace Ossendorf.Csla.PollingCommand.Server;

internal interface IFinishedCommands {
    bool TryTake(Guid correlationId, [NotNullWhen(true)] out FinishedCommand? result);
}
