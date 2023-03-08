namespace NjuCsCmsHelper.Server.Controllers;

using Datas;
using Services;

public abstract class AppControllerBase<T> : ControllerBase
{
    protected IServiceProvider provider { get; init; }

    protected ILogger<T> logger => _logger.Value;
    private readonly Lazy<ILogger<T>> _logger;

    protected AppDbContext dbContext => _dbContext.Value;
    private readonly Lazy<AppDbContext> _dbContext;

    protected AutoMapper.IMapper dtoMapper => _dtoMapper.Value;
    private readonly Lazy<AutoMapper.IMapper> _dtoMapper;

    protected IAuthorizationService authorizationService => _authorizationService.Value;
    private readonly Lazy<IAuthorizationService> _authorizationService;

    protected MailingService mailingService => _mailingService.Value;
    private readonly Lazy<MailingService> _mailingService;

    protected MyAppService myAppService => _myAppService.Value;
    private readonly Lazy<MyAppService> _myAppService;

    protected SubmissionService submissionService => _submissionService.Value;
    private readonly Lazy<SubmissionService> _submissionService;

    public AppControllerBase(IServiceProvider provider)
    {
        this.provider = provider;
        _logger = new(() => provider.GetRequiredService<ILogger<T>>());
        _dbContext = new(() => provider.GetRequiredService<AppDbContext>());
        _dtoMapper = new(() => provider.GetRequiredService<AutoMapper.IMapper>());
        _authorizationService = new(() => provider.GetRequiredService<IAuthorizationService>());
        _mailingService = new(() => provider.GetRequiredService<MailingService>());
        _myAppService = new(() => provider.GetRequiredService<MyAppService>());
        _submissionService = new(() => provider.GetRequiredService<SubmissionService>());
    }
}
