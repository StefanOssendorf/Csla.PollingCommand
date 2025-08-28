using Csla.Serialization.Mobile;
using Ossendorf.Csla.PollingCommand.Client;

namespace Ossendorf.Csla.PollingCommand;

internal class NoCommandParametersSerializer : IMobileSerializer {
    public static bool CanSerialize(Type type) => type == typeof(NoCommandParameters);

    public object Deserialize(SerializationInfo info) => NoCommandParameters.Value;
    public void Serialize(object obj, SerializationInfo info) {
        ArgumentNullException.ThrowIfNull(obj);

        info.AddValue("marker", "");
    }
}
