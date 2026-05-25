using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Csla;
using Csla.Configuration;
using Csla.Core;
using Csla.Serialization;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Ossendorf.Csla.PollingCommand.Server;
using System.Runtime.ExceptionServices;
using System.Security.Claims;

namespace Ossendorf.Csla.PollingCommand.Tests.Client;

public class DefaultPollingCommandTests {
    private readonly ServiceProvider _serviceProvider;
    private readonly IPollingCommand _systemUnderTest;
    private readonly ISerializationFormatter _serializationFormatter;
    private readonly ICommandStarter _commandStarter;
    private readonly IProcessingCommands _processingCommands;
    private readonly IFinishedCommands _finishedCommands;

    public DefaultPollingCommandTests() {
        _commandStarter = A.Fake<ICommandStarter>();
        _processingCommands = A.Fake<IProcessingCommands>();
        _finishedCommands = A.Fake<IFinishedCommands>();

        _serviceProvider = new ServiceCollection()
            .AddCsla(o => o.AddConsoleApp())
            .AddLogging()
            .AddPollingCommandClient(TimeSpan.FromMilliseconds(5))
            .AddScoped(_ => _commandStarter)
            .AddScoped(_ => _processingCommands)
            .AddScoped(_ => _finishedCommands)
            .BuildServiceProvider();

        _systemUnderTest = _serviceProvider.GetRequiredService<IPollingCommand>();
        _serializationFormatter = _serviceProvider.GetRequiredService<ISerializationFormatter>();
    }

    [After(Test)]
    public async Task TearDown() => await _serviceProvider.DisposeAsync();

    [Test, DisplayName($"When executing {nameof(CommandReturnsFixedString)} the result should return the command with the constant string as a result.")]
    public async Task Execute_Testcase01() {
        var capturedParameters = A.Captured<byte[]>();
        var capturedPrincipal = A.Captured<byte[]>();
        var correlationId = Guid.NewGuid();

        A.CallTo(() => _commandStarter.Start(typeof(CommandReturnsFixedString), capturedParameters.Ignored, capturedPrincipal.Ignored)).Returns(correlationId);
        A.CallTo(() => _processingCommands.IsBeingProcessed(correlationId)).ReturnsNextFromSequence(true, false);

        var commandResult = _serviceProvider.GetRequiredService<ApplicationContext>().CreateInstanceDI<CommandReturnsFixedString>();
        commandResult.FixedString = CommandReturnsFixedString.ReturnConstant;
        var processingResult = FinishedCommand.Success(correlationId, commandResult, _serializationFormatter);
        A.CallTo(() => _finishedCommands.TryTake(correlationId, out processingResult)).Returns(true);

        var result = (await _systemUnderTest.Execute<CommandReturnsFixedString>()).FixedString;
        result.Should().Be(CommandReturnsFixedString.ReturnConstant);
    }

    [Test, DisplayName("When executing a command which causes an exception the exception must be rethrown on the client side.")]
    public async Task Execute_Testcase02() {
        var correlationId = Guid.NewGuid();

        A.CallTo(() => _commandStarter.Start(typeof(EmptyCommand), An<byte[]>.Ignored, An<byte[]>.Ignored)).Returns(correlationId);
        A.CallTo(() => _processingCommands.IsBeingProcessed(correlationId)).ReturnsNextFromSequence(true, false);

        const string exceptionMessage = "This is a test exception";
        var processingResult = FinishedCommand.Fail(correlationId, ExceptionDispatchInfo.Capture(new InvalidOperationException(exceptionMessage)));
        A.CallTo(() => _finishedCommands.TryTake(correlationId, out processingResult)).Returns(true);

        var error = await FluentActions.Awaiting(_systemUnderTest.Execute<EmptyCommand>).Should().ThrowAsync<DataPortalException>();
        error.Which.BusinessException.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be(exceptionMessage);
    }

    public static IEnumerable<Func<(ClaimsPrincipal, string?, bool)>> PrincipalMustBeMaintainedCases() {
        yield return () => (new ClaimsPrincipal(new ClaimsIdentity()), null, false);

        const string userName = "Test User";
        var claims = new List<Claim> {
            new(ClaimTypes.Name, userName),
            new("TestClaim", "Krznbf")
        };
        var authenticatedPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        yield return () => (authenticatedPrincipal, userName, true);
    }

    [Test, DisplayName("When executing a command the current principal (Name: $name, IsAuthenticated: $isAuthenticated) must be transferred to the backend processing.")]
    [MethodDataSource(nameof(PrincipalMustBeMaintainedCases))]
    public async Task Execute_Testcase03(ClaimsPrincipal principal, string? name, bool isAuthenticated) {
        var capturedPrincipal = A.Captured<byte[]>();
        var correlationId = Guid.NewGuid();

        _serviceProvider.GetRequiredService<ApplicationContextAccessor>().GetContextManager().SetUser(principal);

        A.CallTo(() => _commandStarter.Start(typeof(EmptyCommand), An<byte[]>.Ignored, capturedPrincipal.Ignored)).Returns(correlationId);
        A.CallTo(() => _processingCommands.IsBeingProcessed(correlationId)).ReturnsNextFromSequence(true, false);

        var commandResult = _serviceProvider.GetRequiredService<ApplicationContext>().CreateInstanceDI<EmptyCommand>();
        var processingResult = FinishedCommand.Success(correlationId, commandResult, _serializationFormatter);
        A.CallTo(() => _finishedCommands.TryTake(correlationId, out processingResult)).Returns(true);

        _ = await _systemUnderTest.Execute<EmptyCommand>();

        var asd = _serviceProvider.GetRequiredService<ISerializationFormatter>();
        using (new AssertionScope()) {
            var transferredPrincipal = (ClaimsPrincipal)asd.Deserialize(capturedPrincipal.GetLastValue());

            transferredPrincipal.Identity.Should().NotBeNull();
            transferredPrincipal.Identity.IsAuthenticated.Should().Be(isAuthenticated);
            transferredPrincipal.Identity.Name.Should().Be(name);
            transferredPrincipal.Identity.AuthenticationType.Should().Be(principal.Identity!.AuthenticationType);
            foreach (var claim in principal.Claims) {
                transferredPrincipal.Claims.Should().Contain(c => c.Type == claim.Type && c.Value == claim.Value);
            }
        }
    }
}