using Csla;
using Csla.Configuration;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Ossendorf.Csla.PollingCommand.Client;
using Ossendorf.Csla.PollingCommand.Server;
using Shouldly;

namespace Ossendorf.Csla.PollingCommand.Tests.Client;

public class DefaultPollingCommandTests : IAsyncLifetime {
    private readonly ServiceProvider _serviceProvider;
    private readonly Captured<object[]> _capturedParameters;
    private readonly IPollingCommand _systemUnderTest;

    public DefaultPollingCommandTests() {
        var portal = A.Fake<IDataPortal<InitiateCommandExecutionCommand>>();
        _capturedParameters = A.Captured<object[]>();

        A.CallTo(() => portal.ExecuteAsync(_capturedParameters.Ignored)).Returns(Task.FromResult<InitiateCommandExecutionCommand>(default!));

        _serviceProvider = new ServiceCollection()
            .AddCsla(o => o.AddConsoleApp())
            .AddPollingCommandClient()
            .AddScoped(_ => portal)
            .BuildServiceProvider();

        _systemUnderTest = _serviceProvider.GetRequiredService<IPollingCommand>();
    }

    public ValueTask InitializeAsync() {
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync() {
        await _serviceProvider.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Execute_MustPassCommandNameAndParametersToInitiateCommandExecutionCommand() {
        await _systemUnderTest.Execute<TestCommand>("asdas", "asdas");

        _capturedParameters.Values.ShouldHaveSingleItem().ShouldBe([typeof(TestCommand).FullName!, new object[] { "asdas", "asdas" }]);
    }

    [Fact]
    public async Task MyTestMethodAsync() {
        await _systemUnderTest.Execute<TestCommand>();

        _capturedParameters.Values.ShouldHaveSingleItem().ShouldBe([typeof(TestCommand).FullName!, An<IReadOnlyList<object?>>.Ignored]);
    }
}

internal class TestCommand : CommandBase<TestCommand> {

    [Execute]
    private void Execute() {
        // Do nothing
    }
}