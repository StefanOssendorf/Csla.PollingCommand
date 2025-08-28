using Csla.Configuration;
using Ossendorf.Csla.PollingCommand;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCsla(o => o.AddAspNetCore().DataPortal(dp => dp.AddServerSideDataPortal()));
builder.Services.AddHttpLogging();
builder.Services.AddPollingCommandServer();

var app = builder.Build();

app.UseHttpLogging();
// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
