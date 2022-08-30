# Scheduler
## Initiator
FIX Antenna™ .NET Core allows sсheduling a session start/stop action using syntax similar to the UNIX cron daemon. We use Quartz.NET and their implementation of cron expressions for scheduling. See <a href="https://www.quartz-scheduler.net/documentation/quartz-3.x/tutorial/crontrigger.html#introduction">cron expression</a>  for more information about the allowed expression format.

The scheduler parameters are set via the config file the following way:
```ini
sessions.testSession.tradePeriodBegin=0 0 8 * * ?
sessions.testSession.tradePeriodEnd=0 0 17 * * ?
sessions.testSession.tradePeriodTimeZone=UTC
```
The `tradePeriodTimeZone` is optional and have "UTC" as a default value. `tradePeriodBegin` and `tradePeriodEnd` can contain several cron expressions divided by "|" ("0 0 8 * * ?|0 0 9 * * ?").

More information about time zone format: <a href="https://docs.microsoft.com/en-us/dotnet/api/system.timezoneinfo.id?view=netstandard-2.0#System_TimeZoneInfo_Id">System.TimeZoneInfo.Id</a>

```csharp
// loading pre-configured session parameters from the fixengine.properties file
var sessionParams = SessionParametersBuilder.BuildSessionParameters("testSession");

// create the pre-configured session
var session = sessionParams.CreateScheduledInitiatorSession();

// create and attach a listener 
session.SetFixSessionListener(new FixSessionListener());

// schedule the session. It will connect/disconnect according to the configured parameters 
session.Schedule();



// stop auto connecting/disconnecting
session.Deschedule();

// it is still possible to connect session manually
session.Connect();
```

If the moment of calling `session.Schedule()` is inside the time interval defined by `tradePeriodBegin` and `tradePeriodEnd`, the session will start connecting immediately.

## Acceptor
It is possible to allow incoming connections only during specified period of time.
To use this feature, you need to add configuration and create an instance of `ScheduledFixServer` instead of `FixServer`:

```ini
tradePeriodBegin=0 0 8 * * ?
tradePeriodEnd=0 0 17 * * ?
tradePeriodTimeZone=UTC
```
The `tradePeriodTimeZone` is optional and have "UTC" as a default value. `tradePeriodBegin` and `tradePeriodEnd` can contain several cron expressions divided by "|".

The accepted session will be automatically disconnected at `tradePeriodEnd`.

```csharp
var configuration = new Config(Config.DefaultEngineProperties);

var server = new ScheduledFixServer(configuration);

server.Start();
```

You can also override these parameters for a particular session:
```ini
sessions.testSession.tradePeriodBegin=0 0 8 * * ?
sessions.testSession.tradePeriodEnd=0 0 17 * * ?
sessions.testSession.tradePeriodTimeZone=UTC
```

If `tradePeriodBegin` is not set, the session will be allowed to connect and will be disconnected at `tradePeriodEnd`.

## Samples
[Samples/SimpleScheduledServer](/Samples/SimpleScheduledServer)

[Samples/SimpleScheduledClient](/Samples/SimpleScheduledClient)