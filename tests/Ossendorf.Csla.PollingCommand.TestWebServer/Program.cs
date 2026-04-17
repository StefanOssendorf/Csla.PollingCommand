using Csla.Configuration;
using Microsoft.AspNetCore.Authentication;
using Ossendorf.Csla.PollingCommand;
using Ossendorf.Csla.PollingCommand.TestWebServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCsla(o => o.AddAspNetCore());
builder.Services.AddPollingCommandServer();
builder.Services.AddAuthentication(BasicAuthenticationHandler.AuthenticationScheme).AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(BasicAuthenticationHandler.AuthenticationScheme, null);

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
