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

    /// <summary>
    /// When <see langword="true"/>, suppresses the warning logged when the client polling interval
    /// is greater than or equal to <see cref="FinishedCommandTtl"/>.
    /// Default: <see langword="false"/>.
    /// </summary>
    public bool SuppressPollingIntervalTtlWarning { get; set; }
}