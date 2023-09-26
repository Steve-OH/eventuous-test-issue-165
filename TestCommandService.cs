using Eventuous;

namespace EventuousTest;

internal class TestCommandService : CommandService<TestAccounts, TestAccountsState, TestAccountsId>
{
    public TestCommandService(IAggregateStore store) : base(store)
    {
        OnAny<InjectTestAccounts>(_ => TestAccountsId.Instance, (accounts, cmd) =>
        {
            accounts.InjectAccounts(cmd.Accounts);
        });
    }
}
