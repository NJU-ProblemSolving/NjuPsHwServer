namespace NjuCsCmsHelper.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VersionController : AppControllerBase<VersionController>
{
    public VersionController(IServiceProvider provider) : base(provider) { }

    [HttpPost]
    [AllowAnonymous]
    [Route("ServerRevision")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public string GetServerRevision()
    {
        return AppConfig.Revision;
    }
}
