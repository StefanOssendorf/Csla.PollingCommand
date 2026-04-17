using AwesomeAssertions;
using AwesomeAssertions.Execution;
using System.Net.Http.Headers;
using System.Text;
using TUnit.Core.Interfaces;

namespace Ossendorf.Csla.PollingCommand.Tests.EndToEnd;

[NotInParallel]
public class FullWorkflowTests : IAsyncInitializer {
    private IPollingCommand _pollingCommand = default!;

    [ClassDataSource<TestWebServerClient>(Shared = SharedType.PerTestSession)]
    public required TestWebServerClient Client { get; init; }

    public Task InitializeAsync() {
        _pollingCommand = Client.PollingCommand();
        return Task.CompletedTask;
    }

    [Test, DisplayName("Executing a command which only returns a primitive value without involving a user should return the expected primitive result.")]
    public async Task Testcase01() {

        var result = await _pollingCommand.Execute<SimplePrimitiveResultCommandWithoutUser>();
        result.Result.Should().NotBeNullOrWhiteSpace();
    }

    [Test, DisplayName("Executing a command which returns a business object without childs should return the business object with expected values.")]
    public async Task Testcase02() {
        var result = await _pollingCommand.Execute<ReturnRootWithoutChildsCommand>();
        using (new AssertionScope()) {
            var root = result.Result;
            root.Should().NotBeNull();
            root.Id.Should().NotBeEmpty();
            root.Today.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromSeconds(2));
        }
    }

    [Test, DisplayName("Executing a command which uses the user it should return the correct user details for verification.")]
    public async Task Testcase03() {
        using (Client.ConfigureClient(ConfigureClient)) {
            var pollingCommand = Client.PollingCommand();
            var result = await pollingCommand.Execute<ReturnUserInfoCommand>();

            using (new AssertionScope()) {
                result.IsAuthenticated.Should().BeTrue();
                result.Name.Should().Be("Testuser");
                result.Claims.Should().BeEquivalentTo(["Testuser", "Testuser"], cfg => cfg.WithoutStrictOrdering());
            }
        }

        static void ConfigureClient(HttpClient httpClient) {
            var base64 = Convert.ToBase64String(Encoding.ASCII.GetBytes("Testuser:t"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);
        }
    }

    [Test, DisplayName("Executing a command which returns a business object graph should return a functional graph.")]
    public async Task Testcase04() {
        var result = await _pollingCommand.Execute<ReturnRootWithChildsCommand>();
        using (new AssertionScope()) {
            var root = result.Result;
            root.Should().NotBeNull();
            root.Id.Should().Be("SomeId");
            root.Childs.Should().NotBeNull().And.HaveCount(5).And.AllSatisfy(c => c.Id.Should().NotBeEmpty());
        }
    }

    [Test, DisplayName("Executing a command with a bo graph as parameter the parameter should be correctly returned.")]
    public async Task Testcase05() {
        var testData = await Client.GetPortal<RootWithChilds>().CreateAsync();

        var result = await _pollingCommand.Execute<ReturnParametersAsIsCommand>(testData);

        using (new AssertionScope()) {
            var root = result.Result;

            root.Should().NotBeNull().And.NotBeSameAs(testData);
            root.Id.Should().Be(testData.Id);
            root.Childs.Should().Equal(testData.Childs, (c, c2) => c.Id == c2.Id);
        }
    }
}
