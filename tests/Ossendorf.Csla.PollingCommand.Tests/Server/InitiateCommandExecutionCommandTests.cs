using AwesomeAssertions;
using Csla;
using Csla.Configuration;
using Csla.Core;
using Csla.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ossendorf.Csla.PollingCommand.Server;
using System.Threading.Channels;

namespace Ossendorf.Csla.PollingCommand.Tests.Server;

public class InitiateCommandExecutionCommandTests {
    private readonly ServiceProvider _serviceProvider;
    private readonly Channel<QueuedCommand> _commandQueue;

    public InitiateCommandExecutionCommandTests() {
        _serviceProvider = new ServiceCollection()
            .AddCsla(o => o.AddConsoleApp())
            .AddLogging()
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

    [Test, DisplayName("When polling interval is greater than or equal to server TTL a warning is logged.")]
    public async Task Start_WhenPollingIntervalExceedsTtl_WarningIsLogged() {
        var capturingLogger = new CapturingLogger();
        var sp = new ServiceCollection()
            .AddCsla(o => o.AddConsoleApp())
            .AddPollingCommandServer(o => o.FinishedCommandTtl = TimeSpan.FromSeconds(5))
            .AddSingleton<ILogger<InitiateCommandExecutionCommand>>(capturingLogger)
            .BuildServiceProvider();

        await using var scope = sp.CreateAsyncScope();
        var serializedParameter = scope.ServiceProvider.GetRequiredService<ISerializationFormatter>().Serialize(new MobileList<object?>());
        await scope.ServiceProvider.GetRequiredService<IDataPortal<InitiateCommandExecutionCommand>>().InitiateExecution(typeof(CommandWithBusinessObjectParameters).AssemblyQualifiedName!, serializedParameter, TimeSpan.FromSeconds(10));

        capturingLogger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning);
    }

    [Test, DisplayName("When SuppressPollingIntervalTtlWarning is true no warning is logged even when polling interval exceeds TTL.")]
    public async Task Start_WhenWarningIsSuppressed_NoWarningIsLogged() {
        var capturingLogger = new CapturingLogger();
        var sp = new ServiceCollection()
            .AddCsla(o => o.AddConsoleApp())
            .AddPollingCommandServer(o => {
                o.FinishedCommandTtl = TimeSpan.FromSeconds(5);
                o.SuppressPollingIntervalTtlWarning = true;
            })
            .AddSingleton<ILogger<InitiateCommandExecutionCommand>>(capturingLogger)
            .BuildServiceProvider();

        await using var scope = sp.CreateAsyncScope();
        var serializedParameter = scope.ServiceProvider.GetRequiredService<ISerializationFormatter>().Serialize(new MobileList<object?>());
        await scope.ServiceProvider.GetRequiredService<IDataPortal<InitiateCommandExecutionCommand>>().InitiateExecution(typeof(CommandWithBusinessObjectParameters).AssemblyQualifiedName!, serializedParameter, TimeSpan.FromSeconds(10));

        capturingLogger.Entries.Should().BeEmpty();
    }

    private sealed class CapturingLogger : ILogger<InitiateCommandExecutionCommand> {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }
}