This repository contains a simple ASP .NET Core project that reproduces a bug 
described in https://github.com/Eventuous/eventuous/issues/165.

There is a hosted background service, `InjectorService`, that runs at application 
startup to send an `InjectTestMessages` command to the `TestCommandService`, which 
then invokes the `TestAccounts` aggregate to populate the `TEST.Messages` table 
with 1000 `V1.TestAccountInserted` messages.

Finally, there is an `$all` stream subscription, `TestSubscription`, and its 
associated `TestHandler`. The handler returns `EventHandlingStatus.Success` for each 
message that it handles.

##### Console app

There is now also a console app, `NoAsp`, that is simpler (no DI) and exhibits 
the same problem.

##### Test conditions
* Operating system: Windows 10 Professional, version 22H2, build 19045.3448
* Database (local): SQL Server Developer (64 bit), version 15.0.2101.7
* Eventuous: version 0.14

##### Usage
Edit the `Data Source` and `Initial Catalog` values in the connection string in 
`Program.cs` to match your local setup, then build and run the app. If you then 
examine the contents of the `TEST.Checkpoints` table, you will see that the 
last checkpoint value is a small number, in the range of roughly 8 to 128, even 
though the subscription has processed 1000 messages (as indicated by the log 
output from the `TestHandler`). Subsequent runs of the app increase the 
checkpoint value by small increments, but it never catches up.

The exact number of events at which the checkpoint update fails is very 
sensitive to the code in the app; while the app pretty reliably fails at the 
same checkpoint value every time when starting from a fresh database, that 
point varies as you make small changes to the code, which suggests that at the 
heart of the problem there is some kind of race condition.

Interestingly, most of the time there is no error reported; the checkpoint 
update process just fails silently. Once in a while, however, after a few
repeated runs, the app will fail with a null reference exception:

```
fail: Eventuous.Subscription[0]
      [TestSubscription] Unable to commit position CommitPosition { Position = 748, Sequence = 709, Timestamp = 9/26/2023 2:20:25 AM, Valid = True, LogContext = Eventuous.Subscriptions.Logging.LogContext }
      System.NullReferenceException: Object reference not set to an instance of an object.
         at System.Collections.Generic.SortedSet`1.DoRemove(T item)
         at System.Collections.Generic.SortedSet`1.Remove(T item)
         at System.Collections.Generic.SortedSet`1.RemoveWhere(Predicate`1 match)
         at Eventuous.Subscriptions.Checkpoints.CheckpointCommitHandler.CommitInternal(CommitPosition position, CancellationToken cancellationToken)
```

I have only experienced the problem with an `$all` stream subscription; 
single-stream subscriptions either don't have the problem at all, or it occurs 
rarely enough that I haven't been able to reproduce it.

In an app with two or more subscriptions, I sometimes see a different error 
that appears to arise from contention among the subscriptions:

```
      [TestSubscription] Dropped
      Microsoft.Data.SqlClient.SqlException (0x80131904): Transaction (Process ID 56) was deadlocked on lock resources with another process and has been chosen as the deadlock victim. Rerun the transaction.
         at Microsoft.Data.SqlClient.SqlConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)
         at Microsoft.Data.SqlClient.SqlInternalConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)
         at Microsoft.Data.SqlClient.TdsParser.ThrowExceptionAndWarning(TdsParserStateObject stateObj, Boolean callerHasConnectionLock, Boolean asyncClose)
         at Microsoft.Data.SqlClient.TdsParser.TryRun(RunBehavior runBehavior, SqlCommand cmdHandler, SqlDataReader dataStream, BulkCopySimpleResultSet bulkCopyHandler, TdsParserStateObject stateObj, Boolean& dataReady)
         at Microsoft.Data.SqlClient.SqlDataReader.TryHasMoreRows(Boolean& moreRows)
         at Microsoft.Data.SqlClient.SqlDataReader.TryReadInternal(Boolean setTimeout, Boolean& more)
         at Microsoft.Data.SqlClient.SqlDataReader.ReadAsyncExecute(Task task, Object state)
         at Microsoft.Data.SqlClient.SqlDataReader.InvokeAsyncCall[T](SqlDataReaderBaseAsyncCallContext`1 context)
      --- End of stack trace from previous location ---
         at Eventuous.SqlServer.Extensions.ReaderExtensions.ReadEvents(SqlDataReader reader, CancellationToken cancellationToken)+MoveNext()
         at Eventuous.SqlServer.Extensions.ReaderExtensions.ReadEvents(SqlDataReader reader, CancellationToken cancellationToken)+System.Threading.Tasks.Sources.IValueTaskSource<System.Boolean>.GetResult()
         at Eventuous.SqlServer.Subscriptions.SqlServerSubscriptionBase`1.PollingQuery(Nullable`1 position, CancellationToken cancellationToken)
         at Eventuous.SqlServer.Subscriptions.SqlServerSubscriptionBase`1.PollingQuery(Nullable`1 position, CancellationToken cancellationToken)
         at Eventuous.SqlServer.Subscriptions.SqlServerSubscriptionBase`1.PollingQuery(Nullable`1 position, CancellationToken cancellationToken)
         at Eventuous.SqlServer.Subscriptions.SqlServerSubscriptionBase`1.PollingQuery(Nullable`1 position, CancellationToken cancellationToken)
         at Eventuous.SqlServer.Subscriptions.SqlServerSubscriptionBase`1.PollingQuery(Nullable`1 position, CancellationToken cancellationToken)
      ClientConnectionId:ae5c45b4-2c6d-48d3-9a63-1dbbc99d542c
```

This won't happen in the current app, since there is only one subscription.

