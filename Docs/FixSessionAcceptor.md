# FIX Session Acceptor
## Create

FIX Antenna automatically creates a session acceptor when a Logon message is received. A user is notified
via `IFixServerListener`, and they may opt to accept or reject this session by calling `Dispose()` or `Connect()`.

## Connect
Use `session.Connect()` to accept a connection.

## Reject
Use `session.Reject(string reason)` to reject an incomming connection. In this case, the engine will send only a `Logout(5)` message
to the counterparty.

## Reconnect
According to the FIX protocol standard, it is not the acceptor's responsibility to reconnect. When a
connection is unexpectedly terminated, the acceptor session is automatically closed. When a correct Logon is received,
the acceptor completes the reconnection process and continues working from the point at which it had been before termination.
Note: if the session wasn't disposed, the same session's instance can be reused during the recconnect procedure.

## Disconnect
To disconnect the acceptor (this process can be also called "terminate", "delete", "close"), the
following method can be used:
```csharp
 session.Disconnect(string reason);
```

## Release
To release all of a session's resources and unregister a session, use:
```csharp
session.Dispose();
```

## Send message
To send outgoing application-level messages, use:
```csharp
session.SendMessage(string type, FixMessage content)
```

All session level messages are sent automatically when needed.

