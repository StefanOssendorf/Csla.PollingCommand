using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Csla;
using Csla.Configuration;
using Csla.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Ossendorf.Csla.PollingCommand.Server;
using System.Runtime.ExceptionServices;

namespace Ossendorf.Csla.PollingCommand.Tests.Server;

public class CommandExecutionProcessorTests {

    private readonly ServiceProvider _serviceProvider;
    private readonly ICommandExecutionProcessor _systemUnderTest;
    private readonly Commands _commands;
    private ICommandStarter SutCommandStarter => _commands;
    private IFinishedCommands SutFinishedCommands => _commands;

    public CommandExecutionProcessorTests() {
        _serviceProvider = new ServiceCollection()
            .AddCsla(o => o.AddConsoleApp())
            .AddPollingCommandServer()
            .BuildServiceProvider();

        _systemUnderTest = _serviceProvider.GetRequiredService<ICommandExecutionProcessor>();
        _commands = _serviceProvider.GetRequiredService<Commands>();
    }

    [After(Test)]
    public async Task TearDown() => await _serviceProvider.DisposeAsync();

    [Test, DisplayName("When an executing command throws an exception the thrown exception must be returned to the client.")]
    public async Task Execute_Testcase01() {
        using var cts = new CancellationTokenSource();
        var serializer = _serviceProvider.GetRequiredService<ISerializationFormatter>();
        var applicationContext = _serviceProvider.GetRequiredService<ApplicationContext>();
        var correlationId = await SutCommandStarter.Start(typeof(ExceptionRaisingCommand), [], serializer.Serialize(applicationContext.User));

        var processorTask = _systemUnderTest.Process(cts.Token);

        using var cts2 = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        FinishedCommand? result = null;
        while (!SutFinishedCommands.TryTake(correlationId, out result)) {
            await Task.Delay(TimeSpan.FromMilliseconds(5), cts.Token);
        }

        using (new AssertionScope()) {
            result.Should().NotBeNull();
            result.Error.Should().BeOfType<ExceptionDispatchInfo>().Which.SourceException.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be(ExceptionRaisingCommand.ExceptionMessage);
        }

        cts.Cancel();
        try {
            await processorTask;
        } catch (OperationCanceledException) {
        }
    }
}