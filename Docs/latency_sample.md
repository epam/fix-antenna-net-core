# How to measure FA .NET Core latency by simple Client-Server scenario

For best results, two machines should be used for the test, one as the Sender Host (Client), the other as the Receiver Host (Server).

FIX Antenna .NET Core performance can be measured by running the following samples: `\Samples\Latency\Sender` and `\Samples\Latency\Server`.

Configuration file `fixengine.properties` can be found in the application folder (Sender or Server).

##Configuration and running
Command line arguments for Server:

`Server host port` where `host` is an IP address of server part and `port` is server's port.

Command line arguments for Sender:

`Sender host port` where `host` is an IP address of server part and `port` is server's port. 

User can configure CPU affinity in the engine.properties file. By default affinity is set to CPU 0 for both sending and receiving threads:

```INI
# This parameter specifies CPU id for the threads of session that send and receive the data from/in socket.
# Please set correct CPU id for better performance
# By default used first CPU (id=0)
cpuAffinity=0
```

Instead of single option `cpuAffinity` user can use `recvCpuAffinity` and `sendCpuAffinity` configuration options to pin sending and receiving threads to different CPU cores.