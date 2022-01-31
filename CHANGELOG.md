# Changelog

## 1.0.2 (2022-02-01)

### Released under Apache 2.0

### Features and improvements
#### QuickFIX dictionaries support
Dictionaries in QuickFIX format can be used for FIX sessions 
#### Assigning an individual dictionary to a specific FIX session
An individual dictionary can be assigned to a specific FIX session in the property file
#### Using environment variables for configuration
Configuration parameters can be defined in environment variables. Environment variables have the highest priority
#### Tag generation tool
The tag generation tool generates a set of FIX-dictionary tags as a set of constants inside a DLL file. Thus, the human-readable names of the tags and tag values can be used instead of their numbers and numeric values (for standard values)
#### Asynchronous connection
API supports the asynchronous session connection. `ConnectAsync()` method was introduced to the `IFixSession` interface

### Breaking changes
Please refer [Release Notes](RELEASE1.0.md) for details.

## 0.9.4 (2021-05-25)
### Features and improvements.
#### TLS Support has been implemented
For samples and details, refer to the [TLS Support](Docs/TlsSupport.md).
The implementation uses the [System.Net.Security.SslStream](https://docs.microsoft.com/en-us/dotnet/api/system.net.security.sslstream?view=netstandard-2.0) class, as well as other types from [System.Net.Security](https://docs.microsoft.com/en-us/dotnet/api/system.net.security?view=netstandard-2.0), [System.Security.Authentication](https://docs.microsoft.com/en-us/dotnet/api/system.security.authentication?view=netstandard-2.0), [System.Security.Cryptography.X509Certificates](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates?view=netstandard-2.0) namespaces.
Support of different certificate types depends only on the [X509Certificate2](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2?view=netstandard-2.0) class.
#### Added configuration option seqNumLength to configure minimal length of SeqNum fields
If configured length is more than actual length, the value will be prepended with leading zeroes.
For the details see [Configuration](Docs/Configuration.md).
#### Added API method FixMessage.TryGetLongByIndex(int index, out long value)
The method returns true if a tag exists and a value could be parsed as long, and returns the parsed value as out long value. If no tag exists or a value cannot be parsed as long, it returns false. 

## 0.9.3 (2020-12-10)
### Features and improvements
#### Hide passwords in FIX logs and application logs
User defined tag values in both the .in and .out log files are masked with asterisks. This is done to obfuscate sensitive information (such as logins and passwords) by applying masks. The tag values are obfuscated with asterisks. The number of asterisks depends on the field length.
#### Implement compatibility with Log Replicator
FIX log index files from `FileSystemStorageFactory` and `MmfStorageFactory` now binary compatible with Log Replication tool.

## 0.9.2 (2020-08-31)
### Features and improvements
#### Microsoft .NET Framework 4.8 compatibility