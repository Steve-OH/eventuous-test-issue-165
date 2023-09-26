using Eventuous;
using Eventuous.SqlServer;
using Eventuous.SqlServer.Subscriptions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using Microsoft.AspNetCore.Builder;

namespace EventuousTest;

public class Program
{
    public static string SchemaName => "TEST";

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var services = builder.Services;

        services.AddLogging();
        Console.OutputEncoding = Encoding.UTF8;

        TypeMap.RegisterKnownEventTypes();
        var schema = new Schema(SchemaName);
        schema.CreateSchema(getConnection);

        services
            .AddSingleton<GetSqlServerConnection>(getConnection)
            .AddSingleton(new SqlServerStoreOptions(SchemaName))
            .AddAggregateStore<SqlServerStore>()
            .AddCheckpointStore<TestCheckpointStore>()
            .AddAggregate<TestAccounts>()
            .AddCommandService<TestCommandService, TestAccounts>()
            .AddSubscription<SqlServerAllStreamSubscription, SqlServerAllStreamSubscriptionOptions>("TestSubscription", subscriptionBuilder => subscriptionBuilder
                .Configure(options => options.Schema = SchemaName)
                .AddEventHandler<TestHandler>());

        services
            .AddHostedService<InjectorService>();

        builder.Build().Run();
    }

    private static SqlConnection getConnection()
    {
        return new SqlConnection("Data Source=MY_SERVER;Initial Catalog=MyDatabase;Integrated Security=true;Trust Server Certificate=true");
    }
}
