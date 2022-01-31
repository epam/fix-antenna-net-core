# Recovery
## Store-and-forward
FIX Antenna follows two main rules:
1. An outgoing message is saved to the file and then sent.
2. An incoming message is saved to the file after it is processed.

There are two very important consequences of these rules that, in combination with
FIX sequencing and retransmission, make losing a message in FIX Antenna impossible:
1. If the application crashes during incoming message processing, the message will not be
stored. Hence, after restoring a session the _**sequence number too high**_ will be identified
and a resend request will be sent.
2. If the application crashes before a message is delivered to a counter-party, the 
counter-party will identify a gap and send a request for retransmission after
restoring the session.

## Gap fill
Gap fill is a standard FIX mechanism designed to indentify and resolve message loss. 
It is based on sequencing messages in each direction, the resend request mechanism, the
`PosDup` flag and sequence reset.

When a session identifies the _**sequence number too high**_ problem, it sends a resend request 
message asking for a retransmission of lost messages. The opposite side resends the requested 
messages with the `PosDupFlag` set to `Y`. Session level messages are not resent. The sequence 
reset is used to keep sequence numbers consistent during resending.

FIX Antnenna automatically resolves gap fill, i.e. no manual work is required. However, it is
possible to intervernt into standard mechanism.

## Fail-over
FIX Antenna comes with a basic failover mechanism. 
If the application crashes after the next initialization, FIX Antenna will fully restore its state as it was before the crash. 
No information will be lost.
