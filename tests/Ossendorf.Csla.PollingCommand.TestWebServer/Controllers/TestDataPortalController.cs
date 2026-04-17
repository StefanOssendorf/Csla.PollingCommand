using Csla;
using Microsoft.AspNetCore.Mvc;

namespace Ossendorf.Csla.PollingCommand.TestWebServer.Controllers;

[Route("api/[Controller]"), ApiController]
public class TestDataPortalController : global::Csla.Server.Hosts.HttpPortalController {
    public TestDataPortalController(ApplicationContext applicationContext) : base(applicationContext) {
    }
}