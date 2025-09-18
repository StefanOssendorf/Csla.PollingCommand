using AwesomeAssertions;
using Csla;
using Csla.Configuration;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Ossendorf.Csla.PollingCommand.Client;
using Ossendorf.Csla.PollingCommand.Server;

namespace Ossendorf.Csla.PollingCommand.Tests.Client;

public class DefaultPollingCommandTests {
    private readonly ServiceProvider _serviceProvider;
    private readonly IPollingCommand _systemUnderTest;
    private readonly ICommandStarter _commandStarter;
    private readonly IProcessingCommands _processingCommands;
    private readonly IFinishedCommands _finishedCommands;

    public DefaultPollingCommandTests() {
        _commandStarter = A.Fake<ICommandStarter>();
        _processingCommands = A.Fake<IProcessingCommands>();
        _finishedCommands = A.Fake<IFinishedCommands>();

        _serviceProvider = new ServiceCollection()
            .AddCsla(o => o.AddConsoleApp())
            .AddPollingCommandClient()
            .AddScoped(_ => _commandStarter)
            .AddScoped(_ => _processingCommands)
            .AddScoped(_ => _finishedCommands)
            .BuildServiceProvider();

        _systemUnderTest = _serviceProvider.GetRequiredService<IPollingCommand>();
    }

    [After(Test)]
    public async Task TearDown() {
        await _serviceProvider.DisposeAsync();
    }

    [Test, DisplayName($"When executing {nameof(CommandReturnsFixedString)} the result should return the command with the constant string as a result.")]
    public async Task Execute_Testcase01() {
        var capturedParameters = A.Captured<IReadOnlyList<object?>>();
        var capturedPrincipal = A.Captured<byte[]>();
        var correlationId = Guid.NewGuid();

        A.CallTo(() => _commandStarter.Start(typeof(CommandReturnsFixedString), capturedParameters.Ignored, capturedPrincipal.Ignored)).Returns(correlationId);
        A.CallTo(() => _processingCommands.IsBeingProcessed(correlationId)).ReturnsNextFromSequence(true, false);

        var commandResult = _serviceProvider.GetRequiredService<ApplicationContext>().CreateInstanceDI<CommandReturnsFixedString>();
        commandResult.FixedString = CommandReturnsFixedString.ReturnConstant;
        var processingResult = FinishedCommand.Success(correlationId, commandResult);
        A.CallTo(() => _finishedCommands.TryTake(correlationId, out processingResult)).Returns(true);

        var result = (await _systemUnderTest.Execute<CommandReturnsFixedString>()).FixedString;
        result.Should().Be(CommandReturnsFixedString.ReturnConstant);
    }

    [Test, DisplayName("When executing a command which causes an exception the exception must be rethrown on the client side.")]
    public async Task Execute_Testcase02() {
        var capturedParameters = A.Captured<IReadOnlyList<object?>>();
        var capturedPrincipal = A.Captured<byte[]>();
        var correlationId = Guid.NewGuid();

        A.CallTo(() => _commandStarter.Start(typeof(EmptyCommand), capturedParameters.Ignored, capturedPrincipal.Ignored)).Returns(correlationId);
        A.CallTo(() => _processingCommands.IsBeingProcessed(correlationId)).ReturnsNextFromSequence(true, false);

        const string exceptionMessage = "This is a test exception";
        var processingResult = FinishedCommand.Fail(correlationId, new InvalidOperationException(exceptionMessage));
        A.CallTo(() => _finishedCommands.TryTake(correlationId, out processingResult)).Returns(true);

        await FluentActions.Awaiting(async () => await _systemUnderTest.Execute<EmptyCommand>()).Should().ThrowAsync<InvalidOperationException>().WithMessage(exceptionMessage);
    }
}


public class EmptyCommand : CommandBase<EmptyCommand>;