using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Csla.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ossendorf.Csla.PollingCommand.Server;
using System.Runtime.ExceptionServices;
using System.Threading.Channels;

namespace Ossendorf.Csla.PollingCommand.Tests.Server;

public class CommandsTests {
    private readonly ServiceProvider _serviceProvider;
    private readonly Commands _systemUnderTest;
    private readonly Channel<QueuedCommand> _commandQueue;

    private ICommandStarter SutCommandStarter => _systemUnderTest;
    private IWaitingCommands SutWaitingCommands => _systemUnderTest;
    private IProcessingCommands SutProcessingCommands => _systemUnderTest;

    public CommandsTests() {
        _serviceProvider = new ServiceCollection()
            .AddCsla(o => o.AddConsoleApp())
            .AddPollingCommandServer()
            .BuildServiceProvider();

        _systemUnderTest = _serviceProvider.GetRequiredService<Commands>();
        _commandQueue = _serviceProvider.GetRequiredService<Channel<QueuedCommand>>();
    }

    [After(Test)]
    public async Task TearDown() => await _serviceProvider.DisposeAsync();

    [Test, DisplayName("When starting a command it must be written to the processing queue with the returned correlation id.")]
    public async Task Start_Testcase01() {
        var correlationId = await SutCommandStarter.Start(typeof(EmptyCommand), [], []);

        _commandQueue.Writer.Complete();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var all = _commandQueue.Reader.ReadAllAsync(cts.Token).ToBlockingEnumerable().ToList();

        using (new AssertionScope()) {
            all.Should().ContainSingle().Which.Should().Satisfy<QueuedCommand>(cmd => {
                cmd.CorrelationId.Should().Be(correlationId);
                cmd.Command.Should().Be<EmptyCommand>();
                cmd.SerializedParameters.Should().NotBeNull().And.BeEmpty();
                cmd.Principal.Should().BeEmpty();
            });
        }
    }

    [Test, DisplayName("When reading the processing queue the previously added item must be returned.")]
    public async Task ReadQueued_Testcase01() {
        var queuedItem = new QueuedCommand(typeof(EmptyCommand), [], []);
        await _commandQueue.Writer.WriteAsync(queuedItem);
        _commandQueue.Writer.Complete();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var readItems = SutWaitingCommands.ReadQueued(cts.Token).ToBlockingEnumerable().ToList();

        readItems.Should().ContainSingle().Which.Should().Be(queuedItem);
    }

    [Test, DisplayName("When checking for a correclation id which is currently not being processed it must return false.")]
    public void IsBeingProcessed_Testcase01() => SutProcessingCommands.IsBeingProcessed(Guid.NewGuid()).Should().BeFalse();

    [Test, DisplayName("When checking for a correclation id which is currently being processed it must return true.")]
    public async Task IsBeingProcessed_Testcase02() {
        var correcltionId = await SutCommandStarter.Start(typeof(EmptyCommand), [], []);

        SutProcessingCommands.IsBeingProcessed(correcltionId).Should().BeTrue();
    }

    [Test, DisplayName("A finished command that is not polled within the TTL must be automatically evicted.")]
    public async Task TryTake_EvictsExpiredEntry() {
        await using var sp = new ServiceCollection()
            .AddCsla(o => o.AddConsoleApp())
            .AddPollingCommandServer(o => o.FinishedCommandTtl = TimeSpan.FromMilliseconds(50))
            .BuildServiceProvider();

        var commands = sp.GetRequiredService<Commands>();
        var finishCommands = (IFinishCommands)commands;
        var finishedCommands = (IFinishedCommands)commands;

        var finished = FinishedCommand.Fail(Guid.NewGuid(), ExceptionDispatchInfo.Capture(new Exception("eviction test")));
        finishCommands.Finish(finished);

        await Task.Delay(200);

        finishedCommands.TryTake(finished.CorrelationId, out _).Should().BeFalse();
    }
}