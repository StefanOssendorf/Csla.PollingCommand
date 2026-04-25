using Csla;
using Csla.Channels.Http;
using Csla.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Interfaces;

namespace Ossendorf.Csla.PollingCommand.Tests.EndToEnd;

public class TestWebServerClient : IAsyncInitializer, IAsyncDisposable {
    private ServiceProvider _clientServiceProvider = default!;
    private Action<HttpClient>? _testMethodConfigureClient;

    [ClassDataSource<TestWebServer>]
    public required TestWebServer TestServer { get; init; }

    public Task InitializeAsync() {
        _clientServiceProvider = new ServiceCollection()
             .AddCsla(o => o.AddConsoleApp()
                     .DataPortal(dpo => dpo.AddClientSideDataPortal(cdp => cdp.UseHttpProxy(ho => ho.WithDataPortalUrl("http://localhost/api/TestDataPortal").WithHttpClientFactory(_ => CreateClient()))))
             )
             .AddPollingCommandClient(TimeSpan.FromMilliseconds(500))
             .BuildServiceProvider();

        return Task.CompletedTask;
    }

    public IPollingCommand PollingCommand() => _clientServiceProvider.GetRequiredService<IPollingCommand>();

    public IDataPortal<T> GetPortal<T>() where T : global::Csla.Core.ICslaObject => _clientServiceProvider.GetRequiredService<IDataPortal<T>>();

    public async ValueTask DisposeAsync() {
        await _clientServiceProvider.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private HttpClient CreateClient() {
        var httpClient = TestServer.CreateClient();
        _testMethodConfigureClient?.Invoke(httpClient);
        return httpClient;
    }

    public IDisposable ConfigureClient(Action<HttpClient> configureClient) {
        _testMethodConfigureClient = configureClient;
        return new ResetHttpClient(this);
    }

    private class ResetHttpClient : IDisposable {
        private readonly TestWebServerClient _testWebServerClient;

        public ResetHttpClient(TestWebServerClient testWebServerClient) {
            _testWebServerClient = testWebServerClient;
        }

        public void Dispose() {
            _testWebServerClient._testMethodConfigureClient = null;
            GC.SuppressFinalize(this);
        }
    }
}