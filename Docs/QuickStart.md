# Quick Start
This chapter explains how to create a simple application step-by step with samples. The
usual scenario to get it to work:
- Create a session (initiator or acceptor)
- Send a message, process incoming messages
- Close the session

## Session acceptor creation
Session-acceptor is created in the following way:
- The `FixServer` object is created (server)
- Port and other server properties are set
- `IFixServerListener` is created (observer entity for new sessions) and set
- As soon as someone tries to establish a connection to your FIX server by sending a Logon message, you will receive a callback to your `IFixServerListener` interface with a new `IFixSession` object. Most of the `SessionParameters` will be prepopulated based on the incoming Logon.
- You may either reject this particular incoming session by calling `Dispose()` or accept the incoming session by calling `Connect()`. Prior to the `Connect()` call set your `IFixSessionListener` (observer entity for session to receive session specific events),

```csharp
// implements IFixServerListener to get notifications about new connections
public class SimpleServer : IFixServerListener
{
    public static void Main(string[] args)
    {
        FixServer server = new FixServer(); // create server that will listen for TCP/IP connections
        server.SetPort(777); // setting port it will listen to
        server.SetListener(new SimpleServer()); // setting listener for new connections
        server.Start(); // this will start new thread and listen for incoming connections
        
        Console.WriteLine("Press enter to exit");
        Console.Read(); // preventing application from exiting
        
        server.Stop(); // this will stop the thread that listens for new connections
    }

    // this method is called for every new connection
    public void NewFixSession(IFixSession session) 
    {
        try 
        {
            session.SetFixSessionListener(new MyFixSessionListener(session)); // setting listener for incoming messages
            session.Connect(); // accepting connection
        } 
        catch (IOException e) 
        {
        }
    }

    // listener for incoming messages and session state changes
    private class MyFixSessionListener : IFixSessionListener 
    {
        private IFixSession session;

        public MyFixSessionListener(IFixSession session) 
        {
            this.session = session;
        }

        // this method will be called every time session state is changed
        public void OnSessionStateChange(SessionState sessionState) 
        {
            if (sessionState == SessionState.Disconnected) 
            {
                session.Dispose();
            }
        }

        // here you can process incoming messages
        public void OnNewMessage(FixMessage message) 
        {
        }
    }
}
```

## Session initiator creation
The session-initiator is created in three steps:
- Application object is created (observer entity for session to receive events)
- The session object is created passing the initiator's specific parameters
- The connect method is called

```csharp
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.Message;

using System.IO;
using System;

// implements IFixServerListener to get notifications about new connections
public class SimpleServer : IFixServerListener
{
	public static void Main(string[] args)
	{
		FixServer server = new FixServer(); // create server that will listen for TCP/IP connections
		server.SetPort(777); // setting port it will listen to
		server.SetListener(new SimpleServer()); // setting listener for new connections
		server.Start(); // this will start new thread and listen for incoming connections

		Console.WriteLine("Press enter to exit");
		Console.Read(); // preventing application from exiting

		server.Stop(); // this will stop the thread that listens for new connections
	}

	// this method is called for every new connection
	public void NewFixSession(IFixSession session)
	{
		try
		{
			session.SetFixSessionListener(new MyFixSessionListener(session)); // setting listener for incoming messages
			session.Connect(); // accepting connection
		}
		catch (IOException e)
		{
		}
	}

	// listener for incoming messages and session state changes
	private class MyFixSessionListener : IFixSessionListener
	{
		private IFixSession session;

		public MyFixSessionListener(IFixSession session)
		{
			this.session = session;
		}

		// this method will be called every time session state is changed
		public void OnSessionStateChange(SessionState sessionState)
		{
			if (sessionState == SessionState.Disconnected)
			{
				session.Dispose();
			}
		}

		// here you can process incoming messages
		public void OnNewMessage(FixMessage message)
		{
		}
	}
}
```

## Creating new order

To create 

FIX Antenna provides an interface for manipulations with FIX messages.
It is based on a single `FixMessage` class and exports high performance.

```csharp
// create FIX 4.4 New Order Single
FixMessage messageContent = new FixMessage();
messageContent.AddTag(11, "USR20000101");
messageContent.AddTag(54, 1);
messageContent.AddTag(60, "20000101-01:12:55");
```

## Sending order
A message is sent asynchronously by calling the `session.SendMessage` method. Consequently, the message is
scheduled for sending in the internal queue and sent in a separate thread. This means that
if the method returns control, the message is not necessarily already sent.
The fastest standard way of sending a message out is to use the `SendMessage(string messageType, FixMessage content)` method.
This message adds a proper footer and header.

```csharp
// Send news to session initiator or acceptor
session.SendMessage("B", messageContent); // news
```

Alternatively, if a user has an entire FIX Message in `FixMessage`, including a header and
footer, it is possible to use the `SendMessage(FixMessage message)`
method. The header and footer fields will be updated but it will take more
time then just wrapping message content, so the first option is preferred.

```csharp
// Send news to session initiator or acceptor
session.SendMessage(message); // news
```

## Processing incoming message
Implement the `IFixSessionListener` interface to process incoming
messages and session changes:
```csharp
private class MyFixSessionListener : IFixSessionListener 
{
    private IFixSession session;

    public MyFixSessionListener(IFixSession session) 
    {
        this.session = session;
    }

    public void OnSessionStateChange(SessionState sessionState) 
    {
        if (sessionState == SessionState.Disconnected) 
        {
            session.Dispose();
        }
    }

    public void OnNewMessage(FixMessage message) 
    {
        Console.WriteLine("New application level message type: " + message.GetTagValueAsString(Fixt11.Header.MsgType) + "received");
    }
}
```


The `IFixSessionListener.OnNewMessage()` method is called only in the case of
the delivery of an incoming application-level message, if all checks are
passed. Possible errors and all session-level messages are handled inside
the FixEngine. The FIX Engine guarantees sequential message delivery in the order
of receiving.

Note: It is possible to provide custom session level handlers if desired,
but it may affect FIX Engine workflow and FIX protocol support so it is not
recommended unless absolutely necessary.

Note: The `IFixSessionListener.OnNewMessage()` method must not be dead-locked or
perform time-demanding operations because it will lock all message processing
for this session. A user should execute time consuming handling operations in
separate threads and return control as soon as possible..

## Closing session
Use the following methods to close the session:
- `session.Disconnect("User request")`
   A user should dispose session if he don't plan to use this instance again.
- `session.Dispose()`

## Sample application
Combining all above:

```csharp
using System;
using System.IO;

using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.Message;

namespace Server
{
  // implements IFixServerListener to get notifications about new connections
  public class SimpleServer : IFixServerListener
  {
    public static void Main(string[] args)
    {
      FixServer server = new FixServer(); // create server that will listen for TCP/IP connections
      server.SetPort(777); // setting port it will listen to
      server.SetListener(new SimpleServer()); // setting listener for new connections
      server.Start(); // this will start new thread and listen for incoming connections

      Console.WriteLine("Press enter to exit");
      Console.Read(); // preventing application from exiting

      server.Stop(); // this will stop the thread that listens for new connections
    }

    // this method is called for every new connection
    public void NewFixSession(IFixSession session)
    {
      try
      {
        session.SetFixSessionListener(new MyFixSessionListener(session)); // setting listener for incoming messages
        session.Connect(); // accepting connection
        Console.WriteLine("New connection accepted");
      }
      catch (IOException e)
      {
      }
    }

    // listener for incoming messages and session state changes
    private class MyFixSessionListener : IFixSessionListener
    {
      private IFixSession session;

      public MyFixSessionListener(IFixSession session)
      {
        this.session = session;
      }

      // this method will be called every time session state is changed
      public void OnSessionStateChange(SessionState sessionState)
      {
        Console.WriteLine("Session state: " + sessionState);
        // if disconnected, dispose it to let GC collect it
        if (sessionState == SessionState.Disconnected)
        {
          session.Dispose();
          Console.WriteLine("Your session has been disconnected. Press ENTER to exit the programm.");
        }
      }

      // here you can process incoming messages
      public void OnNewMessage(FixMessage message)
      {
        Console.WriteLine("New message is accepted: " + message.ToString());
      }
    }
  }
}
```

```csharp
using System;
using System.Threading;

using Epam.FixAntenna.Constants.Fixt11;
using Epam.FixAntenna.NetCore.Common;
using Epam.FixAntenna.NetCore.FixEngine;
using Epam.FixAntenna.NetCore.Message;

namespace Client
{
  public class SimpleNewsBroadcaster
  {
    public static void Main(string[] args)
    {
      // creating connection parameters
      var details = new SessionParameters
      {
	      FixVersion = FixVersion.Fix42,
	      Host = "localhost",
	      HeartbeatInterval = 30,
	      Port = 777,
	      SenderCompId = "senderId",
	      TargetCompId = "targetId"
      };


      // create a session we intend to work with
      IFixSession session = details.CreateNewFixSession();

      // listener for incoming messages and session state changes
      IFixSessionListener application = new MyFixSessionListener(session);

      // setting listener for incoming messages
      session.SetFixSessionListener(application);
      // initiate a connection
      session.Connect();


      // create FIX 4.2 News
      FixMessage messageContent = new FixMessage();
      messageContent.AddTag(148, "Hello there"); // Add Subject
      messageContent.AddTag(33, 3); // Add Repeating group
      messageContent.AddTag(58, "line1");
      messageContent.AddTag(58, "line2");
      messageContent.AddTag(58, "line3");

      // sending a message
      session.SendMessage("B", messageContent);
      try
      {
        // sleep for some time to ensure message delivery. Other flow control procedures should
        // be used in real applications.
        Thread.Sleep(100);
      }
      catch (Exception e)
      {
        // ignored
      }

      // disconnecting
      session.Disconnect("User request");
    }

    private class MyFixSessionListener : IFixSessionListener
    {
      private IFixSession session;

      public MyFixSessionListener(IFixSession session)
      {
        this.session = session;
      }

      public void OnNewMessage(FixMessage message)
      {
        Console.WriteLine("New application level message type: " + message.GetTagValueAsString(Tags.MsgType) + "received");
        // this callback is called upon new message arrival
      }

      public void OnSessionStateChange(SessionState sessionState)
      {
        Console.WriteLine("Session state changed:" + sessionState);
        // this callback is called upon session state change
        if (sessionState == SessionState.Disconnected)
        {
          // end this session
          session.Dispose();
        }
      }
    }
  }
}
```
