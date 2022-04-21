# How to test FA .NET Core performance by simple Client-Server scenario

For best results, two machines should be used for the test, one as the Sender Host (Client), the other as the Receiver Host (Server).

FIX Antenna .NET Core performance can be measured by running the following samples: `\Samples\Latency\Sender` and `\Samples\Latency\Server`.

Configuration file `fixengine.properties` can be found in the application folder (Sender or Server).

## Description
`Server` starts as acceptors, and waiting for incoming connection. `Sender` connects as initiator to the `Server` and sends `WarmUpCycleCount` messages as warm-up. After that, `Sender` sends `NoOfMessages` (100 000 by default) with `MsgType=D`. For each `D` message `Server` responds with `8` message.

`Sender` measures time between outgoing `D` and received `8` messages and stores measured value to `HdrHistogram`.

## Configuration and running
Command line arguments for Server:

`Server host port` where `host` is an IP address of server part and `port` is server's port.

Command line arguments for Sender:

`Sender host port` where `host` is an IP address of server part and `port` is server's port. 

User can configure CPU affinity in the `fixengine.properties` file. By default affinity is set to CPU 0 for both sending and receiving threads:

```INI
# This parameter specifies CPU id for the threads of session that send and receive the data from/in socket.
# Please set correct CPU id for better performance
# By default used first CPU (id=0)
cpuAffinity=0
```

Instead of single option `cpuAffinity` user can use `recvCpuAffinity` and `sendCpuAffinity` configuration options to pin sending and receiving threads to different CPU cores.

## Results
After the `NoOfMessages` sent, the collected data exported as `.hgrm` file to the `Result` folder of the `Sender`.

#### Example
The results were collected on laptop running Win10 on i5-10310U @1.7Ghz

Parameters:
`Server 127.0.0.1 3010`
`Sender 127.0.0.1 3010`

Both parts of the test are running on the same laptop and no any optimizations applied. `Value` column is the time in µs.

```
       Value     Percentile TotalCount 1/(1-Percentile)

      82,700 0,000000000000          1           1,00
      89,100 0,100000000000      10278           1,11
      89,800 0,200000000000      20958           1,25
      91,400 0,300000000000      30262           1,43
      96,000 0,400000000000      40066           1,67
      99,300 0,500000000000      50545           2,00
      99,600 0,550000000000      56377           2,22
      99,800 0,600000000000      60017           2,50
     100,200 0,650000000000      65145           2,86
     100,900 0,700000000000      70030           3,33
     102,300 0,750000000000      75344           4,00
     102,900 0,775000000000      77853           4,44
     103,400 0,800000000000      80025           5,00
     104,500 0,825000000000      82587           5,71
     107,300 0,850000000000      85026           6,67
     109,800 0,875000000000      87584           8,00
     111,600 0,887500000000      88777           8,89
     113,600 0,900000000000      90046          10,00
     116,000 0,912500000000      91267          11,43
     120,100 0,925000000000      92500          13,33
     124,800 0,937500000000      93769          16,00
     126,700 0,943750000000      94404          17,78
     129,100 0,950000000000      95004          20,00
     132,600 0,956250000000      95626          22,86
     136,000 0,962500000000      96269          26,67
     138,100 0,968750000000      96890          32,00
     139,700 0,971875000000      97199          35,56
     141,500 0,975000000000      97503          40,00
     144,100 0,978125000000      97816          45,71
     147,700 0,981250000000      98131          53,33
     151,100 0,984375000000      98445          64,00
     152,600 0,985937500000      98595          71,11
     154,500 0,987500000000      98756          80,00
     156,800 0,989062500000      98908          91,43
     159,500 0,990625000000      99063         106,67
     163,000 0,992187500000      99222         128,00
     164,700 0,992968750000      99297         142,22
     167,700 0,993750000000      99375         160,00
     170,300 0,994531250000      99455         182,86
     173,500 0,995312500000      99535         213,33
     177,800 0,996093750000      99610         256,00
     180,500 0,996484375000      99649         284,44
     183,800 0,996875000000      99689         320,00
     187,900 0,997265625000      99728         365,71
     192,200 0,997656250000      99766         426,67
     200,300 0,998046875000      99805         512,00
     206,100 0,998242187500      99825         568,89
     213,900 0,998437500000      99844         640,00
     221,500 0,998632812500      99864         731,43
     233,700 0,998828125000      99883         853,33
     250,900 0,999023437500      99903        1024,00
     275,700 0,999121093750      99913        1137,78
     290,500 0,999218750000      99922        1280,00
     315,100 0,999316406250      99932        1462,86
     335,700 0,999414062500      99942        1706,67
     352,500 0,999511718750      99952        2048,00
     362,700 0,999560546875      99957        2275,56
     367,700 0,999609375000      99961        2560,00
     387,100 0,999658203125      99966        2925,71
     409,300 0,999707031250      99971        3413,33
     421,500 0,999755859375      99976        4096,00
     435,500 0,999780273437      99979        4551,11
     449,100 0,999804687500      99981        5120,00
     479,900 0,999829101563      99983        5851,43
     486,700 0,999853515625      99986        6826,67
     529,500 0,999877929688      99988        8192,00
     549,500 0,999890136719      99990        9102,22
     618,700 0,999902343750      99991       10240,00
     743,500 0,999914550781      99992       11702,86
     750,700 0,999926757812      99993       13653,33
     817,500 0,999938964844      99994       16384,00
    1299,100 0,999945068359      99995       18204,44
    1311,900 0,999951171875      99996       20480,00
    1311,900 0,999957275391      99996       23405,71
    1319,100 0,999963378906      99997       27306,67
    1319,100 0,999969482422      99997       32768,00
    1445,500 0,999972534180      99998       36408,89
    1445,500 0,999975585938      99998       40960,00
    1445,500 0,999978637695      99998       46811,43
    1447,100 0,999981689453      99999       54613,33
    1447,100 0,999984741211      99999       65536,00
    1447,100 0,999986267090      99999       72817,78
    1447,100 0,999987792969      99999       81920,00
    1447,100 0,999989318848      99999       93622,86
    1851,100 0,999990844727     100000      109226,67
    1851,100 1,000000000000     100000
#[Mean    =      100,632, StdDeviation   =       19,468]
#[Max     =     1851,100, Total count    =       100000]
#[Buckets =           17, SubBuckets     =         2048]
```