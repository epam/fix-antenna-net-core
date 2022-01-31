# FIX Session
## Persistent session
All messages are saved in persistent storage.

Session data is stored to the hard drive and can be restored after failure.
The following session information is stored in this case:
- Session parameters (`SenderCompID`, `TargetCompID`, FIX version etc.)
- Session incoming sequence number
- Incoming messages (optional)
- Outgoing messages

## Session state
From the moment of session creation to the moment of its termination,
the session uses a state that dictates its reaction to events.
The session state can be obtained using the `session.GetSessionState()`
method, which returns `SessionState`.

The `SessionState` class provides a set of predefined public static fields for each state available. <br>
`SessionState.ToString()` returns the values listed below:
- CONNECTING - connection process is in progress
- WAITING_FOR_LOGON - connection has been estabished, but Logon has not been received yet
- CONNECTED - session is connected
- WAITING_FOR_LOGOFF - waiting for a Logoff message
- DISCONNECTED - session is disconnected
- LOGON_RECEIVED - Logon received (for acceptor sessions)
- DEAD - session is disposed
- DISCONNECTED_ABNORMALLY - session has been disconnected abnormally
- RECONNECTING - session is reconnecting
- WAITING_FOR_FORCED_LOGOFF - session is waiting for Logout and trying to terminate gracefully due to the certain FIX
condition, but will be disconnected after a while even if Logout is not received. Engine will wait for Logout answer
during next heartbeat interval.
- WAITING_FOR_FORCED_DISCONNECT - session was disconnected due to an error (for example, received lower sequence number).
The Logoff was sent to counterparty and the session is waiting a bit before closing connection. According to FIX specifications,
the engine ignores all incoming messages in this state.

## Sequence number handling

A session always has two sequence numbers:
- Incoming sequence number is a counter of incoming messages
- Outgoing sequence number is a counter of outgoing messages

Both sides must maintain those two values and control their
synchronization. There are two types of sequence numbers desynchronization:
- Sequence number too high indicates message loss and leads to resend procedure.
- Sequence number too low indicates some serious problem and must lead to immediate
session termination and manual sequence number synchronization.

Both sequence numbers are started from 1 when the session is created from
scratch. If the connection is terminated, sequence numbers continue after
restoration. A lot of service providers never reset sequence numbers
during the day. The are also some that reset sequence numbers once per week.
There are several cases to handle a deviation such as this one from the standard in FIX Antenna.


### Resetting sequence numbers

To reset sequence numbers, simply set `incomingSequenceNumber` and
`outgoingSequenceNumber` in `SessionParameters` to 1 or any other desired value.
By default, it is set to 0 (automatically restore sequence number).
It is also possible to remove FIX session persistent files from the logs
directory. Since FIX Antenna stores the session state in those files, the
absence of such files will be treated as a session being created from scratch,
and thus sequence numbers will be set to 1. Files can be removed during
the End-Of-Day or End-Of-Week procedure.
- ForceSeqNumReset mode makes the session reset sequence numbers each time
on Logon and forces a counterparty to do the same (standard FIX mechanism).
This is not a recommended way since messages sent during inactivity time
will be lost.


## Session qualifier
The session qualifier gives a user the ability to create several sessions with the same `SenderCompId` and `TargetCompId`, as well as the ability to address these sessions by unique ID.


To establish several sessions with the same `SenderCompId` and `TargetCompId`, both the Initiator and Acceptor should create several session objects that differ from each other by `SessionQualifier` property. The `SessionQualifier` property on the Initiator side must be the same as the `SessionQualifier` property of the corresponding session on the Acceptor side. When the Initiator connects to the Acceptor, it sends the `SessionQualifier` along with the `SenderCompId` and `TargetCompId` in the logon message. The `SessionQualifier` tag in the logon message is optional and can be configured in the <i>fixengine.properties</i> file. When the Acceptor receives logon message with the `SessionQualifier` it searches for a registered session with a `SessionId` corresponding to the received `TargetCompId`, `SenderCompId` and `SessionQualifier`.


The FIX engine will automatically insert the  `SessionQualifier` tag into the logon message if the user creates a session with a qualifier on the initiator side and the `SessionQualifier` tag is appropriately configured in <i>fixengine.properties</i>. To configure the `SessionQualifier` tag, the user needs to add the following lines to the config file:
```csharp
logonMessageSessionQualifierTag = 9012
```
To create a session with a qualifier, you need set a unique qualifier to `SessionParameters`:
```csharp
// define session
SessionParameters details = new SessionParameters();
details.SenderCompId = "senderId";
details.TargetCompId = "targetId";
// set session qualifier
details.SessionQualifier = "1";
```
