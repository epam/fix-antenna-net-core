# Scheduler
## Initiator
FIX Antenna .NET Core allows sсheduling a session start/stop action using syntax similar to a UNIX cron daemon. We use Quartz.NET and their implementation of cron expressions for scheduling. See <a href="https://www.quartz-scheduler.net/documentation/quartz-3.x/tutorial/crontrigger.html#introduction">Cron expression</a>.

The scheduler parameters are set via the config file the following way:
```ini
sessions.testSession.tradePeriodBegin=0 0 8 * * ?
sessions.testSession.tradePeriodEnd=0 0 17 * * ?
sessions.testSession.tradePeriodTimeZone=UTC
```
The `tradePeriodTimeZone` is optional and have "UTC" as a default value. `tradePeriodBegin` and `tradePeriodEnd` can contain several cron expression divided by "|" ("0 0 8 * * ?|0 0 9 * * ?").  
More information about time zone format:  
<a href="https://docs.microsoft.com/en-us/dotnet/api/system.timezoneinfo.id?view=netstandard-2.0#System_TimeZoneInfo_Id">System.TimeZoneInfo.Id</a>

```csharp
// loading a pre-configured session parameters from the fixengine.properties file
var sessionParams = SessionParametersBuilder.BuildSessionParameters("testSession");

// create pre-configured session
var session = sessionParams.CreateScheduledInitiatorSession();

// create and attach listener 
session.SetFixSessionListener(new FixSessionListener());

// schedule the session. It will connect/disconnect according to the configured parameters 
session.Schedule();



// stop auto connecting/disconnecting
session.Deschedule();

// it is still possible to connect session manually
session.Connect();
```

## Acceptor
It is possible to allow incomming connections only during specified period of time.
To use this feature you need to add configuration and create an instance of `ScheduledFixServer` instead of `FixServer`:

```ini
tradePeriodBegin=0 0 8 * * ?
tradePeriodEnd=0 0 17 * * ?
tradePeriodTimeZone=UTC
```
The `tradePeriodTimeZone` is optional and have "UTC" as a default value. `tradePeriodBegin` and `tradePeriodEnd` can contain several cron expression divided by "|".

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