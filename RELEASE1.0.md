# Version 1.0 Release Notes

## Breaking Changes
Version 1.0 API does not compatible with versions 0.9.x API.

### All namespaces reorganized and renamed with 'Epam.' prefix
For example, in version 1.0
```csharp
namespace Epam.FixAntenna.NetCore.FixEngine
{
    public class SessionParameters : ICloneable
    {
```
In version 0.9.x
```csharp
namespace FixAntenna.FixEngine
{
    public class SessionParameters : ICloneable
    {
```
### Many methods converted to properties
For example, in version 1.0:
```csharp
/// <summary>
/// Gets or sets heartbeat interval.
/// </summary>
public int HeartbeatInterval { get; set; } = 30;
```
In version 0.9.x:
```csharp
/// <summary>
/// Gets heartbeat interval.
/// </summary>
public virtual int GetHeartbeatInterval()
{
    return HeartbeatInterval;
}
 
/// <summary>
/// Sets heartbeat interval.
/// </summary>
/// <param name="heartbeatInterval"> the HBI, default value is 30. </param>
public virtual void SetHeartbeatInterval(int heartbeatInterval)
{
    HeartbeatInterval = heartbeatInterval;
}
```
### Number of binaries reduced
| v1.0 | v0.9.x |
|---|---|
|Epam.FixAntenna.Dialects.dll<br>Epam.FixAntenna.NetCore.dll|FixAntenna.AdminTool.dll<br>FixAntenna.Common.dll<br>FixAntenna.Configuration.dll<br>FixAntenna.Core.dll<br>FixAntenna.Dictionary.dll<br>FixAntenna.Encryption.dll<br>FixAntenna.Message.dll<br>FixAntenna.Tester.dll<br>FixAntenna.Validation.dll<br>FixAntenna.XmlBeans.dll|

## Features and improvements
### QuickFIX dictionaries support
Dictionaries in QuickFIX format can be used to start FIX sessions 
### Assigning an individual dictionary to a specific FIX session
An individual dictionary can be assigned to a specific FIX session in the property file
### Using environment variables for configuration
Configuration parameters can be defined in environment variables. Environment variables have the highest priority
### Tag generation tool
The tag generation tool generates a set of FIX-dictionary tags as a set of constants inside a DLL file. Thus, the human-readable names of the tags and tag values can be used instead of their numbers and numeric values (for standard values)
### Asynchronous connection
API supports the asynchronous session connection. ConnectAsync() method was introduced to the IFixSession interface:
```csharp
/// <summary>
/// Connects to remote counterparty, if initiator
/// or accepts incoming connection if acceptor.
/// Async version
/// </summary>
/// <exception cref="IOException"> I/O exception if error occurred </exception>
Task ConnectAsync();
```