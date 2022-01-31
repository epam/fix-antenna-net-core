# TLS Support
## Introduction

In order to support secure connection, FA .NET Core provides the respective API and configuration options.

The SSL/TLS connection propeties are set via API or by using the FIX Antenna configuration file (`fixengine.properties` file).

Implementation uses the [System.Net.Security.SslStream](https://docs.microsoft.com/en-us/dotnet/api/system.net.security.sslstream?view=netstandard-2.0) class and other types from [System.Net.Security](https://docs.microsoft.com/en-us/dotnet/api/system.net.security?view=netstandard-2.0), [System.Security.Authentication](https://docs.microsoft.com/en-us/dotnet/api/system.security.authentication?view=netstandard-2.0), [System.Security.Cryptography.X509Certificates](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates?view=netstandard-2.0) namespaces.

**NOTE:** Support of different certificate types depends only on the [X509Certificate2](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2?view=netstandard-2.0) class.

The acceptor can be configured to listen to both regular (insecure) and secure connections.

### Example - fixengine.properties
```INI
# listening port(s) for regular connections
port=30000
# listening port(s) for secure connections
sslPort=30001,30002
```

### Example - API
```csharp
fixServer.Ports = new[]{30000};
fixServer.SslPorts = new[]{30001, 30002};
```

The case when the same port is defined for both secure and insecure connections is specified in the [Diagnostics and troubleshooting](#diagnostics-and-troubleshooting) section. 

The certificate used for connections can be set up with the following options:

```INI
sslCertificate=server.pfx
sslCertificatePassword=password
sslValidatePeerCertificate=true
sslCaCertificate=TestCA.crt
```

All the options above can be configured using API, for example:
```csharp
Config configuration = ...;
configuration.SetProperty(Config.SslCertificate, "server.pfx");
```

The options can also be configured via session parameters:
```csharp
SessionParameters parameters = ...;
parameters.Configuration.SetProperty(Config.SslValidatePeerCertificate, "false");
```

## SSL/TLS configuration

The table below contains all SSL/TLS related configuration options.

|    **Option**          |           **Description**            |     **Level**     |
|------------------------|--------------------------------------|-------------------|
| sslPort                | Acceptor: listening port(s) for SSL/TLS connections. Initiator: ignored.  | Global |
| requireSSL             | Requires establishing secured transport for an individual session, or for all sessions, when used on top-level configuration.   
| Both global and per session |
| sslProtocol            | Selected SSL protocol, the default value is "None" [as recommended by Microsoft](https://docs.microsoft.com/en-us/dotnet/api/system.security.authentication.sslprotocols?view=netstandard-2.0) - in this case the best suitable protocol will be used. | Both global and per session |
| sslCertificate         | Name of the certificate. Could be a file name, or a distinguished name (CN=...) of a certificate when the certificate store is used. | Both global and per session |
| sslCertificatePassword | Password for the SSL certificate. | Both global and per session |
| sslValidatePeerCertificate | If true, the remote certificate must be validated for successful connection. If false, also disables `sslCheckCertificateRevocation`. | Both global and per session |
| sslCheckCertificateRevocation | If true and also `sslValidatePeerCertificate`=true, the remote certificate will be checked for revocation. | Both global and per session |
| sslCaCertificate       | Name of the CA certificate. Could be a file name, or a distinguished name (CN=...) of a certificate when the certificate store is used.   | Both global and per session |
| sslServerName          | Used on initiator only. Should match with CN=[serverName] in the acceptor certificate. | Both global and per session |

## How to define an SSL certificate
The `sslCertificate` configuration option can define:

* _Windows, Linux_: certificate file name, for example `sslCertificate=client.pfx`
* _Windows Certificate Store_: certificate distinguished name (see [X509FindType.FindBySubjectDistinguishedName](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509findtype?view=netstandard-2.0#System_Security_Cryptography_X509Certificates_X509FindType_FindByIssuerDistinguishedName)), for example `sslCertificate=CN=Test Client, O=Test-Certificates`
* _Windows Certificate Store_: part of the certificate subject name (see [X509FindType.FindBySubjectName](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509findtype?view=netstandard-2.0#System_Security_Cryptography_X509Certificates_X509FindType_FindBySubjectName)), for example `sslCertificate=Test-Certificates` or `sslCertificate=Test Client` for the same certificate with CN=Test Client, O=Test-Certificates.
When the Windows Certificate Store is used, the Local Machine store is searched at first, and then the Current User store is searched if nothing is found in the Local Machine store.

## How to use sslPort and requireSsl configuration options

**Acceptor:** All incoming connections to **sslPort** should be SSL/TLS. Regular TCP connections to this port are impossible. Otherwise, SSL/TLS connection to the regular listening port is unavailable too.

**Initiator:** The type of outgoing connection to a defined target port (`session.SessionId.port=...`) depends on the **requireSsl** option. If `sessions.SessionId.requireSsl`=true, the outgoing connection will be wrapped by the [SslStream](https://docs.microsoft.com/en-us/dotnet/api/system.net.security.sslstream?view=netstandard-2.0) class.

## Configuration examples

### Configure initiator and acceptor for mutual authentication using the CA certificate

**Acceptor fixengine.properties**
```INI
# SSL/TLS listening port
sslPort=3001
# name of the file with the CA certificate
sslCaCertificate=TestCA.crt
# name of the file with server's certificate
sslCertificate=server.pfx
# password for a server certificate
sslCertificatePassword=password
# secure connection configured to validate the remote peer certificate
sslValidatePeerCertificate=true

# settings specific for a session with the "sslsession" ID
sessions.sslsession.senderCompID=EchoServer
sessions.sslsession.targetCompID=SSL
sessions.sslsession.sessionType=acceptor
sessions.sslsession.requireSsl=true
```

**Initiator fixengine.properties**

```INI
# settings specific for a session with the "sslSession" ID
sessions.sslSession.senderCompID=SSL
sessions.sslSession.targetCompID=EchoServer
sessions.sslSession.sessionType=initiator
sessions.sslSession.requireSsl=true
sessions.sslSession.sslValidatePeerCertificate=true
sessions.sslSession.sslCertificate=client.pfx
sessions.sslSession.sslCertificatePassword=password
sessions.sslSession.sslCaCertificate=TestCA.crt
sessions.sslSession.sslServerName=SERVER_NAME
sessions.sslSession.port=3001
```

### Configure acceptor for accepting SSL connection from clients without an SSL certificate

```INI
...
sessions.sslSession.sslValidatePeerCertificate=false
...
```

### Use of the **requireSsl** option globally or per-session

The **requireSsl** option can be defined on three different levels:

**Global**
```INI
requireSsl=true
```

In this case, only secure connection is available.

**Sessions default**
```INI
sessions.default.requireSsl=true
...
sessions.SessionId1.requireSsl=false
...
```

In this case, all sessions are configured to use secure connections. They can also use regular (unsecure) connections if they are configured on a per-session basis (see SessionId1).

**Per-session**

```INI
sessions.default.requireSsl=false
...
sessions.SessionId2.requireSsl=true
...
```

In this case, all sessions by default use regular connections, but SessionId2 is secure.

## Diagnostics and troubleshooting

After a successful connection, the user can find information about connection parameters in the logs (DEBUG level).

**Initiator log**
```LOG
[13:10:16.612] [DEBUG] [Main] [ConnectionAuthenticator]: Secure connection information:
Protocol: Tls12
Authenticated: True
Mutually authenticated: True
Encrypted: True
Signed: True
Auth. as server: False
Revocation checked: True
Cipher: Aes256
Hash: Sha384
```

### The same port is defined for both secure and insecure connections

When the same port is defined for both secure and insecure connections, it will be used for SSL/TLS connections, not for regular ones.

For example, if the `fixengine.properties` file contains the following port definitions.

**Same port defined both for regular and secured connections**
```INI
port=3000,3001
sslPort=3001
```

the following warning message will be reported to the log:

```LOG
[2021-03-04 13:48:44.487] [ WARN] [Main] [FixServer]: Server on port 3001 has been configured already. Configuration will be overriden.
[2021-03-04 13:48:44.487] [ INFO] [Main] [CurrentDirResourceLoader]: Load resource:.\fixengine.properties
[2021-03-04 13:48:44.503] [ INFO] [Main] [FixServer]: Server started on port 3000
[2021-03-04 13:48:44.503] [ INFO] [Main] [FixServer]: Server started on port 3001 (secure)
```

### Unsuccessful connection examples

Wrong certificate file name:
```LOG
[13:18:18.941] [ERROR] [TcpServer.Thread-1] [ConnectionAuthenticator]: Cannot find certificate server.pfx.
```

Cannot validate a remote certificate:
```LOG
[13:22:34.467] [ERROR] [TcpServer.Thread-1] [ConnectionAuthenticator]: The remote certificate is invalid according to the validation procedure.
```

Wrong password for a certificate:
```LOG
[13:25:28.526] [ERROR] [Main] [ConnectionAuthenticator]: The specified network password is not correct.
```

Incompatible SslProtocol:
```LOG
[13:33:00.037] [ERROR] [Main] [ConnectionAuthenticator]: Authentication failed, see inner exception.
```

### Self-signed certificates
When self-signed certificates are used, and these certificates are issued by a non-trusted CA, it could be nesessary to turn certificate revocation check off by configuration (if a CA certificate like this is placed in the Windows Certificate Store):
```INI
sslCheckCertificateRevocation=false
```

### Simplest scenario

Below, the code blocks specify the simplest scenario when both sides of the connection have `sslValidatePeerCertificate`=false and the client connecting to the server doesn't have a certificate configured.

**Server configuration**
```INI
sslPort=3001
sslCaCertificate=TestCA.crt
sslCertificate=server.pfx
sslCertificatePassword=password
sslValidatePeerCertificate=false
```

**Server connection info**
```LOG
[13:42:53.705] [DEBUG] [TcpServer.Thread-1] [ConnectionAuthenticator]: Secure connection information:
Protocol: Tls12
Authenticated: True
Mutually authenticated: False
Encrypted: True
Signed: True
Auth. as server: True
Revocation checked: False
Cipher: Aes256
Hash: Sha384
```

**Client configuration**
```INI
sessions.sslSession.requireSsl=true
sessions.sslSession.sslValidatePeerCertificate=false
sessions.sslSession.port=3001
```

**Client connection info**
```LOG
[13:42:53.732] [DEBUG] [Main] [ConnectionAuthenticator]: Secure connection information:
Protocol: Tls12
Authenticated: True
Mutually authenticated: False
Encrypted: True
Signed: True
Auth. as server: False
Revocation checked: False
Cipher: Aes256
Hash: Sha384
```
