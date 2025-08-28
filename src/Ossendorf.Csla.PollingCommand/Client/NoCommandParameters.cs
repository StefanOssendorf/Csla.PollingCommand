using Csla.Serialization;

namespace Ossendorf.Csla.PollingCommand.Client;

[AutoSerializable]
public sealed partial class NoCommandParameters {
    public static NoCommandParameters Value { get; } = new NoCommandParameters();
}