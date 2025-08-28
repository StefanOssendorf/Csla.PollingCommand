using Csla.Configuration;
using Csla.Serialization.Mobile;
using Ossendorf.Csla.PollingCommand.Client;

namespace Ossendorf.Csla.PollingCommand;

public static class MobileFormatterOptionsExtensions {
    public static MobileFormatterOptions AddPollingCommand(this MobileFormatterOptions options) {
        options.CustomSerializers.Add(new TypeMap<NoCommandParameters, NoCommandParametersSerializer>(NoCommandParametersSerializer.CanSerialize));
        return options;
    }
}
