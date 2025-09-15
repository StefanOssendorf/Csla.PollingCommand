using Csla.Configuration;
using Microsoft.AspNetCore.Authentication;
using Ossendorf.Csla.PollingCommand;
using Sample.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCsla(o => o.AddAspNetCore().DataPortal(dp => dp.AddServerSideDataPortal()));
builder.Services.AddHttpLogging();
builder.Services.AddPollingCommandServer();
builder.Services.AddAuthentication(BasicAuthenticationHandler.AuthenticationScheme).AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(BasicAuthenticationHandler.AuthenticationScheme, null);

var app = builder.Build();

app.UseHttpLogging();
// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
