using Csla;
using Csla.Channels.Http;
using Csla.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ossendorf.Csla.PollingCommand;
using Sample.Shared;
using System.Net.Http.Headers;
using System.Text;

var services = new ServiceCollection()
    .AddCsla(
        o => o.AddConsoleApp().DataPortal(
            dp => dp.AddClientSideDataPortal(
                cdp => cdp.UseHttpProxy(hp => hp.WithDataPortalUrl("https://localhost:7168/api/DataPortal").WithTimeout(TimeSpan.FromMinutes(1)))
            )
        )
    )
    .AddPollingCommandClient(TimeSpan.FromMilliseconds(250));

services.AddHttpClient("", cfg => {
    var base64 = Convert.ToBase64String(Encoding.ASCII.GetBytes("Test:Test"));
    cfg.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);
});

await using var sp = services.BuildServiceProvider();

await Task.Delay(TimeSpan.FromSeconds(2));

var pollingCommand = sp.GetRequiredService<IPollingCommand>();
var result = await pollingCommand.Execute<FooCommand>();

Console.WriteLine("Returned from server...");
Console.WriteLine(result.NewId.ToString() ?? "<null>");
Console.WriteLine($"Username: {result.UserName}");

await Task.Delay(TimeSpan.FromSeconds(2));

Console.WriteLine(new string('-', 15));

try {
    _ = await pollingCommand.Execute<ErroringCommand>();
} catch (Exception e) {
    Console.WriteLine(e.Message);
}

Console.WriteLine(new string('-', 15));

var foo = await sp.GetRequiredService<IDataPortal<Foo>>().CreateAsync();

var result2 = await pollingCommand.Execute<CommandWithParameter>(foo);

Console.WriteLine(result2.Result);
