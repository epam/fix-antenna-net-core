# FIX Session Initiator
## Create

Call the following method to create a session:
```csharp
IFixSession session = sessionParameters.CreateNewFixSession();
```

You can use the `autoreconnectAttempts` configurations parameter set to positive value 
to create sessions that will try to reconnect if the connection is lost.

After the session is created, set a listener
```csharp
session.SetFixSessionListener(IFixSessionListener listener);
```

After the session is created, use the method below to connect it as an initiator.
```csharp
session.Connect();
```

## Establish connection

After `session.Connect()` is called, the initiator starts attempts to establish a FIX session in the background. The following scenario is executed:
- Establish telecommunication link (TCP/IP)
- Send Logon message
- Wait for confirm logon message
- Move to "established" state

There is no need to wait until the connection is established to send messages. If the connection is
not established, messages will be sent later after the connection process succeeds.

## Reconnect
The initiator is responsible for restoring a connection once it is broken. When a connection error
is detected, the initiator moves to the "Reconnect" state and starts the reconnection process. The
foillowing scenario is executed:
1. Establish telecommunication link
2. Send Logon message
3. Repeat from 1 if 1 or 2 fails
4. Stop trying if attempts are over

The number of reconnection attempts and delay can be set in the properties file.

## Disconnect
You can use the following method to disconnect the initiator (the process can also be called "terminate", "delete", "close"):
```csharp
session.Disconnect(string reason); 
```

## Dispose
To release all of a session's resources and unregister the session, use:
```csharp
session.Dispose();
```

## Send message
To send outgoing application-level messages, use one of the following methods:
```csharp
session.SendMessage(string type, FixMessage content)
```

or

```csharp
session.SendMessage(FixMessage message)
```

All session-level messages are sent automatically when needed.

