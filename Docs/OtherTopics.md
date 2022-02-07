# Other Topics
## Log files

A file system is used to log messages for FIX Antenna recovery.

Log files are created in the directory specified in the configuration
parameters (the `storageDirectory` property).

Files with the *.properties extension (properties files) contain basic session information.
- Port
- InitialIncomingSeqNumber
- Host
- InitialOugoingSeqNumber
- Sender SenderCompID
- Target TargetCompID
- FIX Protocol version

Incoming messages are stored to files with the .in extension.

Outgoing messages are stored to files with the .out extension.

Some tag values in both the .in and .out log files are masked with asterisks. This is done to obfuscate sensitive information (such as logins and passwords) by applying masks. The tag values are obfuscated with asterisks. The number of asterisks depends on the field length.

**NOTE:** Session logins are not obfuscated by default, but this option can be configured.

When a row with the text "Password=<span><<span>quoted value<span>><span>" is added to the application log file, the "quoted value" is obfuscated with three asterisks.     

When a message in the application log includes a FIX message, the Engine obfuscates sensitive field values in the FIX message with the corresponding number of asterisks.

Files related to the same session have the same basic part of the name (the part before a period).

Writing information about FA operations into log files could be configured via [file targets](https://github.com/nlog/NLog/wiki/File-target) of `NLog`.

Log files are used during automatic session reestablishment. Thus, to
ensure a "clean" run of FIX Antenna, it is necessary to remove all the files from
the log directory (the "storageDirectory" property).

## Compatibility with Log Replicator

Starting from version 0.9.3, FIX Antenna .NET Core is compatible with the Log Replicator.

The Replication tool determines the type of session logs automatically and does not require any additional configuration. However, FIX Antenna .NET Core must be configured in a particular way, having the following properties with certain values for the Replication tool to operate correctly:

|    **Property Name**     | **Prohibited Value(s)** | **Allowed Value(s)** |
---------------------------|-------------------------|----------------------|
| storageFactory           | SlicedFileStorageFactory<br> InMemoryStorageFactory | FilesystemStorageFactory<br>  MMFStorageFactory |
| incomingStorageIndexed   | false                   | true                 |
| outgoingStorageIndexed   | false                   | true                 |
| timestampsInLogs         | false                   | true                 |

Resetting sequence numbers in a session isn't supported. You must create a new session instead.

## FAQ

#### Is it possible to create more than one instance of FIX Antenna?
Yes, it is possible to create many instances of FIX Antenna as long as they use different listening ports. 
FIX Server instances are pretty lightweight so there is no limit on the number of servers, rather on the number 
of simultaneous sessions.

#### Is it possible to create more than one instance of FIX Antenna using the same logging directory?
Yes. However, it is recommended to set different logging files. Otherwise, different instances will put logging data into the 
same file and it will be difficult to distinguish records made by different instances.

#### May I put session log files to the log directory and expect that FIX Antenna will analyse them when a new session with the same parameters is created?
Yes. FIX Antenna loads all session information at session creation time.

#### FIX Antenna was incorrectly terminated. I don't want the session to be restored on the next start. What should I do for a clean start?
It is enough to just clean the logging directory before creating a new instance of FIX Antenna.

#### How can I do the customization (add user-defined fields, messages, or change fields and messages definition)?
User Defined Fields (the tag numbers 5000 to 9999) are handled like ordinary fields.

#### How can I fix the "Repeating group fields out of order" error?
Example:
```LOG
8=FIX.4.3 | 9=285 | 35=X | 49=FIXERBRIDGETRD1 | 34=1 | 57=B2BTEST1 | 52=99990909-17:17:17 | 268=4 | 269=2 | 270=9539.500000 | 271=1 | 272=20041208 | 273=13:34:10.000 | 290=1 | 269=4 | 27 0 =9539.500000 | 271=4294967295 | 272=20041208 | 273=13:34:10.000 | 290=1 | 269=Z | 270=9528.000000 | 271=401 | 290=1 | 269=Y | 270=0.000000 | 271=0 | 290=1 | 10=019 |
[ERROR] Repeating group fields out of order [RefSeqNum: 1, RefTagID: 269, RefMsgType: X]
```

Solution:
The source of this problem cannot be detected easily. The required fields of both the FIX message itself and 
the group (`AdditionalFields`) must be filled out with corresponding values, and any gap or change in the tag order 
leads to the error (this is true for both standard and custom messages). Therefore, both standard and additional group fields 
must be checked for validity and then processed again. Extra attention must be paid to the order of fields in the repeating 
FIX group.
For this particular example: The tag 290 is placed before the tag 269 (wrong tag order). To solve the problem: place the tag 290 and its value after tag 269 and process the message again.

#### How to create a FIX 5.0 session?
The FIX session has two parameters for FIX version control: `FixVersion` and `AppVersion`.
`FixVersion` resends data that will be put to `BeginString(8)` tag of each message. 
`AppVersion` is used only if `FixVersion` contains a marker for FIX Session Protocol FIXT11).
In this case, the `AppVersion` parameter will contain the `ApplVerID(1128)` tag value.
If you want to create a FIX 5.0 session, add the following strings to your code:

```csharp
SessionParameters details = new SessionParameters();
details.FixVersion = FixVersion.Fixt11;
details.AppVersion = FixVersion.Fix50;
```

#### How to reset seqeuence numbers on logon?
Unfortunately, FIX Antenna does not have this function so far. We plan to include it in future releases. If you'd like, you can implement it in your code:
```csharp
class MyServerListener : IFixServerListener {
    
    //...
    SessionParameters sessionParameters = session.Parameters;
    sessionParameters.OutgoingSequenceNumber = 1; //reset outgoint sequnce number to 1
    sessionParameters.IncomingSequenceNumber = 1; //reset outgoint sequnce number to 1
    //send this flag to other side to notificate tat thay should reset sequence too.
    sessionParameters.AddOutgoingLoginField(Tags.ResetSeqNumFlag, "Y");
    //...

    session.Connect();
}
```

#### How do I implement a basic authorization process?
To send a login/password via a Logon message, use `SessionParameters.AddOutgoingLoginField` to set additional tags before session connect:
```csharp
details.AddOutgoingLoginField(Tags.Username, "user");
details.AddOutgoingLoginField(Tags.Password, "pass");
```

To verify a login/password on server side:
```csharp
public void NewFixSession(IFixSession session) 
{
    SessionParameters parameters = session.Parameters;
    //get username(553) and password(554) tags from incomming login
    string username = parameters.IncomingLoginMessage.GetTagValueAsString(Tags.Username);
    string password = parameters.IncomingLoginMessage.GetTagValueAsString(Tags.Password);
    //check that this user has access to the system
    if (!IsAuthorized(username, password))
    {
        // terminate session
        session.Dispose();
    }
    else
    {
        //accept this session
        session.SetFixSessionListener(new MyFixSessionListener(session));
        session.Connect();
    }
}
```

FIX specification allows shutting down the connection without sending Logout here:
"If authentication fails, the session acceptor should shut down the connection after optionally sending a Logout message to indicate the reason of failure. Sending Logout in this case is not required because doing so would consume a sequence number for that session, which in some cases may be problematic."<sup>[[1]](#source1)</sup>

But if a Logout message is required in your environment, you need to write a custom handler with authorization for Logon messages. It is possible to send a Logout message without Logon from the handler.


#### How do I use a customized FIX version with Antenna?
Antenna receives all required information about a FIX version from FIX dictionaries. They contain meta-data in the XML format, which helps FIX Antenna validate messages.

There are several ways to customize FIX Antenna behaviour:
- Set a custom dictionary for a certain session:
   ```csharp
    SessionParameters details = new SessionParameters();
    //replace standard FIX 4.4 dictionary with custom for concrete session
    FixVersionContainer fixVersion = new FixVersionContainer("CustomFIX44", FixVersion.Fix44, "custom/fixdic44.xml");
    details.FixVersionContainer = fixVersion;
   ...
   IFixSession session = details.CreateNewFixSession();
   ```
- Load customized dictionaries in runtime:
   ```csharp
   //replace standard FIX 4.4 dictionary with custom
   ValidationEngine.PreloadDictionary(FixVersion.Fix44, "custom/fixdic44.xml", true);
   ```
- Customize the existing version with this extension:
   ```csharp
    FixVersionContainer fix44Version = FixVersionContainer.GetFixVersionContainer(FixVersion.Fix44);
    FixVersionContainer customFix44Version = new FixVersionContainer("CustomFIX44", FixVersion.Fix44, "custom/additional44.xml");
    details.FixVersionContainer = customFix44Version;
   ```

## Troubleshooting

#### I get a "System.IO.IOException : Not enough memory resources are available to process this command." exception when using storages based on memory-mapped files)
This error means that the application memory could not hold a memory-mapped file of such a size.
Make sure your application targets x64 architecture.

#### FIX Antenna cannot create a listen port on start
Make sure that this port is not in use by some other application. Try `netstat -a`
to get the list of used ports.

## References
<a name="source1">1. FIX Trading Community. "FIX connection termination" fixtrading.org, Accessed on February 7, 2022, https://www.fixtrading.org/standards/fix-session-layer-online/#fix-connection-termination</a>
