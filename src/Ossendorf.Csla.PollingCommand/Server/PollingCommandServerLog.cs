using Microsoft.Extensions.Logging;

namespace Ossendorf.Csla.PollingCommand.Server;

internal static partial class PollingCommandServerLog {
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "The client polling interval ({PollingInterval}) is greater than or equal to the server result TTL ({FinishedCommandTtl}) for command type '{FullTypeName}'. Results may be evicted before the client polls for them.")]
    internal static partial void PollingIntervalExceedsTtl(this ILogger logger, TimeSpan pollingInterval, TimeSpan finishedCommandTtl, string fullTypeName);
}
