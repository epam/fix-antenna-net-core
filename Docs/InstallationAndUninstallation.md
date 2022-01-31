# Installation & Uninstallation
## Requirements & Compatibility

Supported operating systems:

1. Libraries: any OS with .NET platform installed that supports .NET Standard 2.0. </br>
   A compatibility table of .NET Standard 2.0 can be found [here](https://docs.microsoft.com/en-us/dotnet/standard/net-standard#net-implementation-support).</br>
   NOTE: Fix Antenna was tested under .NET 6 and under .NET Framework 4.8.
2. Samples: any OS with .NET 6 or .NET Framework 4.8 installed. </br>
   .NET Core and .NET Framework can be downloaded from [here](https://dotnet.microsoft.com/download).

Note:
It is recommended to target x64 architecture when using FIX Antenna with storages based on memory-mapped files.

## Supported Features

### FIX Engine
* FIX 4.0 - FIX 4.4
* FIX 5.0
* FIX 5.0 SP1
* FIX 5.0 SP2
* Custom messages and fields support
* Multiple FIX sessions
* Initiator/Acceptor Sessions
* Autoreconnect Session
* Transient Sessions
* Restore state after failure (Persistent Session)
* Switch to backup connection
* Pluggable session level
* Administrative Plugin for Fix Antenna

### Message Vallidation
* Wellformeness Validation
* Allowed Fields Validation
* Required Fields Validation
* Fields Order Validation
* Duplicate Fields Detection	
* Field Type Validation	
* Groups Validation	
* Conditional Fields Validation

## Samples descriptions

Samples is a set of small applications accompanied with sources that
demonstrate the usage of the core FIX Antenna functionality.

### SimpleAdminClient
This sample demonstrates how to connect to an administrative session and communicate with it using the implementation of administrative messages.

How to run:
- make sure that your environment is properly configured
- run a FIX server application with an enabled Administrative plugin
- run the script runSimpleAdminClient.bat
- the application will connect to the server, subscribe for the session list, receive an answer with the session list on the server and close the current session

### SimpleServer
This sample demonstrates how to implement a FIX server with simple IP filtering of incoming sessions.

How to run:
- run the script runSimpleServer.bat
- the FIX server will start at 3000 port. Press the **Enter** key to finish the sample.

### EchoServer
This sample demonstrates a very simple server, which sends all the received messages back to a client.

Note: the sending messages back mode can be disabled, set echoMode=false for that.

How to run:
- run the script runEchoServer.bat
- once it starts, clients are able to connect and send messages

### ConnectToGateway
This sample demonstrates a very simple client, which connects to the server and accepts all received messages.

How to run:
- run a FIX server application
- run the script runConnectToGateway.bat
- once it starts, the sample will connect to the server and keep the fix session

## Uninstallation instructions
To uninstall FIX Antenna, simply remove the FIX Antenna directory from the disk. 
