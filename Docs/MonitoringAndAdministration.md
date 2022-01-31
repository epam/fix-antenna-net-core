# Monitoring and Administration
## Overview
The FIX Antenna library provides access to embedded monitoring and administrative
functionalities by means of usual sessions.
Creation of an admin session requires configuring the following parameters:

```INI
autostart.acceptor.targetIds
autostart.acceptor.targetId.login
autostart.acceptor.targetId.password
autostart.acceptor.targetId.ip
autostart.acceptor.targetId.fixServerListener
```

and creating a FIX session with one of configured parameters of `TargetCompID` (`autostart.acceptor.targetIds`).

By default, these parameters have the following values:

```INI
autostart.acceptor.targetIds=admin,admin1
autostart.acceptor.admin.login=admin
autostart.acceptor.admin.password=admin
autostart.acceptor.admin.ip=*
autostart.acceptor.admin.fixServerListener=Epam.FixAntenna.AdminTool.AdminTool,Epam.FixAntenna.AdminTool

autostart.acceptor.admin1.login=admin1
autostart.acceptor.admin1.password=admin1
autostart.acceptor.admin1.ip=*
autostart.acceptor.admin1.fixServerListener=Epam.FixAntenna.AdminTool.AdminTool,Epam.FixAntenna.AdminTool
```

All FIX messages from a client of this kind of session are transferred to the `AdminTool` class,
where they are handled.
A FIX XML message (MsgType = n) is used to transport commands or execution results in the XML form.

An example of a FIX XML message with a command:
```xml
8=FIX.4.49=17435=n49=MonitoringTool56=FIXADMIN34=252=20050811-12:22:53212=89213=<?xml version="1.0" encoding="utf-8"?>
<ToBackup>
    <SenderCompID>TestSender</SenderCompID>
    <TargetCompID>TestTarget</TargetCompID>
</ToBackup>10=012
```

An example of a FIX XML message with a response:
```xml
8=FIX.4.49=18235=n49=FIXADMIN56=MonitoringTool34=252=20050811-12:22:55212=97213=<?xml version="1.0" encoding="utf-8"?>
<Response ResultCode="3">
    <Description>Unknown session</Description>
</Response>10=043
```

The customer has an opportunity to extend the basic functionality:
- The customer can develop her/his own utility, the protocol is open.
- The customer can register her/his own service session client to support other instructions.
- The customer can register her/his own class inherited from the abstract class Command
and register the root package to a new class in the configurations file (set value for
the parameter `autostart.acceptor.command.package`) to support other instructions.
For example:
   ```csharp
   autostart.acceptor.command.package=MyCompany.AdminCommands
   ```

## Response result codes
Each response of RAI has a special `ResultCode` attribute.

<table class="ria" border="1" cellpadding="2" cellspacing="0">
    <tbody>
    <tr class="header">
        <td>Code</td>
        <td>Description</td>
    </tr>
    <tr>
        <td align="center">0</td>
        <td>This result code indicates that RIA has successfully processed the request.</td>
    </tr>
    <tr>
        <td align="center">1</td>
        <td>The requested operation is not implemented.</td>
    </tr>
    <tr>
        <td align="center">3</td>
        <td>The RAI can't find requested session.</td>
    </tr>
    <tr>
        <td align="center">6</td>
        <td>The RAI thrown unexpected error during processing request of client.</td>
    </tr>
    <tr>
        <td align="center">7</td>
        <td>The client request was rejected.</td>
    </tr>
    <tr>
        <td align="center">9</td>
        <td>The client request has an invalid parameter.</td>
    </tr>
    </tbody>
</table>


## Supported commands

Monitoring and administrative commands, as well as execution results, are transported
by means of FIX XML messages (MsgType = n).

### Monitoring commands

- `SessionsList` - subscription request to get a list of sessions.

   An example of a request:
   ```xml
   <SessionsList RequestID="1">
       <SubscriptionRequestType>1</SubscriptionRequestType>
   </SessionsList>
   ```

   An example of a response:
   ```xml
   <Response ResultCode="0" RequestID="1">
       <SessionsListData>
           <Session>
               <SenderCompID>admin</SenderCompID>
               <TargetCompID>FIXICC</TargetCompID>
               <Timestamp>2010-03-26T16:54:03.443+02:00</Timestamp>
               <Status>CONNECTED</Status>
               <StatusGroup>ESTABLISHED</StatusGroup>
               <Action>NEW</Action>
           </Session>
       </SessionsListData>
   </Response>
   ```

- `SessionsSnapshot` - request to get detailed information about the list of sessions.

   An example of a request:
   ```xml
   <SessionsSnapshot RequestID="1">
       <View>STATUS_PARAMS</View>
   </SessionsSnapshot>
   ```

   An example of a response:
   ```xml
   <Response ResultCode="0" RequestID="1">
       <SessionsSnapshotData>
           <Session>
               <SenderCompID>admin</SenderCompID>
               <TargetCompID>FIXICC</TargetCompID>
               <StatusData>
                   <Status>CONNECTED</Status>
                   <StatusGroup>ESTABLISHED</StatusGroup>
                   <BackupState>PRIMARY</BackupState>
                   <InSeqNum>3</InSeqNum>
                   <OutSeqNum>3</OutSeqNum>
               </StatusData>
               <ParamsData>
                   <Version>FIX44</Version>
                   <Role>ACCEPTOR</Role>
                   <RemoteHost>127.0.0.1</RemoteHost>
                   <RemotePort>1694</RemotePort>
                   <ExtraSessionParams>
                       <HBI>120</HBI>
                       <StorageType>PERSISTENT</StorageType>
                       <EnableMessageRejecting>true</EnableMessageRejecting>
                       <ClientType>GENERIC</ClientType>
                       <ReconnectMaxTries>-1</ReconnectMaxTries>
                       <InSeqNum>4</InSeqNum>
                       <OutSeqNum>4</OutSeqNum>
                       <ForceSeqNumReset>ALWAYS</ForceSeqNumReset>
                   </ExtraSessionParams>
               </ParamsData>
           </Session>
       </SessionsSnapshotData>
   </Response>
   ```

- `SessionParams` - request to get detailed information about the session.

   An example of a request:
   ```xml
   <SessionParams RequestID="3">
       <SenderCompID>admin</SenderCompID>
       <TargetCompID>FIXICC</TargetCompID>
   </SessionParams>
   ```

   An example of a response:
   ```xml
   <Response ResultCode="0" RequestID="3">
       <SessionParamsData>
           <SenderCompID>admin</SenderCompID>
           <TargetCompID>FIXICC</TargetCompID>
           <Version>FIX44</Version>
           <Role>ACCEPTOR</Role>
           <RemoteHost>127.0.0.1</RemoteHost>
           <RemotePort>1694</RemotePort>
           <ExtraSessionParams>
               <HBI>120</HBI>
               <StorageType>PERSISTENT</StorageType>
               <EnableMessageRejecting>true</EnableMessageRejecting>
               <ClientType>GENERIC</ClientType>
               <ReconnectMaxTries>-1</ReconnectMaxTries>
               <InSeqNum>4</InSeqNum>
               <OutSeqNum>4</OutSeqNum>
               <ForceSeqNumReset>ALWAYS</ForceSeqNumReset>
           </ExtraSessionParams>
       </SessionParamsData>
   </Response>
   ```

- `SessionStatus` - request to get the session status.

   An example of a request:
   ```xml
   <SessionStatus RequestID="2">
       <SenderCompID>admin</SenderCompID>
       <TargetCompID>FIXICC</TargetCompID>
   </SessionStatus>
   ```

   An example of a response:
   ```xml
   <Response ResultCode="0" RequestID="2">
       <SessionStatusData>
           <SenderCompID>admin</SenderCompID>
           <TargetCompID>FIXICC</TargetCompID>
           <Status>CONNECTED</Status>
           <StatusGroup>ESTABLISHED</StatusGroup>
           <BackupState>PRIMARY</BackupState>
           <InSeqNum>3</InSeqNum>
           <OutSeqNum>3</OutSeqNum>
       </SessionStatusData>
   </Response>
   ```

- `SessionStat` - request to get statistical information about the session.

   An example of a request:
   ```xml
   <SessionStat RequestID="15">
       <SenderCompID>TestSender</SenderCompID>
       <TargetCompID>TestTarget</TargetCompID>
   </SessionStat>
   ```

   An example of a response:
   ```xml
   <Response ResultCode="0" RequestID="15">
       <SessionStatData>
           <SenderCompID>TestSender</SenderCompID>
           <TargetCompID>TestTarget</TargetCompID>
           <ReceivedBytes>5107901</ReceivedBytes>
           <SentBytes>6445854</SentBytes>
           <ReceivedMessages>25539</ReceivedMessages>
           <SentMessages>32229</SentMessages>
           <Established>2008-05-13T07:55:23</Established>
           <TerminatedNormal>2008-05-13T07:51:34</TerminatedNormal>
           <TerminatedAbnormal>2008-05-10T15:43:11</TerminatedAbnormal>
           <LastReceivedMessage>2008-05-13T18:21:10</LastReceivedMessage>
           <LastSentMessage>2008-05-13T18:21:10</LastSentMessage>
       </SessionStatData>
      </Response>
   ```

- `GeneralSessionsStat` - request to get statistical information about the current state of the FIX Engine.

   An example of a request:
   ```xml
   <GeneralSessionsStat RequestID="25"/>
   ```

   An example of a response:
   ```xml
   <Response ResultCode="0" RequestID="25">
       <GeneralSessionsStatData>
           <ActiveSessions>2</ActiveSessions>
           <ReconnectingSessions>1</ReconnectingSessions>
           <AwaitingSessions>0</AwaitingSessions>
           <TerminatedNormalSessions>32</TerminatedNormalSessions>
           <TerminatedAbnormalSessions>5</TerminatedAbnormalSessions>
           <NumOfProcessedMessages>10345</NumOfProcessedMessages>
           <MaxSessionLifetime>204580</MaxSessionLifetime>
           <MinSessionLifetime>4523</MinSessionLifetime>
           <LastSessionCreation>2008-05-13T18:21:10</LastSessionCreation>
       </GeneralSessionsStatData>
   </Response>
   ```

- `ReceivedStat` - request to get quantity of received messages.

   An example of a request:
   ```xml
   <ReceivedStat RequestID="10"/>
   ```
   
   An example of a response:
   ```xml
   <Response ResultCode="0" RequestID="10">
       <ReceivedStatData>
           <ReceivedMessages>76617</ReceivedMessages>
       </ReceivedStatData>
   </Response>
   ```

- `SentStat` - request to get quantity of sent messages.

   An example of a request:
   ```xml
   <SentStat RequestID="20"/>
   ```
   An example of a response:
   ```xml
   <Response ResultCode="0" RequestID="20">
       <SentStatData>
           <SentMessages>96687</SentMessages>
       </SentStatData>
   </Response>
   ```

- `ProceedStat` - request to get quantity of proceeded messages.
   
   An example of a request:
   ```xml
   <ProceedStat RequestID="30"/>
   ```

   An example of a response:
   ```xml
   <Response ResultCode="0" RequestID="30">
       <ProceedStatData>
           <ProceedMessages>173304</ProceedMessages>
       </ProceedStatData>
   </Response>
   ```

### Administrative commands

- `CreateInitiator` - request to create a FIX session with an initiator role.

   An example of a request:
   ```xml
   <CreateInitiator RequestID="46">
       <SenderCompID>TEST</SenderCompID>
       <TargetCompID>TEST2</TargetCompID>
       <Version>FIX44</Version>
       <RemoteHost>localhost</RemoteHost>
       <RemotePort>3000</RemotePort>
       <ExtraSessionParams>
           <SenderSubID>TEST_SUB</SenderSubID>
           <TargetSubID>TEST2_SUB</TargetSubID>
           <HBI>30</HBI>
           <StorageType>PERSISTENT</StorageType>
           <MaxMessagesAmountInBunch>100</MaxMessagesAmountInBunch>
           <EnableMessageRejecting>false</EnableMessageRejecting>
           <SocketOpPriority>EVEN</SocketOpPriority>
           <ForcedReconnect>false</ForcedReconnect>
           <DisableTCPBuffer>false</DisableTCPBuffer>
           <ForceSeqNumReset>ON</ForceSeqNumReset>
           <IntradayLogoutToleranceMode>false</IntradayLogoutToleranceMode>
           <IgnoreSeqNumTooLowAtLogon>false</IgnoreSeqNumTooLowAtLogon>
           <EncryptMethod>NONE</EncryptMethod>
       </ExtraSessionParams>
   </CreateInitiator>
   ```

   An example of a response:
   ```xml
   <Response ResultCode="0" RequestID="46">
       <Description>CreateInitiator completed</Description>
   </Response>
   ```

- `ToBackup` - request to switch the FIX session to backup connection.

   An example of a request:
   ```xml
   <ToBackup RequestID="35">
       <SenderCompID>TestSender</SenderCompID>
       <TargetCompID>TestTarget</TargetCompID>
   </ToBackup>
   ```

   An example of a response:
   ```xml
   <Response ResultCode="0" RequestID="35">
       <Description>ToBackup completed</Description>
   </Response>
   ```

- `ToPrimary` - request to switch the FIX session back to primary connection.

   An example of a request:
   ```xml
   <ToPrimary RequestID="35">
       <SenderCompID>TestSender</SenderCompID>
       <TargetCompID>TestTarget</TargetCompID>
   </ToPrimary>
   ```

   An example of a response:
   ```xml
   <Response ResultCode="0" RequestID="35">
       <Description>ToPrimary completed</Description>
   </Response>
   ```

- `Delete` - request to delete the FIX session.
   
   An example of a request:
   ```xml
   <Delete RequestID="35">
       <SenderCompID>TestSender</SenderCompID>
       <TargetCompID>TestTarget</TargetCompID>
       <SendLogout>true</SendLogout>
       <LogoutReason>Evacuation</LogoutReason>
   </Delete>
   ```

   An example of a response:
   ```xml
   <Response ResultCode="0" RequestID="35">
       <Description>Delete completed</Description>
   </Response>
   ```

- `DeleteAll` - request to delete all FIX sessions.
   
   An example of a request:
   ```xml
   <DeleteAll RequestID="95">
       <SendLogout>true</SendLogout>
       <LogoutReason>Evacuation</LogoutReason>
   </DeleteAll>
   ```

   An example of a response:
   ```xml
   <Response ResultCode="0" RequestID="95">
       <Description>DeleteAll completed</Description>
   </Response>
   ```
### Other commands

- `ChangeSeqNum` - request to change a session's sequence number.
   
   An example of a request:
   ```xml
   <ChangeSeqNum RequestID="45">
       <SenderCompID>TestSender</SenderCompID>
       <TargetCompID>TestTarget</TargetCompID>
       <InSeqNum>100</InSeqNum>
       <OutSeqNum>100</OutSeqNum>
   </ChangeSeqNum>
   ```

   An example of a response:
   ```xml
   <Response ResultCode="0" RequestID="45">
       <Description>ChangeSeqNum completed</Description>
   </Response>
   ```

- `ResetSeqNum` - request to reset a session's sequence number (force seqnum reset logon).
   
   An example of a request:
   ```xml
   <ResetSeqNum RequestID="55">
       <SenderCompID>TestSender</SenderCompID>
       <TargetCompID>TestTarget</TargetCompID>
   </ResetSeqNum>
   ```

   An example of a response:
   ```xml
   <Response ResultCode="0" RequestID="55">
       <Description>ResetSeqNum completed</Description>
   </Response>
   ```

- `TestRequest` - request to send a Test Request message to the session.
   
   An example of a request:
   ```xml
   <TestRequest RequestID="65">
       <SenderCompID>TestSender</SenderCompID>
       <TargetCompID>TestTarget</TargetCompID>
       <TestReqID>12345576</TestReqID>
   </TestRequest>
   ```

   An example of a response:
   ```xml
   <Response ResultCode="0" RequestID="65">
       <Description>TestRequest completed</Description>
   </Response>
   ```

- `Heartbeat` - request to send a Heartbeat message to the session.
   
   An example of a request:
   ```xml
   <Heartbeat RequestID="75">
       <SenderCompID>TestSender</SenderCompID>
       <TargetCompID>TestTarget</TargetCompID>
   </Heartbeat>
   ```

   An example of a response:
   ```xml
   <Response ResultCode="0" RequestID="75">
       <Description>"beat"</Description>
   </Response>
   ```

- `SendMessage` - request to send a message to the session.
   
   An example of a request:
   ```xml
   <SendMessage RequestID="85">
       <SenderCompID>TestSender</SenderCompID>
       <TargetCompID>TestTarget</TargetCompID>
       <Message>8=FIX.4.29=05635=849=THEM56=US37=117=134=53011=00492-0476150=039=010=161</Message>
   </SendMessage>
   ```

   An example of a response:
   ```xml
   <Response ResultCode="0" RequestID="85">
       <Description>SendMessage completed</Description>
   </Response>
   ```

- `Help` - request to get a list of supported commands.
   An example of a request:
   ```xml
   <Help RequestID="100"/>
   ```

   An example of a response:
   ```xml
   <Response ResultCode="0" RequestID="100">
       <HelpData>
           ...
       </HelpData>
   </Response>
   ```
