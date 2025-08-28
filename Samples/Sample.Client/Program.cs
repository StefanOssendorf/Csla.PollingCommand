using Csla;
using Csla.Channels.Http;
using Csla.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ossendorf.Csla.PollingCommand;
using Sample.Shared;

var services = new ServiceCollection()
    .AddHttpClient()
    .AddCsla(
        o => o.AddConsoleApp().DataPortal(
            dp => dp.AddClientSideDataPortal(
                cdp => cdp.UseHttpProxy(hp => hp.WithDataPortalUrl("https://localhost:7168/api/DataPortal").WithTimeout(TimeSpan.FromMinutes(1)))
            )
        )
    )
    .AddPollingCommandClient();

using var sp = services.BuildServiceProvider();

var foo = await sp.GetRequiredService<IDataPortal<Foo>>().CreateAsync();
Console.WriteLine(foo.Random);

var pollingCommand = sp.GetRequiredService<IPollingCommand>();
var result = await pollingCommand.Execute<FooCommand>();

Console.WriteLine(result.NewId);
