using AwesomeAssertions;
using Csla;
using Csla.Configuration;
using Csla.Core;
using Csla.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Ossendorf.Csla.PollingCommand.Server;
using System.Threading.Channels;

namespace Ossendorf.Csla.PollingCommand.Tests.Server;

public class InitiateCommandExecutionCommandTests {
    private readonly ServiceProvider _serviceProvider;
    private readonly Channel<QueuedCommand> _commandQueue;

    public InitiateCommandExecutionCommandTests() {
        _serviceProvider = new ServiceCollection()
            .AddCsla(o => o.AddConsoleApp())
            .AddPollingCommandServer()
            .BuildServiceProvider();

        _commandQueue = _serviceProvider.GetRequiredService<Channel<QueuedCommand>>();
    }

    [Test, DisplayName("When starting a command with business objects as parameters the business objects must be still usable after scope switch. Simulation of a asp.net core request.")]
    public async Task Start_Testcase02() {
        Guid correlationId;
        await using (var scope = _serviceProvider.CreateAsyncScope()) {
            var foo = await scope.ServiceProvider.GetRequiredService<IDataPortal<Foo>>().CreateAsync();

            var serializedParameter = scope.ServiceProvider.GetRequiredService<ISerializationFormatter>().Serialize(new MobileList<object?> { foo });

            var result = await scope.ServiceProvider.GetRequiredService<IDataPortal<InitiateCommandExecutionCommand>>().InitiateExecution(typeof(CommandWithBusinessObjectParameters).AssemblyQualifiedName!, serializedParameter, TimeSpan.FromSeconds(1));

            correlationId = result.CorrelationId;
        }

        _commandQueue.Writer.Complete();
        var commands = _serviceProvider.GetRequiredService<Commands>();
        var queuedItems = ((IWaitingCommands)commands).ReadQueued(default).ToBlockingEnumerable().ToList();

        var queuedItem = queuedItems[0];
        queuedItem.SerializedParameters.Should().NotBeNull();
    }
}