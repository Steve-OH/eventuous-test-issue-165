using Eventuous.SqlServer;
using Eventuous.SqlServer.Subscriptions;

namespace EventuousTest;

internal class TestCheckpointStore : SqlServerCheckpointStore
{
    public TestCheckpointStore(GetSqlServerConnection getConnection) : base(getConnection, Program.SchemaName)
    { }
}
