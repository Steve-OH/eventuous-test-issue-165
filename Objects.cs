using Eventuous;

namespace EventuousTest;

// value object to act as event data
internal record TestAccount(string Username);

// command to inject a batch of test accounts into the TestAccounts aggregate
internal record InjectTestAccounts(IList<TestAccount> Accounts);

// event representing insertion of a test account into the TestAccounts aggregate
[EventType($"V1.{nameof(TestAccountInserted)}")]
internal record TestAccountInserted(TestAccount Account);
