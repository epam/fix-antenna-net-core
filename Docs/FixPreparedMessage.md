# FIX Prepared Message
## Create message
A prepared message can be created in three different ways:
1. Create a skeleton and set fields
2. Parse a raw FIX message (byte array)
3. Create a message from an existing `FixMessage`

To create a FIX prepared message, please use the `IFixSession.PrepareMessage()` and `IFixSession.PrepareMessageFromString()` methods.
Thereafter, you can set/change the desired field values of a message and send it to the counterparty many times.

For example:
```csharp
MessageStructure ms = new MessageStructure();
ms.ReserveString(148, 11);
FixMessage pm = null;
try 
{
    pm = session.PrepareMessage("B", ms);
} 
catch (PreparedMessageException e) 
{
    //handle error of building prepared message
}

pm.Set(148, "Hello there");
session.SendMessage(pm);
```

To parse a string containing a raw FIX message, 
use the `IFixSession.PrepareMessageFromString(byte[] message, string type, MessageStructure structure)` method.

For example:
```csharp
string message = "5=ABRA\u00016=KADABRA\u0001";
MessageStructure ms = new MessageStructure();
ms.Reserve(5, 4);
ms.Reserve(6, 7);
FixMessage pm = null;
try
{
    pm = session.PrepareMessageFromString(message.AsByteArray(), "A", ms);
}
catch (PreparedMessageException e)
{
    //handle error of building prepared message
}

session.SendMessage(pm);
```

To create prepared message from a `FixMessage` object, please use the
`IFixSession.PrepareMessage(FixMessage message, string type, MessageStructure structure)` method:

```csharp
FixMessage list = new FixMessage();
list.AddTag(1, "TestValue");
MessageStructure ms = new MessageStructure();
ms.Reserve(1, 9);
FixMessage pm = null;
try 
{
    pm = session.PrepareMessage(list, "A", ms);
} 
catch (PreparedMessageException e) 
{
    //handle error of building prepared message
}

session.SendMessage(pm);
```

## Add field
To add a field to a message, use the `MessageStructure.Reserve(int tagId, int length)` method. You can also give a hint to the
builder about field types with similar methods, such as `MessageStructure.ReserveString(int tagId, int length)` and
`MessageStructure.ReserveLong(int tagId, int length)`. In the latter case, the builder will be able to prepare more a optimized
structure for a new message.

Keep in mind that all tags in a message will be in the order that you have reserved. Length is strictly fixed. If the length
of a tag is undetermined, you can use the `MessageStructure.VariableLength` constant as a length parameter in the
`Reserve(int,int)` method. In this case, length will be auto adjusted. Keep in mind that this way is much slower than using a
fixed size for the field. You will get the best perfomance if your prepared message has only fixed length fields.

If you reserved 5 bytes, the content length of this tag should be equal to 5 bytes too. Otherwise, the engine will mark the
length for the given field as undetermined (`MessageStructure.VariableLength`) automatically. However, there is an exception to
this rule: if you try to set a numeric value (int, long, double) with a lower length, the engine will automatically fill
a tag value leading with "0" to the required length.


Fixed tag value length:

```csharp
MessageStructure ms = new MessageStructure();
ms.Reserve(1, 9);
ms.ReserveLong(54, 1);
```

Variable tag value length:

```csharp
MessageStructure ms = new MessageStructure();
ms.ReserveString(58, MessageStructure.VariableLength);
```

## Set field

To change a field value, use the `FixMessage.Set()` method:

```csharp
message.Set(148, "Hello there");
message.Set(54, 1);
```

In the case of repeating groups, you can use the following way to set a concrete tag value:

```csharp
message.Set(58, 1, "Hello there");
message.Set(58, 2, "Hello there again");
```

Entries in repeating group have numbers starting from 1.
