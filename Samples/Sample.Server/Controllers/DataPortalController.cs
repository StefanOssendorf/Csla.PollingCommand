using Csla;
using Microsoft.AspNetCore.Mvc;

namespace Sample.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DataPortalController : Csla.Server.Hosts.HttpPortalController {
    public DataPortalController(ApplicationContext applicationContext) : base(applicationContext) {
    }

    public override Task PostAsync([FromQuery] string operation) => base.PostAsync(operation);
}
