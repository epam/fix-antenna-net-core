# Configuration

FIX Antenna provides a few ways for configuring its behavior. The global configuration is defined in the default
configuration file: *fixengine.properties*. It’s also possible to define and load custom configuration for sessions from a
separate file.

## Global configuration
Antenna loads global settings from a configuration file with a fixed name: *fixengine.properties*. This file should be placed in one of the following places:
1. in a custom directory configured via `Config.ConfigurationDirectory`
2. in the application working directory
3. in the user home directory
4. inside the library as an embedded resource

Global settings could include server behavior and/or default behavior of sessions.

### Server behavior

The table below specifies the server behavior settings.

<table class="config" border="1" cellpadding="2" cellspacing="0">
<tbody>
<tr class="header">
    <td><b>Property Name</b></td>
    <td><b>Default Value</b></td>
    <td><b>Description</b></td>
</tr>
<tr>
    <td>performResetSeqNumTime</td>
    <td>false</td>
    <td>This parameter specifies whether to reset a sequence number at the time defined in <b>resetSequenceTime</b>.
    </td>
</tr>
<tr>
    <td>switchOffSendingMultipleResendRequests</td>
    <td>false</td>
    <td>This parameter specifies whether to send multiple RR for the same gap or not.
    </td>
</tr>
<tr>
    <td>resetSequenceTime</td>
    <td>00:00:00</td>
    <td>This parameter specifies the GMT time when the FIX Engine initiates the reset.
    </td>
</tr>
<tr>
    <td>resetSequenceTimeZone</td>
    <td>UTC</td>
    <td>Time zone id for resetSequenceTime property.
    More information about time zone format:
    <a href="https://docs.microsoft.com/en-us/dotnet/api/system.timezoneinfo.id?view=netstandard-2.0#System_TimeZoneInfo_Id">System.TimeZoneInfo.Id</a>
    </td>
</tr>
<tr>
    <td>intraDaySeqNumReset</td>
    <td>false</td>
    <td>This parameter specifies whether to reset a sequence number after a session is closed.<br/>
        Valid values: true | false.<br/>
        Default value: false.
    </td>
</tr>
<tr>
    <td>encryptionConfig</td>
    <td>${fa.home}/</br>encryption/</br>encryption.cfg</td>
    <td>This parameter specifies the encryption config file name.
        Valid values: existent valid config file name (relative or absolute path).
    </td>
</tr>
<tr>
    <td>encryptionMode</td>
    <td>None</td>
    <td>This parameter specifies the default value of <b>encryptionMode</b>.<br/>
        Valid values: None | Des | PgpDesMd5.<br/>
        Default value: None
    </td>
</tr>
<tr>
    <td>autoreconnectAttempts</td>
    <td>-1</td>
    <td>Specifies the number of autoreconnect attempts:
        <ul>
            <li>negative number - no reconnects</li>
            <li>0 - infinite number of reconnects</li>
            <li>positive number - number of reconnect attempts</li>
        </ul>
        Please use 0 wisely - it means attempting to reconnect infinitely.
    </td>
</tr>
<tr>
    <td>autoreconnectDelayInMs</td>
    <td>1000</td>
    <td>Specifies a delay between autoreconnect attempts in
        milliseconds, the default value is 1000ms.<br>
        The parameter must be an integer and not negative. Otherwise,
        the default value for this parameter will be used.
    </td>
</tr>
<tr>
    <td>loginWaitTimeout</td>
    <td>5000</td>
    <td>Sets
        the timeout interval after which a connected acceptor session will be timed
        out and disposed if the Logon is not received for this session.<br>
        The parameter must be an integer and not negative.
        Otherwise, the default value for this parameter will be used.
    </td>
</tr>
<tr>
    <td>logoutWaitTimeout</td>
    <td></td>
    <td>Sets disconnect timeout in seconds for the logout.<br>
        The parameter must be an integer and not negative. 
        Otherwise, the session's HeartbeatInterval will be used.
    </td>
</tr>
<tr>
    <td>handleSeqNumAtLogon</td>
    <td>false</td>
    <td>This parameter specifies whether to process the <b>789-NextExpectedMsgSeqNum tag</b>.
        If true, the outgoing sequence number must be updated by the <b>789-NextExpectedMsgSeqNum tag value</b>.
    </td>
</tr>
<tr>
    <td>disconnectOnLogonHeartbeatMismatch</td>
    <td>true</td>
    <td>Checks and disconnects a session if the Logon answer contains a HeartBtInt(108) value other than one defined in the session
        configuration.
    </td>
</tr>
<tr>
    <td>forcedLogoffTimeout</td>
    <td>2</td>
    <td>Sets the disconnect timeout in seconds for a Logout ack only when
        waiting.<br>
        The Logout ack from the counterparty is caused by the incoming sequence number being
        less than expected. The parameter must be an integer and not negative.
        Otherwise, the standard value for this parameter will be used.
    </td>
</tr>
<tr>
    <td>sendRejectIfApplicationIsNotAvailable</td>
    <td>true</td>
    <td>Sends a reject if a user application
        is not available. If the value is false and the client applicaiton is not available, acts
        like a "black hole" - accepts and ignores all valid messages.
    </td>
</tr>
<tr>
    <td>rawTags</td>
    <td>96, 91, 213, 349, 351, 353, 355, 357, </br> 359, 361, 363, 365, 446, 619, 622
    </td>
    <td>Raw tags. <br>
        Lists all tags that the Engine should treat as raw. A raw tag may contain a SOH
        symbol inside it and should be preceded by the <b>rawTagLength</b> field.
    </td>
</tr>
<tr>
    <td>maskedTags</td>
    <td>554, 925
    </td>
    <td>Masked tags. <br>
        List of tags whose values are masked by the Engine when used in in/out log files.
    </td>
</tr>
<tr>
    <td>resendRequestNumberOfMessagesLimit</td>
    <td>0</td>
    <td>Limits the maximum number of messages during the resend request.
        If more messages are requested, the reject will be sent in response.<br>
        The parameter must be an integer and not negative. 
        Otherwise, the default value for this parameter will be used.
    </td>
</tr>
<tr>
    <td>maxRequestResendInBlock</td>
    <td>0</td>
    <td>The max requested messages in block. This parameter defines how many messages
        will be requested in one block.
        The value must be an integer and not less than 0.
    </td>
</tr>
<tr>
    <td>testRequestsNumberUponDisconnection</td>
    <td>1</td>
    <td>This parameter specifies number of Test Request messages that will be sent before connection loss is reported
        when no messages are received from the counterparty.
        Valid values: positive integer.
    </td>
</tr>
<tr>
    <td>advancedResendRequestProcessing</td>
    <td>false</td>
    <td>This parameter specifies whether to subsequently issue  uplicates (PossDupFlag(43) = 'Y') of the last Resend Request
        for continuing gaps resting on the <b>LastMsgSeqNumProcessed(369)</b> field values of incoming messages.
        The counterparty must then respond only to the original request or a subsequent duplicate Resend Request if it
        missed
        the original. The duplicate(s), otherwise, can be discarded, as it does not have a unique message sequence
        number of its own.
        Valid values: true | false
    </td>
</tr>
<tr>
    <td>skipDuplicatedResendRequests</td>
    <td>false</td>
    <td>This parameter specifies whether to respond only to the original request or a subsequent duplicate Resend Request
        if it missed the original.
        If this option is disabled, FA will respond to any Resend Request.
        Valid values: true | false
    </td>
</tr>
<tr>
    <td>serverAcceptorStrategy</td>
    <td>Epam.FixAntenna.NetCore.FixEngine.Acceptor.</br>AllowNonRegisteredAcceptor</br>StrategyHandler</td>
    <td>This parameter specifies the default Acceptor Strategy.
        Valid values: subclasses of
        <ul><li>Epam.FixAntenna.NetCore.FixEngine.Acceptor.SessionAcceptorStrategyHandler</li>
        <li>Epam.FixAntenna.NetCore.FixEngine.Acceptor.AllowNonRegisteredAcceptorStrategyHandler</li>
        <li>Epam.FixAntenna.NetCore.FixEngine.Acceptor.DenyNonRegisteredAcceptorStrategyHandler</li>
    </td>
</tr>
<tr>
    <td>possDupSmartDelivery</td>
    <td>false</td>
    <td>This parameter enables the delivery of only those <b>PossDup</b> messages that weren't received previously, discarding
        already processed possDups.
        Valid values: true | false
    </td>
</tr>
<tr>
    <td>maxMessageSize</td>
    <td>1Mb</td>
    <td>Maximum message size supported
        by this FIX engine instance.<br>
        The parameter must be an integer and not negative. Otherwise, the default value for this parameter will be used.
        Should be set to a greater than expected maximum message by approximately 1-5%.
        <ul>
            <li>positive number - maximum allowed size of incoming message</li>
            <li>0 - any size message allowed (not recommended, could lead to OutOfMemoryError if the counterparty sends an
                invalid stream)
            </li>
        </ul>
    </td>
</tr>
<tr>
    <td>includeLastProcessed</td>
    <td>false</td>
    <td>Include the last processed sequence
        369 tag in every message for FIX versions &gt; 4.2.
    </td>
</tr>
<tr>
    <td>enableMessageRejecting</td>
    <td>false</td>
    <td>Enable/disable message rejecting.
        If this feature is enabled, and during session closing there are messages in its output queue, they
        will be passed to the session's `IRejectMessageListener`
        and the queue will be cleaned. If this feature is disabled, messages will be left in the queue and
        processed according to queue behavior.
    </td>
</tr>
<tr>
    <td>enableMessageStatistic</td>
    <td>true</td>
    <td>Enable/disable message statistics, such as the number of messages and bytes that were read and sent.
    </td>
</tr>
<tr>
    <td>enableNagle</td>
    <td>true</td>
    <td>Disable/enable Nagle's algorithm for TCP sockets. This option has the opposite meaning of the <b>TcpNoDelay</b> socket
        option.
        When enabled, Nagle's algorithm will be better throughput (TcpNoDelay=false). When disabled, you will
        get a better result for latency on a single message (TcpNoDelay=true)<br/>
        Default value is <b>true</b>.
    </td>
</tr>
 <tr>
    <td>recvCpuAffinity</td>
    <td>-1</td>
    <td>This parameter specifies a cpu id for the threads of a session that receives data from socket.
    </td>
</tr>
<tr>
    <td>sendCpuAffinity</td>
    <td>-1</td>
    <td>This parameter specifies a cpu id for the threads of a session that sends data in socket.
    </td>
</tr>
<tr>
    <td>cpuAffinity</td>
    <td>-1</td>
    <td>This parameter specifies a cpu id for the threads of a session that sends and receives data from/in socket.
    </td>
</tr>
<tr>
    <td>connectAddress</td>
    <td></td>
    <td>
        The engine's local IP address to send from. It can be used on a multi-homed host
        for a FIX Engine that will only send IP datagrams from one of its addresses.<br/>
        If this parameter is commented, the engine will send IP datagrams from any/all
        local addresses.
    </td>
</tr>
<tr>
    <td>resetOnSwitchToBackup</td>
    <td>false</td>
    <td>Reset sequences on switch to backup.</td>
</tr>
<tr>
    <td>resetOnSwitchToPrimary</td>
    <td>false</td>
    <td>Reset sequences on switch back to primary connection.</td>
</tr>
<tr>
    <td>enableAutoSwitchToBackupConnection</td>
    <td>true</td>
    <td>Enable auto switching to backup
        connection, the default value is <b>true</b>.
    </td>
</tr>
<tr>
    <td>cyclicSwitchBackupConnection</td>
    <td>true</td>
    <td>Enable switching to primary connection, the default value is <b>true</b>.</td>
</tr>
<tr>
    <td>forceSeqNumReset</td>
    <td>Never</td>
    <td>This parameter allows automatically resolving the sequence gap problem (for example, when there is a
        sequence reset every day). Supported values: Always, OneTime, Never.<br/>
        If this parameter is set to:
        <ul>
            <li>Always - the session will send logon with 34= 1 and 141=Y every time (during connection and
                reconnection).
            </li>
            <li>OneTime - the session will send logon with 34= 1 and 141=Y only one time (during connection).</li>
            <li>Never - the user can set 34= 1 and 141=Y from the session parameters by hand.</li>
        </ul>
    </td>
</tr>
<tr>
    <td>ignorePossDupForGapFill</td>
    <td>true</td>
    <td>Enable this option if you need to handle a <b>SequenceReset-GapFill</b> message without <b>PossDupFlag(43)</b>.
        This option also allows ignoring the absence of <b>OrigSendingTime(122)</b> in that kind of message.<br>
        Valid values: true | false
    </td>
</tr>
<tr>
    <td>checkSendingTimeAccuracy</td>
    <td>true</td>
    <td>Toggle the check of <b>SendingTime(52)</b> accuracy for received messages on/off.
    </td>
</tr>
<tr>
    <td>reasonableDelayInMs</td>
    <td>120000</td>
    <td>Sending time delay for incoming messages.<br>
        The parameter must be an integer and not negative.
        Otherwise, the default value for this parameter will be used.
    </td>
</tr>
<tr>
    <td>heartbeatReasonableTransmissionTime</td>
    <td>200</td>
    <td> This parameter specifies "some reasonable transmission time" of the FIX specification, measured in
        milliseconds.<br/>
        Valid values: positive integer
    </td>
</tr>
<tr>
    <td>measurementAccuracyInMs</td>
    <td>1</td>
    <td>Measurement accuracy for sending time.<br>
        The parameter must be an integer and not negative.
        Otherwise, the standard value for the parameter will be used.
    </td>
</tr>
<tr>
    <td>timestampsPrecisionInTags</td>
    <td>Milli</td>
    <td>The desired precision of timestamps in appropriate tags of the FIX message.<br>
        Valid values: Second | Milli | Micro | Nano.
    </td>
</tr>
<tr>
    <td>allowedSecondsFractionsForFIX40</td>
    <td>false</td>
    <td>If enabled, use the timestamp with precision defined by the <b>timestampsPrecisionInTags</b> option for FIX 4.0.
    </td>
</tr>
<tr>
    <td>preferredSendingMode</td>
    <td>sync</td>
    <td>This parameter specifies the way the session will send most of its messages:</br>
        <ul>
            <li>Async - the session will send all messages asynchronously, in separate threads. The client thread will be released
                as fast as possible.
            </li>
            <li>Sync - the session will be optimized to send messages from the user thread synchronously, but it still can make asynchronous
                operations and allows sending messages to the internal queue. Synchronous sending
                is faster, but tge user thread can be affected by delays on network writing.
            </li>
            <li>SyncNoqueue - the session sends messages only from the user thread and doesn't use the internal queue. It's impossible to
                send messages to a disconnected session.
            </li>
        </ul>
    </td>
</tr>
<tr>
    <td>waitForMsgQueuingDelay</td>
    <td>1000</td>
    <td>This parameter specifies the maximum delay interval on message sending if the internal session queue is
        full.</br>
        If the internal session's queue is full, FIX Antenna pauses sending the thread until the message
        pumper thread sends some messages and frees some space in the queue. If after the delay, the interval queue is still full,
        the message will be pushed to the queue anyway.</br>
        Valid values: positive integer
    </td>
</tr>
<tr>
    <td>ignoreSeqNumTooLowAtLogon</td>
    <td>false</td>
    <td>This parameter allows resolving a wrong incoming sequence at Logon.
        When set to <b>true</b> the session continues with the received <b>seqNum</b>.
    </td>
</tr>
<tr>
    <td>resetQueueOnLowSequence</td>
    <td>true</td>
    <td>When disabled, prevents outgoing queue reset if the client is connecting with a lower than expected sequence number.
        Once a session is reestablished, queued messages will be sent out.<br/>
        Please note that queued messages won't be sent out until the session is fully established regardless of this parameter.
    </td>
</tr>
<tr>
    <td>quietLogonMode</td>
    <td>false</td>
    <td>Enable this option if you need to quietly handle Logout as the first session message.
        FIX Specification requires that first message is a Logon message. Otherwise, the Logout
        message must contain the warning "First message is not logon". Additionally, sоmetimes the first incoming Logout message has the wrong sequence
        (for example, if you send a Logon with 141=Y). This option allows skiping sending a ResendRequest and warning
        to the counterparty.
    </td>
</tr>
<tr>
    <td>allowedCountOfSimilarRR</td>
    <td>3</td>
    <td>This option indicates how many similar ResendRequests (for the same range of sequences) the engine may send before
        detecting a possible infinite resend loop. This should prevent an infinite loop from requesting the same
        corrupted messages many times or if the user logic can't correctly handle a message and throws an exception every time.<br/>
        The parameter must be an integer and not negative. Otherwise, the standard value for this parameter will be used.
    </td>
</tr>
<tr>
    <td>throttleCheckingEnabled</td>
    <td>false</td>
    <td>Enables throttling checks per message type.<br/>
        If this option is enabled, the engine counts how many times a session receives messages with a given message type during 'throttleCheckingPeriod'.
        If this counter is greater than the value in throttleChecking.MSG_TYPE.threshold, the session will be
        disconnected with the reason: THROTTLING.
    </td>
</tr>
<tr>
    <td>throttleCheckingPeriod</td>
    <td>1000</td>
    <td>Defines a period common for all throttling per message type checks.<br/>
        Default value: 1000 milliseconds
    </td>
</tr>
<tr>
    <td>throttleChecking.MsgType.threshold</td>
    <td>-1</td>
    <td>Allowed number of messages with MsgType type for a given 'throttleCheckingPeriod' period.</br>
        Valid values: positive integer<br/>
        Default value: -1 (disabled)
    </td>
</tr>
<tr>
    <td>resetThreshold</td>
    <td>0</td>
    <td>This parameter specifies a gap in sequences during connecting that may be treated as missed sequence reset
        event by the counterparty. It works if the current session has reset sequences and expects a message (Logon)
        with 34=1 but the counterparty is still sending messages with much higher sequences (because they didn't do a reset on
        their side).<br/>
        This option helps control bidirectional agreed sequence reset events and prevents requesting old
        messages.<br/>
        This option only works for acceptor sessions.<br/>
        Default value is 0, which means the check is not going to be performed.
    </td>
</tr>
<tr>
    <td>slowConsumerDetectionEnabled</td>
    <td>false</td>
    <td>This parameter enables slow consumer detection during message sending.<br/>
        Default value: false (disabled)
    </td>
</tr>

<tr>
    <td>slowConsumerWriteDelayThreshold</td>
    <td>10</td>
    <td>This parameter is used for decision making in slow consumer detection in pumpers.<br/>
        It defines a maximum timeframe for sending a message. If the session transport can't send a message during this
        timeframe, it will notify about a slow consumer.<br/>
        Default value: 10 (milliseconds)
    </td>
</tr>
<tr>
    <td>system.messagehandler.global.X</td>
    <td>Set of predefined handlers</td>
    <td>Sets global message handler(s). A handler will be called upon for each and every incoming message.<br>
        You can define your own set of global message handlers, but you should be very careful. The default set of
        handlers
        provides integrity checks like sending time accuracy, sequencing, visioning, validation.<br>
        NOTE: Handlers will be applied in reverse order: a handler with a bigger number will be applied first.<br>
        NOTE: Handler numbers should be unique and sequential.<br>
        NOTE: Handlers can prevent further message processing.
    </td>
</tr>
<tr>
    <td>user.messagehandler.global.X</td>
    <td>&nbsp;</td>
    <td>User supplied handlers.<br>
        This set of handlers will be applied after system handlers for every incoming message (if it does not fall out).<br>
        NOTE: Handlers will be applied in reverse order: a handler with a bigger number will be applied first.<br>
        NOTE: Handler numbers should be unique and sequential.<br>
        NOTE: Handlers can prevent further message processing.
        Example: You can enable the <b>DeliverToCompId</b> message handler for 3rd Party Message routing if needed: <br>
        user.messagehandler.global.0=Epam.FixAntenna.NetCore.FixEngine.Session.MessageHandler.User.DeliverToCompIdMessageHandler
    </td>
</tr>
<tr>
    <td>system.messagehandler.MsgType</td>
    <td>Set of predefined handlers</td>
    <td>Sets the handler per message type that will be called upon for each and every incoming message.<br>
        This set of properties is for redefining engine behavior. You can change the default processing for the following session
        message types: Logon(A), Heartbeat(0), Test Request(1), Resend Request(2), Reject(3), Sequence Reset(4),
        Logout(5). <br>
        NOTE: Handler numbers should be unique and sequential. These handlers will be applied after system and user
        defined global message handlers.<br>
        NOTE: Handlers can prevent further message processing.<br>
        Example:<br><br> <code>system.messagehandler.3=MyCompany.Custom.RejectMessageHandler</code>
    </td>
</tr>
<tr>
    <td>autostart.acceptor.targetIds</td>
    <td>&nbsp;</td>
    <td>Comma separated list of <b>TargetCompID</b> for automatic accepting sessions on the server.<br>
        For each of the sessions below, other parameters such as login, password, source IP filter and
        listener should be set.<br>
        IP filter could be defined as "*", which means that connection from any address is allowed. Or this
        property can contain a comma separated list of allowed IPs of subnet masks like 192.168.0.1/16.<br>
        For example, you can enable the administrative plugin in the following way:<br><br>
        <code>autostart.acceptor.targetIds=admin<br>
        autostart.acceptor.admin.login=admin<br>
        autostart.acceptor.admin.password=admin<br>
        autostart.acceptor.admin.ip=*<br>
        autostart.acceptor.admin.fixServerListener=Epam.FixAntenna.AdminTool.AdminTool,Epam.FixAntenna.AdminTool<br>
        </code>
    </td>
</tr>
<tr>
    <td>autostart.acceptor.TARGET_ID.login</td>
    <td>&nbsp;</td>
    <td>Username(553) for automatic session acceptance.</td>
</tr>
<tr>
    <td>autostart.acceptor.TARGET_ID.password</td>
    <td>&nbsp;</td>
    <td>Password(554) for automatic session acceptance.</td>
</tr>
<tr>
    <td>autostart.acceptor.TARGET_ID.ip</td>
    <td>&nbsp;</td>
    <td>Source IP for automatic session acceptance or "*" if any applicable.</td>
</tr>
<tr>
    <td>autostart.acceptor.TARGET_ID.fixServerListener</td>
    <td>&nbsp;</td>
    <td>Implementation of `IFixServerListener` for
        accepting the given session.
    </td>
</tr>
<tr>
    <td>tcpSendBufferSize</td>
    <td>0</td>
    <td>This parameter specifies value for <code>System.Net.Sockets.Socket.SendBufferSize</code> option<br/>
        The default value is 0 and it means that the parameter is not specified and the option 
        <code>System.Net.Sockets.Socket.SendBufferSize</code> will not be changed.
    </td>
</tr>
<tr>
    <td>tcpReceiveBufferSize</td>
    <td>0</td>
    <td>This parameter specifies the value for <code>System.Net.Sockets.Socket.ReceiveBufferSize</code> option<br/>
        The default value is 0 and it means that the parameter is not specified and the option 
        <code>System.Net.Sockets.Socket.ReceiveBufferSize</code> will not be changed.
    </td>
</tr>
<tr>
    <td>tradePeriodBegin</td>
    <td></td>
    <td>Cron expression to set the start of the period when the server is allowed to accept a connection.<br>
        We use Quartz.NET and their implementation of cron expressions for scheduling. See
        <a href="https://www.quartz-scheduler.net/documentation/quartz-3.x/tutorial/crontrigger.html#introduction">cron expression</a> for more information about the allowed expression format.<br>
        It is possible to combine several cron expressions with the "|" symbol.<br>
        Example: 0 0 8 1/2 * ?|0 0 9 2/2 * ?
    </td>
</tr>
<tr>
    <td>tradePeriodEnd</td>
    <td></td>
    <td>Cron expression to set the end of the period when the server is allowed to accept a connection.<br>
        It is possible to combine several cron expressions with the "|" symbol.
    </td>
</tr>
<tr>
    <td>tradePeriodTimeZone</td>
    <td>UTC</td>
    <td>Specifies the time zone and affects the tradePeriodBegin and tradePeriodEnd.
        More information about time zone format:
        <a href="https://docs.microsoft.com/en-us/dotnet/api/system.timezoneinfo.id?view=netstandard-2.0#System_TimeZoneInfo_Id">System.TimeZoneInfo.Id</a><br>
    </td>
</tr>
<tr>
    <td>resetSeqNumFromFirstLogon</td>
    <td>Never</td>
    <td>Determines if sequence numbers should be accepted from the incoming Logon message. The option allows to reduce
        miscommunication between sides and easier connect after scheduled sequence reset.<br>
        The option doesn’t change behavior if the Logon message contains ResetSeqNumFlag(141) equals to “Y” (in this
        case session sequence numbers will be reset).<br>
        The value ‘Schedule’ allows to adopt to the sequence numbers from the incoming Logon message if the reset time
        is outdated (the session recovers after scheduled reset time). In this case session’s incoming sequence number
        will be set to the value of MsgSeqNum(34) tag from the incoming Logon and outgoing sequence number become
        equivalent to NextExpectedMsgSeqNum (789) tag value (if the tag is present) or will be reset to 1.<br>
        Valid values: Never | Schedule
    </td>
</tr>
</tbody>
</table>

### Queue and storage

The table below specifies the queue and storage settings.

<table class="config" border="1" cellpadding="2" cellspacing="0">
<tbody>
<tr>
    <td>Property Name</td>
    <td>Default Value</td>
    <td>Description</td>
</tr>
<tr>
    <td>storageFactory</td>
    <td>Epam.FixAntenna.NetCore.FixEngine.Storage.</br>FilesystemStorageFactory</td>
    <td>Allows a user to replace the storage factory with the user's own implementation. <br>
        FIX Antenna provides 4 implementations:
        <ul>
            <li>
                FilesystemStorageFactory
                - factory with persistent storage, all messages will be saved on disk
            </li>
            <li>
                SlicedFileStorageFactory
                - factory with persistent storage and possibility to set max file size (use the <b>maxStorageSliceSize</b> property)
            </li>
            <li>MmfStorageFactory
                - factory with persistent file storage (using the memory mapped files technology)
            </li>
            <li>
                InMemoryStorageFactory
                - provides fast in-memory storage, but during session restart all messages will be lost
            </li>
        </ul>
    </td>
</tr>
<tr>
    <td>queueThresholdSize</td>
    <td>0</td>
    <td>Maximum number of messages in a
        queue before the pumper thread is paused to let the queued message be sent
        out.<br>
        <ul>
            <li>Set rather high for max performance.</li>
            <li>Set 1 or pretty low for realtime experience.</li>
            <li>0 - disable queue control, do not pause the pumper thread.</li>
        </ul>
        The parameter must be an integer and not negative.
        Otherwise, the default value for this parameter will be used.
    </td>
</tr>
<tr>
    <td>maxMessagesToSendInBatch</td>
    <td>10</td>
    <td>The maximum number of messages in the buffer before a message is written to transport.<br/>
        NOTE: Value for this property should be always > 0.
    </td>
</tr>
<tr>
    <td>inMemoryQueue</td>
    <td>false</td>
    <td>Sets queue mode. <br>
        This property makes sense only if <b>FilesystemStorageFactory</b> or <b>MmfStorageFactory</b> is set. <br>
        Set to "false" for a persistent queue (slower but no messages will be
        lost), "true" for in-memory queue (faster but less safe, some messages
        may be lost).
    </td>
</tr>
<tr>
    <td>memoryMappedQueue</td>
    <td>true</td>
    <td>Sets persistent queue mode for <b>MmfStorageFactory</b>.<br>
        This property makes sense only if <b>MmfStorageFactory</b> is set.<br>
        Set to "false" for persistent queue (slower but no messages will be lost),
        "true" for memory mapped queue (faster but less safe, some messages may be lost)
    </td>
</tr>
<tr>
    <td>incomingStorageIndexed</td>
    <td>false</td>
    <td>Incoming storage index.<br>
        This property makes sense only if the file storage is set. <br>
        Enabled index - messages in incoming storage will be available via API.
    </td>
</tr>
<tr>
    <td>outgoingStorageIndexed</td>
    <td>true</td>
    <td>Outgoing storage index.<br>
        This property makes sense only if the file storage is set. <br>
        Set to "true" to enable outgoing storage index that is to be used in
        decision making in the resend request handler. Enabled index - support resend
        request, disabled - never resend messages and always send gap fill.
    </td>
</tr>
<tr>
    <td>maxStorageSliceSize</td>
    <td>100Mb</td>
    <td>Specifies the maximum size of the storage file after
        which the engine creates a new storage file with a different name.<br/>
        Parameter must be integer and not negative.<br/>
        This property makes sense only if SlicedFileStorageFactory is set. <br>
        Default value: 100Mb.
    </td>
</tr>
<tr>
    <td>storageGrowSize</td>
    <td>false</td>
    <td>Enable/disable storage grow.<br/>
        Default value: false.<br/>
        This parameter and <b>maxStorageGrowSize</b> only work with a persistent session.
    </td>
</tr>
<tr>
    <td>maxStorageGrowSize</td>
    <td>1Mb</td>
    <td>Sets the maximum storage grow size in bytes.<br/>
        Parameter must be an integer and not negative.<br/>
        Default value: 1Mb.
    </td>
</tr>
<tr>
    <td>mmfStorageGrowSize</td>
    <td>100Mb</td>
    <td>Sets the storage grow size in bytes for memory mapped implementation.<br/>
        Parameter must be an integer and not negative.<br/>
        This property makes sense only if <b>MMFStorageFactory</b> is set. <br>
        Default value: 100Mb.
    </td>
</tr>
<tr>
    <td>mmfIndexGrowSize</td>
    <td>20Mb</td>
    <td>Sets the index grow size in bytes for memory mapped implementation.<br/>
        Only used for storage with a memory mapped index file.<br/>
        Parameter must be an integer and not negative.<br/>
        This property makes sense only if <b>MmfStorageFactory</b> is set
        and at least one of <b>incomingStorageIndexed</b> or <b>outgoingStorageIndexed</b> is true.<br>
        Default value: 20Mb.
    </td>
</tr>
<tr>
    <td>timestampsInLogs</td>
    <td>true</td>
    <td>Ability to write timestamps in the in/out log files.<br/>
        Default value: true.
    </td>
</tr>
<tr>
    <td>timestampsPrecisionInLogs</td>
    <td>Milli</td>
    <td>The desired pecision of timestamps in the in/out log files.<br/>
        Valid values: Milli | Micro | Nano.
    </td>
</tr>
<tr>
    <td>backupTimestampsPrecision</td>
    <td>Milli</td>
    <td>The desired pecision of timestamps in names of storage backup files.<br/>
        Valid values: Milli | Micro | Nano.
    </td>
</tr>
<tr>
    <td>logFilesTimeZone</td>
    <td>System time zone</td>
    <td>Specifies the time zone and affects the time stamp prefix in the FIX in/out logs.
        More information about time zone format:
        <a href="https://docs.microsoft.com/en-us/dotnet/api/system.timezoneinfo.id?view=netstandard-2.0#System_TimeZoneInfo_Id">System.TimeZoneInfo.Id</a>
        For sample: <b>logFilesTimeZone</b>=UTC
    </td>
</tr>
<tr>
    <td>storageDirectory</td>
    <td>logs</td>
    <td>Storage directory could be either an absolute path
        (like /tmp/logs or c:\fixengine\logs) or a relative
        path, e.g., logs (this one is relative to the application start
        directory).<br>
        This property makes sense only if file storage is set.
    </td>
</tr>
<tr>
    <td>storageCleanupMode</td>
    <td>None</td>
    <td>This parameter specifies the cleaning mode for message storage of closed sessions.<br/>
        Valid values: None | Backup | Delete.<br/>
        Default value: None.
    </td>
</tr>
<tr>
    <td>incomingLogFile</td>
    <td>{0}.in</td>
    <td>Incoming log filename template.<br>
        {0} will be replaced with the actual <b>sessionID</b>, {1} with the actual <b>SenderCompID</b>, {2} with the actual <b>TargetCompID</b>
        and {4} with the actual session qualifier.<br>
        This property makes sense only if file storage is set.
    </td>
</tr>
<tr>
    <td>outgoingLogFile</td>
    <td>{0}.out</td>
    <td>Outgoing log filename template.<br>
        {0} will be replaced with the actual <b>sessionID</b>, {1} with the actual <b>SenderCompID</b>, {2} with the actual <b>TargetCompID</b>
        and {4} with the actual session qualifier.<br>
        This property makes sense only if file storage is set.
    </td>
</tr>
<tr>
    <td>sessionInfoFile</td>
    <td>{0}.properties</td>
    <td>Info filename template.<br>
        {0} will be replaced with the actual <b>sessionID</b>, {1} with the actual <b>SenderCompID</b>, {2} with the actual <b>TargetCompID</b>
        and {4} with the actual session qualifier.<br>
        This property makes sense only if file storage is set.
    </td>
</tr>
<tr>
    <td>outgoingQueueFile</td>
    <td>{0}.outq</td>
    <td>Out queue file template.<br>
        {0} will be replaced with the actual <b>sessionID</b>, {1} with the actual <b>SenderCompID</b>, {2} with the actual <b>TargetCompID</b>
        and {4} with the actual session qualifier.<br>
        This property makes sense only if file storage is set.
    </td>
</tr>
<tr>
    <td>backupIncomingLogFile</td>
    <td>{0}-{3}.in</td>
    <td>Backup incoming log filename template.<br>
        {0} will be replaced with the actual <b>sessionID</b>, {1} with actual the <b>SenderCompID</b>, {2} with the actual <b>TargetCompID</b>, {3} with the timestamp
        and {4} with the actual session qualifier.<br>
        This property makes sense only if backup storage is set.
    </td>
</tr>
<tr>
    <td>backupOutgoingLogFile</td>
    <td>{0}-{3}.out</td>
    <td>Incoming log filename template.<br>
        {0} will be replaced with the actual <b>sessionID</b>, {1} with the actual <b>SenderCompID</b>, {2} with the actual <b>TargetCompID</b>, {3} with the timestamp
        and {4} with the actual session qualifier.<br>
        This property makes sense only if backup storage is set.
    </td>
</tr>
</tbody>
</table>

### Validation

The table below specifies the validation settings.

<table class="config" border="1" cellpadding="2" cellspacing="0">
    <tbody>
    <tr>
        <td>Property Name</td>
        <td>Default Value</td>
        <td>Description</td>
    </tr>
    <tr>
        <td>origSendingTimeChecking</td>
        <td>true</td>
        <td>This parameter specifies whether to check the <b>OrigSendingTime(122)</b> field value for incoming possible
            duplicated messages (<b>PossDupFlag(43)</b> = 'Y')<br>
        </td>
    </tr>
    <tr>
        <td>validateCheckSum</td>
        <td>true</td>
        <td>Toggle the validation of the <b>CheckSum(10)</b> field value for incoming messages on/off.<br>
            Is relevant only if <b>validateGarbledMessage</b>=true<br>
        </td>
    </tr>
    <tr>
        <td>markIncomingMessageTime</td>
        <td>false</td>
        <td>If this option is set to true, transport will set the additional time mark in nanoseconds for incoming messages right after read data 
        from socket.<br>
            <code>AbstractFixTransport.GetLastReadMessageTimeNano()</code> method could return this value.<br>
        </td>
    </tr>
    <tr>
        <td>validateGarbledMessage</td>
        <td>true</td>
        <td>Toggle the validation of garbled messages for incoming flow on/off<br>
            Validates the existence and order of the following fields:
            <b>BeginString(8)</b>, <b>BodyLength(9)</b>, <b>MsgType(35)</b>, <b>CheckSum(10)</b>.
            Also validates value of <b>BodyLength(9)</b>.<br>
        </td>
    </tr>
    <tr>
        <td>validation</td>
        <td>false</td>
        <td>Toggle the validation of incoming messages according to
            the base of custom dictionaries on/off.<br>
            The following parameters work only if this property set to "true".
        </td>
    </tr>
    <tr>
        <td>wellformenessValidation</td>
        <td>true</td>
        <td>Toggle the validation of fields with the tag values 8, 9, 35 and 10 on/off.<br>
            If "validation=false" then this parameter always reads as false.
        </td>
    </tr>
    <tr>
        <td>allowedFieldsValidation</td>
        <td>true</td>
        <td>Toggle the validation of allowed message fields on/off.<br>
            If "validation=false" then this parameter always reads as false.
        </td>
    </tr>
    <tr>
        <td>requiredFieldsValidation</td>
        <td>true</td>
        <td>Toggle the validation of required message fields on/off.<br>
            If "validation=false" then this parameter always reads as false.
        </td>
    </tr>
    <tr>
        <td>fieldOrderValidation</td>
        <td>true</td>
        <td>Toggle the validation of the order of fields in messages on/off. With this option, the engine will check that tags from the
            header, body, and trailer were not mixed up.<br>
            If "validation=false", this parameter always reads as false.
        </td>
    </tr>
    <tr>
        <td>duplicateFieldsValidation</td>
        <td>true</td>
        <td>Toggle the validation of duplicated message fields on/off.<br>
            If "validation=false", this parameter always reads as false.
        </td>
    </tr>
    <tr>
        <td>fieldTypeValidation</td>
        <td>true</td>
        <td>Toggle the validation of field values according to defined data types on/off.<br>
            If "validation=false", this parameter always reads as false.
        </td>
    </tr>
    <tr>
        <td>groupValidation</td>
        <td>true</td>
        <td>Toggle the validation of repeating group fields on/off.<br>
            If "validation=false", this parameter always reads as false.
        </td>
    </tr>
    <tr>
        <td>conditionalValidation</td>
        <td>true</td>
        <td>Conditional validation is very time consuming, so use it carefully.<br>
            If "validation=false", this parameter always reads as false.
        </td>
    </tr>
    <tr>
        <td>senderTargetIdConsistencyCheck</td>
        <td>true</td>
        <td>This parameter specifies the validation of values in tags <b>49(SenderCompID)</b>, <b>56(TargetCompID)</b>
            according to the same session parameters.<br/>
            If this property equals "false", this validation is disabled.
        </td>
    </tr>
    </tbody>
</table>

### Administrative plugin

The table below specifies the only setting of the Administrative plugin.

<table class="config" border="1" cellpadding="2" cellspacing="0">
    <tbody>
    <tr>
        <td>Property&nbsp; Name</td>
        <td>Default Value</td>
        <td>Description</td>
    </tr>
    <tr>
        <td>autostart.acceptor.command.package</td>
        <td>&nbsp;</td>
        <td>Name of the custom package for admin command processing. <br>
            This property is used for extending the count of admin-commands.
            By default, the package is null, but if custom commands are present, this
            property should be initialized.<br>
            Example:<br>
            <code>
                autostart.acceptor.command.package=Admin.Commands
            </code>
        </td>
    </tr>
    </tbody>
</table>

## Session configuration
In addition to the global settings, you can also define the behavior for a specific session or group of sessions.
The description of session’s configuration can be placed into fixengine.properties near global parameters or in a separate file.
Antenna supports two formats of session configuration – properties and XML.

Custom session configuration can override global settings for the session and also define additional parameters for session:
<table class="config" border="1" cellpadding="2" cellspacing="0">
    <tbody>
    <tr>
        <td>Property Name</td>
        <td>Default Value</td>
        <td>Description</td>
    </tr>
    <tr>
        <td>sessionType</td>
        <td></td>
        <td>Session type. In type not defined that session could be resolved in the same time as initiator and
            acceptor</br>
            Valid values: acceptor | initiator
        </td>
    </tr>
    <tr>
        <td>host</td>
        <td></td>
        <td>The connecting host for an initiator session.</td>
    </tr>
    <tr>
        <td>port</td>
        <td>The connecting port for an initiator session.</td>
        <td></td>
    </tr>
    <tr>
        <td>senderCompID</td>
        <td></td>
        <td>The <b>SenderCompID</b> for a FIX session.</td>
    </tr>
    <tr>
        <td>senderSubID</td>
        <td></td>
        <td>The <b>SenderSubID</b> for a FIX session.</td>
    </tr>
    <tr>
        <td>senderLocationID</td>
        <td></td>
        <td>The <b>SenderLocationID</b> for a FIX session.</td>
    </tr>
    <tr>
        <td>targetCompID</td>
        <td></td>
        <td>The <b>TargetCompID</b> for a FIX session.</td>
    </tr>
    <tr>
        <td>targetSubID</td>
        <td></td>
        <td>The <b>TargetSubID</b> for a FIX session.</td>
    </tr>
    <tr>
        <td>targetLocationID</td>
        <td></td>
        <td>The <b>TargetLocationID</b> for a FIX session.</td>
    </tr>
    <tr>
        <td>fixVersion</td>
        <td></td>
        <td>Identifies the beginning of a message and its protocol version(Tag = 8).</br>
            Valid values: FIX.4.0, FIX.4.1, FIX.4.2, FIX.4.3, FIX.4.4, FIXT.1.1</td>
    </tr>
    <tr>
        <td>appVersion</td>
        <td></td>
        <td>Application protocol version(Tag = 1128).</br>
            Valid values: FIX.4.0, FIX.4.1, FIX.4.2, FIX.4.3, FIX.4.4, FIX.5.0, FIX.5.0SP1, FIX.5.0SP2</td>
    </tr>
    <tr>
        <td>backupHost</td>
        <td></td>
        <td>The backup host for an initiator session.</td>
    </tr>
    <tr>
        <td>backupPort</td>
        <td></td>
        <td>The backup port for an initiator session.</td>
    </tr>
    <tr>
        <td>incomingSequenceNumber</td>
        <td>0</td>
        <td>Incoming sequence number.</td>
    </tr>
    <tr>
        <td>outgoingSequenceNumber</td>
        <td>0</td>
        <td>Outgoing sequence number.</td>
    </tr>
    <tr>
        <td>processedIncomingSequenceNumber</td>
        <td>0</td>
        <td>Last valid incoming sequence number.</td>
    </tr>
    <tr>
        <td>heartbeatInterval</td>
        <td>30</td>
        <td>Heartbeat interval.</td>
    </tr>
    <tr>
        <td>LastSeqNumResetTimestamp</td>
        <td></td>
        <td></td>
    </tr>
    <tr>
        <td>SeqNumLength</td>
        <td>1</td>
        <td>This parameter specifies the minimum length for the <b>SeqNum</b> fields.<br/>
            Valid values: intergers from 1 to 10.</td>
    </tr>
    <tr>
        <td>FixMessage</td>
        <td></td>
        <td>User defined fields for messages. If this list is not empty, the engine adds it to each outgoing message.</td>
    </tr>
    <tr>
        <td>outgoingLoginFixMessage</td>
        <td></td>
        <td>Additional fields for an outgoing Logon message.</td>
    </tr>
    <tr>
        <td>tradePeriodBegin</td>
        <td></td>
        <td>For initiator sessions, it is a cron expression to set a scheduled session start. 
            For acceptor sessions, the parameter overrides the appropriate server value for this session.<br>
            We use Quartz.NET and their implementation of cron expressions for scheduling. See
            <a href="https://www.quartz-scheduler.net/documentation/quartz-3.x/tutorial/crontrigger.html#introduction">cron expression</a> for more information about the allowed expression format.<br>
            It is possible to combine several cron expressions with the "|" symbol.<br>
            Example: 0 0 8 1/2 * ?|0 0 9 2/2 * ?
        </td>
    </tr>
    <tr>
        <td>tradePeriodEnd</td>
        <td></td>
        <td>For initiator sessions, it is a cron expression to set a scheduled session end. 
            For acceptor sessions, the parameter overrides the appropriate server value for this session.<br>
            It is possible to combine several cron expressions with the "|" symbol.
        </td>
    </tr>
    <tr>
        <td>tradePeriodTimeZone</td>
        <td>UTC</td>
        <td>Specifies the time zone and affects the tradePeriodBegin and tradePeriodEnd.
            More information about time zone format:
            <a href="https://docs.microsoft.com/en-us/dotnet/api/system.timezoneinfo.id?view=netstandard-2.0#System_TimeZoneInfo_Id">System.TimeZoneInfo.Id</a><br>
        </td>
    </tr>
</tbody>
</table>

### Configure SeqNum fields length
The user can set the length of the Sequence Number fields by using the <b>SeqNumLength</b> parameter.

This parameter is applied to the fields that are processed in the FA .NET library.

The field length can vary from 1 to 10 symbols. All fields of the <b>SeqNum</b> type are padded with leading zeros. The default value is “1”, which implies that no padding is applied.

If the <b>SeqNumLength</b> parameter is missing or a wrong value is specified, the default value is used.

The length of the following <b>SeqNum</b> fields can be configured:
<ul>
<li><b>StartSeqNo (7)</b></li>
<li><b>EndSeqNo (16)</b></li>
<li><b>MsgSeqNum (34)</b></li>
<li><b>NewSeqNo (36)</b></li>
<li><b>RefSeqNum (45)</b></li>
<li><b>LastMsgSeqNumProcessed (369)</b></li>
<li><b>HopRefID (630)</b></li>
<li><b>NextExpectedMsgSeqNum (789)</b></li>
</ul>

Example:
   ```INI
   seqNumLength=5
   ```
This setting produces the following formatting of the <b>SeqNum</b> field (tag 34):
   ```
   8=FIX.4.4 | 9=73 | 35=0 | 34=00002 | 49=ConnectToGateway | 56=EchoServer | 52=20210401-16:10:00.687 | 10=227 |
   ```

The FA .NET library regulates the length of these fields on its own, except for <b>HopRefID</b>.

The length of the <b>HopRefID (630)</b> is not regulated by the library. The length of the <b>HopRefID (630)</b> is up to user.

### Definition of a session’s configuration via the properties file
The description of a session’s configuration consists of a few sections:
1. The list of active sessions should be defined in the <b>sessionIDs</b> property. IDs should be separated with semicolon:
   ```INI
   sessionIDs=sessionID1,sessionID2
   ```

2. Default settings for all sessions in this file could be defined with the prefix «sessions.default.»:

   ```INI
   sessions.default.senderCompID=SCID
   sessions.default.storageFactory=Epam.FixAntenna.NetCore.FixEngine.Storage.InMemoryStorageFactory
   ```

3. To define a custom setting for a specified session, use properties with the `sessions. <SessionID>.` prefix format, where `<SessionID>` is an ID of this session:
   ```INI
   sessions.NYSE.targetCompID=NYSE
   sessions.NYSE.validation=true
   ```

### Definition of a session’s configuration via an XML file
The description of a session’s configuration consists of a few sections:
1. Active sessions should be defined inside the config/<b>sessionIDs</b> tag. IDs should be separated with semicolon:

   ```xml
   <config>
       <sessionIDs>sessionID1,sessionID2</sessionIDs>
       …
   </config>
   ```

2. Default settings for all sessions in this file could be defined inside the config/sessions/default tag:

   ```xml
   <config>
       …
       <sessions>
           <default>
               <senderCompID>SCID</senderCompID>
               <storageFactory>Epam.FixAntenna.NetCore.FixEngine.Storage.InMemoryStorageFactory</storageFactory>
           </default>
       </sessions>
       …
   </config>
   ```

3. The custom configuration for a specified session could be placed inside the tag config/sessions/ `<SessionID>`, where `<SessionID>` is an ID of this session:

   ```xml
   <config>
       …
       <sessions>
           <NYSE>
               <targetCompID>NYSE</targetCompID>
               <validation>true</validation>
           </NYSE>
       </sessions>
       …
   </config>
   ```

### Loading session configuration
Please use the methods of the `SessionParametersBuilder` class to load a session’s configuration:

```csharp
//extract configuration for session from config
SessionParameters details = SessionParametersBuilder.BuildSessionParameters("initiators.xml",
"sessionID1");

// create session we intend to work with
IFixSession session = details.CreateNewFixSession();
```

You can skip a parameter with the path to the configuration file if the session’s configuration is defined in the
default configuration file (fixengine.properties)

```csharp
//extract configuration for session from default config
SessionParameters details = SessionParametersBuilder.BuildSessionParameters("sessionID1");
IFixSession session = details.CreateNewFixSession();
```

Use the `FixServer.ConfigPath` property to register configured sessions in servers:
```csharp
FixServer server = new FixServer();
server.ConfigPath = "acceptor.properties";
```
FIXServer will register a session automatically if it is defined in the default Antenna configuration file
(fixengine.properties).

NOTE: Please make sure that the <b>SenderCompID</b> and <b>TargetCompId</b> parameters are defined for all acceptor
sessions. The server uses these parameters for resolving configuration for incoming sessions.

### Setting client and server address depending on IP version
When OS supports IPv6 and the server address is not set using the 'FixServer.SetNic(string nic)' method, 
a socket will be created in the "Dual" mode, allowing both IPv4 and IPv6 connection.

Otherwise, the type of allowed connection depends on the type of address, provided by the 'nic' parameter.
For example, if 'nic' is '::1', then IPv6 will be used to create a network socket. And IPv4 - if 'nic' 
like '127.0.0.1'.

### Environment variables for configuration
FIX Anenna configuration properties can be set in environment variables
To be applied, an environment variable must comply with the following rules:
- Variable name must start with the prefix FANET_
- For hierarchical properties, double underscore '__' should be used instead of the '.' symbol as a delimiter

*Example:*

Property set in **fixengine.properties** file:
```INI
sessions.session.SenderCompID=senderComp
```
Property set in environment variable:
```INI
FANET_sessions__session__SenderCompID=senderComp
```
Priority of property sources:
1. Environment variables
2. **fixengine.properties** file
3. Default value

NOTE:
The environment variable name is case insensitive.
The environment variable value is case sensitive.

### QuickFIX dictionaries support
FIX Antenna .NET Core provides compatibility with QuickFIX dictionaries.
QuickFIX dictionaries shoud be used the same way as FIX Antenna dictionaries.
To assign a QuickFIX dictionary to all sessions of a specific FIX version, replace the standard FIX Antenna dictionary file with the QuickFIX dictionary file of the same FIX version in the dictionary folder. The name of the file must be the same.

### Assign an individual dictionary to a specific session 
To assign a dictionary to a specific session:

```INI
# comma separated list of custom FIX dictionary aliases
customFixVersions=FIX44Custom,FIX50Custom

# pair of 'fixVersion' and 'fileName' for each FIX dictionary alias with pattern:
# customFixVersion.<custom FIX version alias>.fixVersion=<base standard FIX version>
# customFixVersion.<custom FIX version alias>.fileName=<custom FIX dictionary file name>

# example of custom FIX dictionary based on FIX.4.4
customFixVersion.FIX44Custom.fixVersion=FIX.4.4
customFixVersion.FIX44Custom.fileName=fixdic44-custom.xml

# example of FIX dictionary based on custom application FIX.4.4
customFixVersion.FIX44Custom.fixVersion=FIX.4.4
customFixVersion.FIX44Custom.fileName=fixdic44-custom.xml

# examples of custom FIX dictionary based on FIX.5.0
customFixVersion.FIX50Custom.fixVersion=FIX.5.0
customFixVersion.FIX50Custom.fileName=fixdic50-custom.xml

# assigning FIX dictionaries to sessions
sessions.testSession1.fixVersion=FIX44Custom

sessions.testSession2.fixVersion=FIXT11
sessions.testSession2.appVersion=FIX50Custom
```
