# FIX Message
## Create
A message can be created in two different ways:
1. Create a skeleton and set fields
2. Parse a raw FIX message (byte array)

To create a framework for a FIX application-level message, use the `FixMessage` class.
Thereafter, you can set the desired field values of a message and send it to the counterparty.
In general, `FixMessage` is just an `IList<TagValue>`, so it contains all the methods from `System.Collections.Generic.IList` plus a few
convenient methods to manipulate tags and groups of tags.

For example:

```csharp
FixMessage messageContent = new FixMessage();
messageContent.AddTag(148, "Hello there");
```

To parse a string containing a raw FIX message into the `FixMessage` class,
use the `RawFixUtil.GetFixMessage(byte[] message)` method.
For example:

```csharp
byte[] bytes =
    "8=FIX.4.3\u00019=94\u000135=A\u000149=target\u000156=sender\u0001115=onBehalf\u000134=1\u000150=senderSub\u000152=20080212-04:15:18.308\u000198=0\u0001108=600\u000110=124\u0001".AsByteArray();

FixMessage msg = RawFixUtil.GetFixMessage(bytes);
```

## Get field

To get a FIX field value, use the `FixMessage.GetTagValueAsXXX(int tag)` methods. For example, if you need a string
value please use `FixMessage.GetTagValueAsString(int tag)`, if you need int - `FixMessage.GetTagValueAsInt(int tag)`, etc.

Please pay attention that `FixMessage.GetTagValueAsString(int tag)` and `FixMessage.GetTagValueAsBytes(int tag)`
return a null if these tags are not present in message. The rest methods for accessing a value as a primitive type throw
an `FieldNotFoundException` if there is no such tag.

## Add field
To add a field to message please, use a set of `FixMessage.AddTag()` methods with different parameters.

## Set field

To change a field value, please use `FixMessage.Set()` methods:

```csharp
message.Set(148, "Hello there");
```

Note: the tag will be added to the message if this type of tag is absent.

## Remove field

To remove a field by tag, use the `FixMessage.RemoveTag(int tag)` method.
This method returns "true" if the given tag is found and the field is removed. Otherwise, the method returns "false".

```csharp
message.RemoveTag(148);
```

## Repeating group

To get a value from a repeating group, use `FixMessage.GetTagValueAsXXX(int tag, int occurrence)`
Example:
```csharp
FixMessage messageContent = new FixMessage(); // prepare message
messageContent.AddTag(148, "Hello there"); // Add Subject
messageContent.AddTag(33, 3); // Add Repeating group
messageContent.AddTag(58, "line1");
messageContent.AddTag(58, "line2");
messageContent.AddTag(58, "line3");

Console.WriteLine(messageContent.GetTagValueAsString(58, 2));
```


Another more convenient option is to use `FixMessage.Split(int leadingTagNumber)`

```csharp
FixMessage messageContent = new FixMessage(); // prepare message
messageContent.AddTag(148, "Hello there"); // Add Subject
messageContent.AddTag(33, 3); // Add Repeating group
messageContent.AddTag(58, "line1");
messageContent.AddTag(58, "line2");
messageContent.AddTag(58, "line3");

IList<FixMessage> repeatingGroups = messageContent.Split(58);

// we have a list of 3 groups and each group in our case
// contains a repeating group with single 58 tag.
foreach (FixMessage repeatingGroup in repeatingGroups)
{
    Console.WriteLine(repeatingGroup.GetTagValueAsString(58)); // line1 line2 line3
}
```

## User defined fields
User Defined Fields (tag numbers from 5000 to 9999) are handled like ordinary fields.

For example:
```csharp
int theCustomTag = 5000; // reserved for user defined fields
FixMessage messageContent = new FixMessage(); // generates a new FIX message
messageContent.AddTag(theCustomTag, "0.15"); // sets the tag value
```

## Clone message

To clone a FIX message, use the `FixMessage.Clone()` method.
