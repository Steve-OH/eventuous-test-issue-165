using Eventuous;
using Eventuous.SqlServer;
using Eventuous.SqlServer.Subscriptions;
using Eventuous.Subscriptions;
using Eventuous.Subscriptions.Context;
using Eventuous.Subscriptions.Filters;
using Microsoft.Data.SqlClient;

namespace NoAsp;

public class Program
{
    public static string SchemaName => "TEST";

    public static async Task Main()
    {
        await using var connection = getConnection();
        connection.Open();
        connection.Close();
        TypeMap.RegisterKnownEventTypes();
        var schema = new Schema(SchemaName);
        await schema.CreateSchema(getConnection);

        IEventStore eventStore = new SqlServerStore(getConnection, new SqlServerStoreOptions(SchemaName));
        var aggregateStore = new AggregateStore(eventStore);
        var checkpointStore = new SqlServerCheckpointStore(getConnection, new SqlServerCheckpointStoreOptions
        {
            Schema = SchemaName
        });
        var testCommandService = new TestCommandService(aggregateStore);
        var pipe = new ConsumePipe();
        pipe.AddDefaultConsumer(new TestHandler());
        var testSubscription = new SqlServerAllStreamSubscription(getConnection,
            new SqlServerAllStreamSubscriptionOptions
            {
                Schema = SchemaName,
                SubscriptionId = "TestSubscription"
            }, checkpointStore, pipe);
        await testSubscription.Subscribe((subscriptionId) =>
        {
            Console.WriteLine($"{subscriptionId} started");
        }, (subscriptionId, dropReason, e) =>
        {
            var suffix = e is not null ? $" with exception {e}" : "";
            Console.WriteLine($"{subscriptionId} dropped because {dropReason}{suffix}");
        }, CancellationToken.None);

        var accounts = Enumerable.Range(0, 100000).Select(n => new TestAccount($"user{n:D4}")).ToList();
        await testCommandService.Handle(new InjectTestAccounts(accounts), CancellationToken.None);

        Console.WriteLine("Please wait for events to be handled, then press any key to exit...");
        Console.ReadKey();
    }

    private static SqlConnection getConnection() =>
        new("Data Source=MY_SERVER;Initial Catalog=MyDatabase;Integrated Security=true;Trust Server Certificate=true");
}

internal class TestCommandService : CommandService<TestAccounts, TestAccountsState, TestAccountsId>
{
    public TestCommandService(IAggregateStore store) : base(store)
    {
        On<InjectTestAccounts>().InState(ExpectedState.Any).GetId(_ => TestAccountsId.Instance).Act((accounts, cmd) =>
        {
            accounts.InjectAccounts(cmd.Accounts);
        });
    }
}

public record TestAccount(string Username);

public record InjectTestAccounts(IList<TestAccount> Accounts);

[EventType($"V1.{nameof(TestAccountInserted)}")]
public record TestAccountInserted(TestAccount Account);

public class TestAccounts : Aggregate<TestAccountsState>
{
    public void InjectAccounts(IList<TestAccount> accounts)
    {
        foreach (var insertion in accounts)
        {
            Apply(new TestAccountInserted(insertion));
        }
    }
}

public record TestAccountsState : State<TestAccountsState>;

public record TestAccountsId() : Id("$$Singleton$$")
{
    public static TestAccountsId Instance = new();
}

public class TestHandler : IEventHandler
{
    public string DiagnosticName => nameof(TestHandler);

    public ValueTask<EventHandlingStatus> HandleEvent(IMessageConsumeContext context)
    {
        count++;
        if (count % 1000 == 0)
        {
            Console.WriteLine("{0}", $"Handled {count} events");
        }
        return ValueTask.FromResult(EventHandlingStatus.Success);
    }

    private int count;
}
