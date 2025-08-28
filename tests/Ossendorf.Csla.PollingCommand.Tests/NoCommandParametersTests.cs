using Csla.Configuration;
using Csla.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Ossendorf.Csla.PollingCommand.Client;

namespace Ossendorf.Csla.PollingCommand.Tests;

public class NoCommandParametersTests {
    private readonly ServiceProvider _serviceProvider;

    public NoCommandParametersTests() {
        _serviceProvider = new ServiceCollection()
            .AddCsla(o => o.Serialization(so => so.UseMobileFormatter(mfo => mfo.AddPollingCommand())))
            .BuildServiceProvider();
    }

    [Fact]
    public void MustBeSerializable() {
        var x = _serviceProvider.GetRequiredService<ISerializationFormatter>().Serialize(NoCommandParameters.Value);
    }

    [Fact]
    public void MustBeDeserializable() {
        var data = _serviceProvider.GetRequiredService<ISerializationFormatter>().Serialize(NoCommandParameters.Value);

        _ = (NoCommandParameters)_serviceProvider.GetRequiredService<ISerializationFormatter>().Deserialize(data);
    }
}
