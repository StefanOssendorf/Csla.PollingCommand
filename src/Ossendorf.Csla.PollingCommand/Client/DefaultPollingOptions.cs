namespace Ossendorf.Csla.PollingCommand.Client;

internal class DefaultPollingOptions {
    public TimeSpan Interval { get; set; }

    public PollingOptions ToPollingOptions() => new() { Interval = Interval };
}