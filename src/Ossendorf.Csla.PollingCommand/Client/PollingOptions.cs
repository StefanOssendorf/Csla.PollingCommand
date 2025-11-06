namespace Ossendorf.Csla.PollingCommand.Client;

/// <summary>
/// Options to configure the command execution.
/// </summary>
public class PollingOptions {
    /// <summary>
    /// Gets or sets the <see cref="Interval"/> used for polling the command completion. <br />
    /// No value means the default polling interval should be used.
    /// </summary>
    public TimeSpan? Interval { get; set; }
}
