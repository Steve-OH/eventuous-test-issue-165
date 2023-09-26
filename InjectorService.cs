using Eventuous;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventuousTest;

internal class InjectorService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var result = await testAccountsService.Handle(new InjectTestAccounts(testData), stoppingToken);
        if (result is ErrorResult e)
        {
            logger.LogError("{message}", e.ErrorMessage);
        }
    }

    public InjectorService(ILogger<InjectorService> logger, ICommandService<TestAccounts> testAccountsService)
    {
        this.logger = logger;
        this.testAccountsService = testAccountsService;
        testData = Enumerable.Range(0, 1000).Select(n => new TestAccount($"user{n:D4}")).ToList();
    }

    private readonly ILogger<InjectorService> logger;
    private readonly ICommandService<TestAccounts> testAccountsService;
    private readonly IList<TestAccount> testData;
}
