# Backgrounder
## About FIX

The Financial Information eXchange (&quot;FIX&quot;) Protocol is a series of
messaging <a href="https://www.fixtrading.org/online-specification/">specifications</a> for the electronic communication of trade-related messages.
It has been developed through the collaboration of banks, broker-dealers, 
exchanges, industry utilities and associations, institutional investors, 
and information technology providers from around the world. 
These market participants share a vision of a common, global language for 
the automated trading of financial instruments.

FIX is the industry-driven messaging standard that is changing the face of
the global financial services sector, as firms use the protocol to transact
in an electronic, transparent, cost efficient and timely manner. FIX is
open and free, but it is not software. Rather, FIX is a specification
around which software developers can create commercial or open-source
software, as they see fit. As the market's leading trade-communications
protocol, FIX is integral to many order management and trading systems.
Yet, its power is unobtrusive, as users of these systems can benefit from
FIX without knowing the language itself.

FIX was written to be independent of any specific communications protocol
(X.25, asynch, TCP/IP, etc.) or physical medium (copper, fiber, satellite,
etc.) chosen for electronic data delivery. It should be noted that if an
&quot;unreliable&quot; or non-stream protocol is used, the Logon, Logout, and
ResendRequest message processing is particularly susceptible to unordered
delivery and/or message loss.

The protocol is defined at two levels: session and application. The
session level is concerned with the delivery of data while the application
level defines business related data content. This document focuses on the
delivery of data using the &quot;FIX Session Protocol&quot;.<sup>[[1]](#source1)</sup>

## About FIX messages

The FIX Protocol currently exists in two syntaxes:
1. &quot;Tag=Value&quot; syntax
2. FIXML syntax

The same business message flow applies to either syntax. A specific syntax
is simply a slightly different way to represent the same thing in much the
same way that &quot;3&quot; and &quot;three&quot; represent the same thing.<sup>[[2]](#source2)</sup>

### Message Format
The general format of a FIX message is a standard header followed by the
message body fields and terminated with a standard trailer.

Each message is constructed of a stream of &lt;tag&gt;=&lt;value&gt; fields with a
field delimiter between fields in the stream. Tags are of data type
TagNum. <strong>All tags must have a value specified.</strong> Optional fields without
values should simply not be specified in the FIX message. A Reject message
is the appropriate response to a tag with no value.

Except where noted, fields within a message can be defined in any sequence.
(The relative position of a field within a message is inconsequential.) The
exceptions to this rule are:

1. General message format is composed of the standard header followed by
the body followed by the standard trailer.
2. The first three fields in the standard header are BeginString (tag
#8), followed by BodyLength (tag #9), followed by MsgType (tag #35).
3. The last field in the standard trailer is the CheckSum (tag #10).
4. Fields within repeating data groups must be specified in the order
that the fields are specified in the message definition within the FIX
specification document. The NoXXX field where XXX is the field being
counted specifies the number of repeating group instances that must
immediately precede the repeating group contents.
5. A tag number (field) should only appear in a message once. If it
appears more than once in the message it should be considered an error
with the specification document. The error should be pointed out to the
FIX Global Technical Committee.

In addition, certain fields of the data type MultipleValueString can
contain multiple individual values separated by a space within the &quot;value&quot;
portion of that field followed by a single &quot;SOH&quot; character (e.g. &quot;18=2 9 C&lt;SOH&gt;&quot;
represents 3 individual values: '2', '9', and 'C').

It is also possible for a field to be contained in both the clear text
portion and the encrypted data sections of the same message. This is
normally used for validation and verification. For example, sending the
SenderCompID in the encrypted data section can be used as a rudimentary
validation technique. In the cases where the clear text data differs from
the encrypted data, the encrypted data should be considered more reliable.
(A security warning should be generated.)<sup>[[2]](#source2)</sup>

### Field Delimiter
All fields (including those of data type data i.e. SecureData, RawData,
SignatureData, XmlData, etc.) in a FIX message are terminated by a
delimiter character. The non-printing, ASCII &quot;SOH&quot; (#001, hex:  0x01,
referred to in this document as &lt;SOH&gt;), is used for field termination.
Messages are delimited by the &quot;SOH&quot; character following the CheckSum field.
All messages begin with the &quot;8=FIX.x.y&lt;SOH&gt;&quot; string and terminate with
&quot;10=nnn&lt;SOH&gt;&quot;.

There shall be no embedded delimiter characters within fields except for data type data.<sup>[[2]](#source2)</sup>

### Repeating Groups
It is permissible for fields to be repeated within a repeating group (e.g.
&quot;384=2&lt;SOH&gt;372=6&lt;SOH&gt;385=R&lt;SOH&gt;372=7&lt;SOH&gt;385=R&lt;SOH&gt;&quot; represents a repeating
group with two repeating instances &quot;delimited&quot; by tag 372 (first field in
the repeating group.).
* If the repeating group is used, the first field of the repeating group
is required. This allows implementations of the protocol to use the
first field as a &quot;delimiter&quot; indicating a new repeating group entry. The
first field listed after the NoXXX then becomes conditionally required
if the NoXXX field is greater than zero.
* The NoXXX field (for example: NoTradingSessions, NoAllocs), which
specifies the number of repeating group instances, occurs once for a
repeating group and must immediately precede the repeating group
contents.
* The NoXXX field is required if one of the fields in the repeating
group is required. If all members of a repeating group are optional,
then the NoXXX field should also be optional.
* If a repeating group field is listed as required, then it must appear
in every repeated instance of that repeating group.
* Repeating groups are designated within the message definition via
indentation and the ' symbol.

Some repeating groups are nested within another repeating group (potentially
more than one level of nesting).
* Nested repeating groups are designated within the message definition
via indentation and the ' symbol followed by another ' symbol.
* If a nested repeating group is used, then the outer repeating group must be specified.<sup>[[2]](#source2)</sup>

### User Defined Fields
In order to provide maximum flexibility for its users, the FIX protocol
accommodates User Defined Fields. These fields are intended to be
implemented between consenting trading partners and should be used with
caution to avoid conflicts, which will arise as multiple parties begin
implementation of the protocol. It is suggested that if trading partners
find that particular User Defined Fields add value, they should be
recommended to the FIX Global Technical Committee for inclusion in a future
FIX version.

The tag numbers 5000 to 9999 have been reserved for use with user defined
fields, which are used as part of inter-firm communcation. These tags can
be registered/reserved via the FIX website.

The tag numbers greater than or equal to 10000 have been reserved for
internal use (within a single firm) and do not need to be
registered/reserved via the FIX website.<sup>[[2]](#source2)</sup>

### Component Blocks

Many of the FIX Application Messages are composed of common &quot;building
blocks&quot; or sets of data fields. For instance, almost every FIX Application
Message has the set of symbology-related fields used to define the
&quot;Instrument&quot;:  Symbol, SymbolSfx, SecurityIDSource, SecurityID,
EncodedSecurityDesc. Rather than replicate a common group of fields, the
FIX specification specifies several key component blocks below which are
simply referenced by component name within each Application Message which
uses them. Thus, when reviewing a specific message definition, the
appropriate group of fields should be expanded and used whenever a
component block is identified.

Note that some component blocks may be part of repeating groups. If the
component block is denoted as part of a repeating group, then the entire
group of fields representing the component block are to be specified at the
component block's repeating group &quot;level&quot; in the message definition and
follow repeating group rules concerning field order. See &quot;Repeating
Groups&quot; for more details.<sup>[[2]](#source2)</sup>

## About FIX sessions

### Sequence Numbers
> All FIX messages are identified by a unique sequence number. Sequence
> numbers are initialized at the start of each FIX session (see Session
> Protocol section) starting at 1 (one) and increment throughout the session.
> Monitoring sequence numbers will enable parties to identify and react to
> missed messages and to gracefully synchronize applications when
> reconnecting during a FIX session.
> 
> Each session will establish an independent incoming and outgoing sequence
> series; participants will maintain a sequence series to assign to outgoing
> messages and a separate series to monitor for sequence gaps on incoming
> messages.<sup>[[3]](#source3)</sup>

### Heartbeats
> During periods of message inactivity, FIX applications will generate
> Heartbeat messages at regular time intervals. The heartbeat monitors the
> status of the communication link and identifies incoming sequence number
> gaps. The Heartbeat Interval is declared by the session initiator using
> the `HeartBtInt` field in the Logon message. The heartbeat interval timer
> should be reset after every message is transmitted (not just heartbeats).
> The `HeartBtInt` value should be agreed upon by the two firms and specified
> by the Logon initiator and echoed back by the Logon acceptor. Note that
> the same `HeartBtInt` value is used by both sides, the Logon "initiator" and
> Logon "acceptor".<sup>[[4]](#source4)</sup>

### Ordered Message Processing
> The FIX protocol assumes complete ordered delivery of messages between
> parties. Implementers should consider this when designing message gap fill
> processes. Two options exist for dealing with gaps: either request all
> messages subsequent to the last message received or ask for the specific
> message missed while maintaining an ordered list of all newer messages.
> For example, if the receiver misses the second of five messages, the
> application could ignore messages 3 through 5 and generate a resend request
> for messages 2 through 5, or, preferably 2 through 0 (where 0 represents
> infinity). Another option would involve saving messages 3 through 5 and
> resending only message 2. In both cases, messages 3 through 5 should not
> be processed before message 2.<sup>[[3]](#source3)</sup>

### Possible Duplicates
> When a FIX engine is unsure if a message was successfully received at its
> intended destination or when responding to a resend request, a possible
> duplicate message is generated. The message will be a retransmission (with
> the same sequence number) of the application data in question with the
> PossDupFlag included and set to "Y" in the header. It is the receiving
> application's responsibility to handle the message (i.e. treat as a new
> message or discard as appropriate). All messages created as the result of
> a resend request will contain the PossDupFlag field set to "Y". Messages
> lacking the PossDupFlag field or with the PossDupFlag field set to "N"
> should be treated as original transmissions. Note:  When retransmitting a
> message with the PossDupFlag set to Y, it is always necessary to
> recalculate the CheckSum value. The only fields that can change in a
> possible duplicate message are the CheckSum, OrigSendingTime, SendingTime,
> BodyLength and PossDupFlag. Fields related to encryption (SecureDataLen
> and SecureData) may also require recasting.<sup>[[3]](#source3)</sup>

### Possible Resends
> Ambiguous application level messages may be resent with the PossResend flag
> set. This is useful when an order remains unacknowledged for an inordinate
> length of time and the end-user suspects it had never been sent. The
> receiving application must recognize this flag and interrogate internal
> fields (order number, etc.) to determine if this order has been previously
> received. Note: The possible resend message will contain exactly the same
> body data but will have the PossResend flag and will have a new sequence
> number. In addition, the CheckSum field will require recalculation and
> fields related to encryption (SecureDataLen and SecureData) may also
> require recasting.<sup>[[3]](#source3)</sup>

### Data Integrity
> The integrity of message data content can be verified in two ways:
> verification of message length and a simple checksum of characters.
> 
> The message length is indicated in the BodyLength field and is verified by
> counting the number of characters in the message following the BodyLength
> field up to, and including, the delimiter immediately preceding the
> CheckSum tag ("10=").
> 
> The CheckSum integrity check is calculated by summing the binary value of
> each character from the "8" of "8=" up to and including the &lt;SOH&gt; character
> immediately preceding the CheckSum tag field and comparing the least
> significant eight bits of the calculated value to the CheckSum value (see
> "CheckSum Calculation" for a complete description).<sup>[[4]](#source4)</sup>

### Message Acknowledgment
> The FIX session protocol is based on an optimistic model; normal delivery
> of data is assumed (i.e. no acknowledgment of individual messages) with
> errors in delivery identified by message sequence number gaps. Each
> message is identified by a unique sequence number. It is the receiving
> application's responsibility to monitor incoming sequence numbers to
> identify message gaps for response with resend request messages.
> 
> The FIX protocol does not support individual message acknowledgment.
> However, a number of application messages require explicit application
> level acceptance or rejection. Orders, cancel requests, cancel/replace
> requests, allocations, etc. require specific application level responses,
> executions can be rejected with the DK message but do not require explicit
> acceptance. See "Volume 1 - Business Message Reject" for details regarding
> the appropriate response message to specific application level messages.<sup>[[4]](#source4)</sup>

### Encryption
The exchange of sensitive data across public carrier networks may make it
advisable to employ data encryption techniques to mask the application
messages.<sup>[[5]](#source5)</sup>

> The choice of encryption method will be determined by mutual agreement of
> the two parties involved in the connection.
> 
> Any field within a message can be encrypted and included in the SecureData
> field, however, certain explicitly identified fields must be transmitted
> unencrypted. The clear (unencrypted) fields can be repeated within the
> SecureData field to serve as an integrity check of the clear data.
> 
> When encryption is employed, it is recommended but not required that all
> fields within the message body be encrypted. If repeating groups are used
> within a message and encryption is applied to part of the repeating group,
> then the entire repeating group must be encrypted.<sup>[[3]](#source3)</sup>

Embedded in the protocol are fields that enable the implementation of a
public key signature and encryption methodology, straight DES encryption
and clear text. The previously agreed upon encryption methodology is
declared in the Logon message. (For more detail on implementation of
various encryption techniques, see the application notes section on the FIX
Web Site.)<sup>[[5]](#source5)</sup>

### FIX Session
A FIX session is defined as a bi-directional stream of ordered messages
between two parties within a continuous sequence number series. A single
FIX session can exist across multiple sequential (not concurrent) physical
connections. Parties can connect and disconnect multiple times while
maintaining a single FIX session. Connecting parties must bi-laterally
agree as to when sessions are to be started/stopped based upon individual
system and time zone requirements. Resetting the inbound and outbound
sequence numbers back to 1, for whatever reason, constitutes the beginning
of a new FIX session.

It is recommended that a new FIX session be established once within each 24
hour period. It is possible to maintain 24 hour connectivity and establish
a new set of sequence numbers by sending a Logon message with the
ResetSeqNumFlag set.

The FIX session protocol is based on an optimistic model. Normal delivery
of data is assumed (i.e. no communication level acknowledgment of
individual messages) with errors in delivery identified by message sequence
number gaps. This section provides details on the implementation of the
FIX session layer and dealing with message sequence gaps.<sup>[[5]](#source5)</sup>

* <strong>Valid FIX Message</strong> is a message that is properly formed according to
this specification and contains a valid body length and checksum field.
* <strong>Initiator</strong> establishes the telecommunications link and initiates the
session via transmission of the initial Logon message.
* <strong>Acceptor</strong> is the receiving party of the FIX session. This party has
responsibility to perform first level authentication and formally
declare the connection request "accepted" through transmission of an
acknowledgment Logon message.
* <strong>Unregistered Acceptor</strong> is the acceptor,
which does not exist by the time the new incoming logon message appears.
* <strong>FIX Connection</strong> is comprised of three parts: logon, message exchange,
and logout.
* <strong>FIX Session</strong> is comprised of one or more FIX Connections, meaning that
a FIX Session spans multiple logins.

## About FIX 5.0
> With the release of FIX 5.0 in October 2006, the FPL Global Technical
> Committee (GTC) introduced a new framework, the transport independence (TI)
> framework, which separated the FIX Session Protocol from the FIX
> Application Protocol. Under the TI framework the application protocol
> messages can be sent over any suitable session transport technology (e.g.
> WS-RX, MQ, publish/subscribe message bus), where the FIX Session Protocol
> is one of the available options as a session transport for FIX application
> messages. From this release forward, the FIX Application layer and the FIX
> Session layer will have their own versioning moniker. The FIX Application
> layer will retain the traditional version moniker of "FIX x.y" while the
> FIX Session layer will utilize a new version moniker of "FIXT x.y" (note
> that the version numbers will be independent of each other). The diagram
> below illustrates how previously the FIX Session layer was tighly coupled
> to the Application layer. With the advent of Application Versioning and
> Transport Independence, the FIX Session and Application layers have been
> decoupled and are now independent.<sup>[[6]](#source6)</sup>

### Transport Independence (TI) Framework
> The transport independence (TI) framework separates the previously coupled
> FIX Session layer from the FIX Application layer. Under this framework, the
> FIX Application Protocol can use any transport technology in addition to
> the FIX Session Protocol. The diagram below illustrates how various
> transport mechanisms, including the FIX Session layer, can be used to carry
> the full suite of FIX Application versions.
> 
> To support this framework, a key new field has been added called ApplVerID
> (application version ID, tag 1128). Depending on the use case, ApplVerID
> may be optional or required. Additionally, the FIX field BeginString will
> no longer identify the FIX application version, but identifies the FIX
> Session Protocol version. The sections below discusses the four main uses
> cases supported by the TI framework.<sup>[[6]](#source6)</sup>

### Application Versioning
> Application Versioning allows extensions to the current base application
> version to be applied using a formal release process. Extension Packs
> represent the individual gap analysis proposals submitted to the GTC for
> review and approval. Extension Packs are grouped into Service Packs and
> are applied to the base application version, usually the most current FIX
> application version. A new application version is formed when a new
> Service Pack is applied to a base version. FIX 4.4
> has been extended via Service Pack 0, forming a new application version
> called FIX 5.0. As new Extension Packs are approved they will be grouped
> into Service Packs which is then released to form the next application
> version identified as FIX 5.0 SP1 and FIX 5.0 SP2. These application versions are
> expressed using the new tag ApplVerID.<sup>[[6]](#source6)</sup>

## References
<a name="source1">1. B2BITS. "FIX Protocol Concepts". corp-web.b2bits.com, B2BITS FIX Antenna .NET, Syntax based on .NET Framework version 4.8, https://corp-web.b2bits.com/fixanet/doc/html/html/e0590827-dcd5-4f9c-a6a6-b181068efbe4.htm.</a><br>

<a name="source2">2. B2BITS. "FIX Messages". corp-web.b2bits.com, B2BITS FIX Antenna .NET, Syntax based on .NET Framework version 4.8, https://corp-web.b2bits.com/fixanet/doc/html/html/464a7a55-67c0-483d-95e4-18aa083715b1.htm.</a><br>

<a name="source3">3. AJ. "FIX protocol interview questions: Part 1 - Admin/Session". Some Technicalities of Life, April 27, 2010, https://aj-sometechnicalitiesoflife.blogspot.com/2010/04/fix-protocol-interview-questions.html.</a><br>

<a name="source4">4. OnixS. "FIXT 1.1: Message Delivery - FIX Dictionary." onixs.biz, OnixS, Accessed on January 1, 2022, https://www.onixs.biz/fix-dictionary/fixt1.1/section_message_delivery.html.</a><br>

<a name="source5">5. B2BITS. "FIX Sessions". corp-web.b2bits.com, B2BITS FIX Antenna .NET, Syntax based on .NET Framework version 4.8, https://corp-web.b2bits.com/fixanet/doc/html/html/60e5d73c-f584-41d1-b6b5-91c233dbd33f.htm.</a><br>

<a name="source6">6. FIX Protocol, Limited. "Financial Information Exchange Protocol (FIX) Version 5.0 Release Notes". bvc.com.co, December 2006, http://www.bvc.com.co/recursos/Files/Bus_de_Integracion/FIX-50-release-notes.pdf.</a><br>
