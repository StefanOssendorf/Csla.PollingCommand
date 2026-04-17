using Microsoft.AspNetCore.Mvc.Testing;
using Ossendorf.Csla.PollingCommand.TestWebServer.Controllers;
using TUnit.Core.Interfaces;

namespace Ossendorf.Csla.PollingCommand.Tests.EndToEnd;

public class TestWebServer : WebApplicationFactory<TestDataPortalController>, IAsyncInitializer {
    public Task InitializeAsync() {
        _ = Server;

        return Task.CompletedTask;
    }
}