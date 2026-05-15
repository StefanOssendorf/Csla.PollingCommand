namespace Ossendorf.Csla.PollingCommand.Server;

/// <summary>
/// Options to configure the server-side polling command infrastructure.
/// </summary>
public class PollingCommandServerOptions {
    /// <summary>
    /// How long a finished command result is retained when the client never polls for it.
    /// Default: 5 minutes.
    /// </summary>
    public TimeSpan FinishedCommandTtl { get; set; } = TimeSpan.FromMinutes(5);
}