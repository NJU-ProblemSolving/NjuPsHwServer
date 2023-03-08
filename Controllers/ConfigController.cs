namespace NjuCsCmsHelper.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ConfigController : AppControllerBase<ConfigController>
{
    public ConfigController(IServiceProvider provider) : base(provider) { }

    [HttpGet]
    [AllowAnonymous]
    [Route("ServerRevision")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public string GetServerRevision()
    {
        return AppConfig.Revision;
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("Reviewers")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public Dictionary<int, string> GetReviewers()
    {
        return AppConfig.ReviewerName;
    }
}
