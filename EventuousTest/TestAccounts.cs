using Eventuous;

namespace EventuousTest;

internal class TestAccounts : Aggregate<TestAccountsState>
{
    public void InjectAccounts(IList<TestAccount> accounts)
    {
        foreach (var insertion in accounts)
        {
            Apply(new TestAccountInserted(insertion));
        }
    }
}

internal record TestAccountsState : State<TestAccountsState>;

internal record TestAccountsId() : AggregateId("$$Singleton$$")
{
    public static TestAccountsId Instance = new();
}
