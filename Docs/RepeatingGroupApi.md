# Repeating Group API
There is an API for Repeating Groups in FIX Antenna .NET Core. It allows creating, removing and modifying Repeating Groups and their entries.

## Indexing Repeating Group
To use the Repeating Group API, you can pass the FixMessage instance to the static method `RawFixUtil.IndexRepeatingGroup()`. There are four implementations of this method:
1. Indexes Repeating Group according to FIX dictionary. The dictionary version and message type are selected by passed arguments.
   ```csharp
   FixMessage IndexRepeatingGroup(FixMessage msg, FixVersion version, string msgType, bool validation)
   ```

2. Indexes Repeating Group according to FIX dictionary. The dictionary version and message type are selected by passed arguments and validation is turned off.

   ```csharp
   FixMessage IndexRepeatingGroup(FixMessage msg, FixVersion version, string msgType)
   ```

3. Indexes Repeating Group according to FIX dictionary. The FIX version and message type are extracted from the message.
   ```csharp
   FixMessage IndexRepeatingGroup(FixMessage msg, bool validation)
   ```

4. Indexes Repeating Group according to FIX dictionary. The FIX version and message type are extracted from the message and validation is turned off.
   ```csharp
   FixMessage IndexRepeatingGroup(FixMessage msg)
   ```

All methods return a passed message. If you try to work with the Repeating Group API without calling `RawFixUtil.IndexRepeatingGroup()`, the `IndexRepeatingGroup(FixMessage msg)` method will implicitly be called, so you get an indexed message with validation turned off.

## Working with Repeating Groups through API
There are two classes for working with Repeating Groups through an API: `RepeatingGroup` and `RepeatingGroup.Entry`. `RepeatingGroup` contains methods for managing entries of Repeating Groups. `Entry` contains methods for managing tags in a specific entry of `RepeatingGroup` and subgroups. By default, FIX Antenna uses instances of `RepeatingGroup` and `RepeatingGroup.Entry` from the internal pool to reduce garbage production.

## Repeating Group Pool
The first thing that you should know about the Repeating Group API is pooling.
Remember this simple rule: if you got an `RepeatingGroup` or `Entry` instance by calling a method that returns an instance of a group or entry, then there is no need to do so in `group.Release()` / `entry.Release()`: all groups and entries will come back to the pool after `msg.ReleaseInstance()` is called. Alternatively, if you get a group or entry by an explicit call of `RepeatingGroupPool.GetEntry()` / `GetRepeatingGroup()`, you should explicitly return a group/entry object to the pool using the Release method call.

## Get Repeating Group
There are two ways to get a Repeating Group from an indexed message:

1. Don't forget about `msg.ReleaseInstance()`!  If you don't call this method, it may cause unnecessary object creation.

   ```csharp
   FixMessage msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());
   msg = RawFixUtil.IndexRepeatingGroup(msg, true);
   
   RepeatingGroup group = msg.GetRepeatingGroup(555); //Leading tag of Repeating Group
   //Some operations
   msg.ReleaseInstance();
   ```

1. In this case, `FixMessage` filling passed a group with information about group content.

   ```csharp
   FixMessage msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());
   msg = RawFixUtil.IndexRepeatingGroup(msg, true);
   RepeatingGroup group = RepeatingGroupPool.RepeatingGroup; // Or new RepeatingGroup() or something else
   msg.GetRepeatingGroup(555, group); // Leading tag of Repeating Group
   //Some operations
   group.Release();
   ```



If you try to get a non-existent group, you get an exception (if you call the void method) or a null value (if you call the method that returns a `RepeatingGroup` instance).
There are safe methods (`GetOrAddRepeatingGroup()`) that add a repeating group if it doesn't exist.

## Get Entry
After you get a `RepeatingGroup` instance, you can get entries of this group. There are two ways to get it:

1. Don't forget about `msg.ReleaseInstance()`!  If you don't call this method, it may cause unnecessary object creation. 

   ```csharp
   FixMessage msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());
   msg = RawFixUtil.IndexRepeatingGroup(msg, true);
   
   RepeatingGroup group = msg.GetRepeatingGroup(555); //Leading tag of Repeating Group
   RepeatingGroup.Entry entry = group.GetEntry(0); //Number of entry in group
   //Some operations
   msg.ReleaseInstance();
   ```

2. In this case, `FixMessage` filling passed an entry with information about entry content.

   ```csharp
   FixMessage msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());
   msg = RawFixUtil.IndexRepeatingGroup(msg, true);
   RepeatingGroup group = RepeatingGroupPool.RepeatingGroup; // Or new RepeatingGroup() or something else
   RepeatingGroup.Entry entry = RepeatingGroupPool.Entry;
   msg.GetRepeatingGroup(555, group); // Leading tag of Repeating Group
   group.GetEntry(0, entry); //Number of entry in group
   //Some operations
   entry.Release();
   group.Release();
   ```

## Get nested group
You can get nested a group using the method `Entry.GetGroup()`:

```csharp
   FixMessage msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());
   msg = RawFixUtil.IndexRepeatingGroup(msg, true);
   RepeatingGroup group = RepeatingGroupPool.RepeatingGroup; // Or new RepeatingGroup() or something else
   RepeatingGroup.Entry entry = RepeatingGroupPool.Entry;
   msg.GetRepeatingGroup(555, group); // Leading tag of Repeating Group
   group.GetEntry(0, entry); //Number of entry in group

   RepeatingGroup subGroup = entry.GetRepeatingGroup(Tags.NoLegSecurityAltID);

   //Some operations
   entry.Release();
   msg.ReleaseInstance();
```

## Add new Repeating Group to message
To create a Repeating Group, use the method `AddRepeatingGroupAtIndex`:
```csharp
FixMessage msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());
msg = RawFixUtil.IndexRepeatingGroup(msg, true);

int repeatingGroupIndex = 20; //Index in message
int leadingTag = 555; //Leading tag for group
bool validation = true; //Turn on validation
RepeatingGroup group = msg.AddRepeatingGroupAtIndex(repeatingGroupIndex, leadingTag, validation);
//Some operations
msg.ReleaseInstance();
```

The parameter `repeatingGroupIndex` is an index of leading tags in a FIX message. The first tag has an index = 0. Parameter validation enables or disables validation of the group. Information about validation is below.
You can also add a nested group to an entry:

```csharp
   FixMessage msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());
   msg = RawFixUtil.IndexRepeatingGroup(msg, true);
   RepeatingGroup group = RepeatingGroupPool.RepeatingGroup; // Or new RepeatingGroup() or something else
   RepeatingGroup.Entry entry = RepeatingGroupPool.Entry;
   msg.GetRepeatingGroup(555, group); // Leading tag of Repeating Group
   group.GetEntry(0, entry); //Number of entry in group

   int leadingTag = Tags.NoLegSecurityAltID;
   bool validation = true;
   RepeatingGroup subGroup = entry.AddRepeatingGroup(Tags.NoLegSecurityAltID, validation);

   //Some operations
   entry.Release();
   group.Release();
   msg.ReleaseInstance();
```

The parameter `validation` is optional for creating a repeating group. If it is ommited, its value will be inherited from the parent group.

## Add new Entry to Repeating Group
To create a new Entry, use the method `AddEntry()`:

```csharp
   FixMessage msg = RawFixUtil.GetFixMessage(executionReport.AsByteArray());
   msg = RawFixUtil.IndexRepeatingGroup(msg, true);
   RepeatingGroup group = RepeatingGroupPool.RepeatingGroup; // Or new RepeatingGroup() or something else
   RepeatingGroup.Entry entry = RepeatingGroupPool.Entry;
   msg.GetRepeatingGroup(555, group); // Leading tag of Repeating Group

   //Add at end
   Entry entry1 = group.AddEntry();
   //Add at index. First index = 0
   Entry entry2 = group.AddEntry(1);

   entry.Release();
   group.Release();
   msg.ReleaseInstance();
```

If you pass an index greater than the current group size or less than zero, you will get `IndexOutOfRangeException`.

## Remove Entry from Repeating Group
There are a few methods to remove an entry from a group:
```csharp
group.Remove(index); //remove entry by index
entry.Remove(); //remove current entry, but doesn't return it in pool
group.Remove(entry); //remove passed entry, but doesn't return it in pool
```

## Leading tag self-maintaining
The leading tag of a group is fully self-maintaining. You can't update it directly. The leading tag doesn't appear in a message until the group is empty.
When a group becomes empty, the leading tag is removed from the message.

```csharp
FixMessage msg = new FixMessage();
msg.Set(8, "FIX.4.4");
msg.Set(35, "8");
msg.Set(10, 123);

RepeatingGroup group = msg.AddRepeatingGroupAtIndex(555, 2);
Console.WriteLine(msg.ToPrintableString()); //8=FIX.4.4 | 35=8 | 10=123  group is empty, so it doesn't show in message

RepeatingGroup.Entry entry1 = group.AddEntry();
RepeatingGroup.Entry entry2 = group.AddEntry();
Console.WriteLine(msg.ToPrintableString()); //8=FIX.4.4 | 35=8 | 10=123  group have two entries now, but all entry is empty, so group still doesn't show in message
Console.WriteLine(group.Count); // Prints 2, method getSize returns number of all groups - empty and non empty
Console.WriteLine(group.GetLeadingTagValue()); // Prints 0

entry1.AddTag(600, "TBD");
Console.WriteLine(msg.ToPrintableString()); //8=FIX.4.4 | 35=8 | 555=1 | 600=TBD | 10=123  group appear in message.
Console.WriteLine(group.Count); // Prints 2
Console.WriteLine(group.GetLeadingTagValue()); // Prints 1

entry1.RemoveTag(600);
Console.WriteLine(msg.ToPrintableString()); //8=FIX.4.4 | 35=8 | 10=123  group is removed from message because there is no non-empty entries
Console.WriteLine(group.Count); // Prints 2, method getSize returns number of all groups - empty and non empty
Console.WriteLine(group.GetLeadingTagValue()); // Prints 0
```

When we add two or more groups at the same place in a message, the behavior is the same as when we add two tags in the same place in a message:
```csharp
FixMessage msg = new FixMessage();
msg.Set(8, "FIX.4.4");
msg.Set(35, "8");
msg.Set(10, 123);

RepeatingGroup group555 = msg.AddRepeatingGroupAtIndex(555, 2);
RepeatingGroup group454 = msg.AddRepeatingGroupAtIndex(454, 2); //group 555 will appear right after group 454

group555.AddEntry().AddTag(600, "TBD");
Console.WriteLine(msg.ToPrintableString()); //8=FIX.4.4 | 35=8 | 555=1 | 600=TBD | 10=123
group454.AddEntry().AddTag(455, 5);
Console.WriteLine(msg.ToPrintableString()); //8=FIX.4.4 | 35=8 | 454=1 | 455=5 | 555=1 | 600=TBD | 10=123
RepeatingGroup group = msg.AddRepeatingGroupAtIndex(555, 2);
```

Nested repeating groups also have self-maintained leading tags.

When we add two nested groups one by one, they will appear in the order they were added:
```csharp
   RepeatingGroup.Entry entry = group555.AddEntry();
   entry.AddTag(600, "TBD");
   RepeatingGroup group604 = entry.AddRepeatingGroup(604);
   RepeatingGroup group539 = entry.AddRepeatingGroup(539);

   Console.WriteLine(msg.ToPrintableString()); //8=FIX.4.4 | 35=8 | 454=1 | 455=5 | 555=1 | 600=TBD | 10=123 sub groups 604 and 539 is empty, so they doesn't show

   group539.AddEntry().AddTag(524, "524val");
   Console.WriteLine(msg.ToPrintableString()); //8=FIX.4.4 | 35=8 | 454=1 | 455=5 | 555=1 | 600=TBD | 525=1 | 524=524val | 10=123
   group604.AddEntry().AddTag(605, "605val");
   Console.WriteLine(msg.ToPrintableString()); //8=FIX.4.4 | 35=8 | 454=1 | 455=5 | 555=1 | 600=TBD | 604=1 | 605=605val | 525=1 | 524=524val | 10=123
```

All methods that work with the index inside an entry like `RemoveTagAtIndex` and `GetTagValueAsXXXAtIndex` do not work with empty groups:
```csharp
RepeatingGroup.Entry entry = group555.AddEntry();
entry.AddTag(600, "TBD");
RepeatingGroup group604 = entry.AddRepeatingGroup(604);

entry.RemoveTagAtIndex(1); //returns false, because there is no any real tag at index 1 inside entry.
```

## ITagList interface
There is a common interface for `FixMessage` and `RepeatingGroup.Entry` for `Add`, `Get`, `Update`, and `Remove` operations on tags in an entry:

```csharp
ITagList entry = group.GetEntry(0);
entry.AddTag(tag, tagValue);
entry.UpdateValue(tag, tagValue, missingTagHandling);
entry.RemoveTag(tag);
entry.GetTagValueAsString(tag);
//And others
```

## Validation
Validation of repeating groups is available during the initial indexing of a Repeating Group and during the modification of a Repeating Group.

During indexing there are the following validations:
1. Leading tag value validation. The leading tag value must be equal to the number of Repeating Group entries.
2. Duplicate tag validation. All tags inside an entry must be unique.
3. Delimiter tag validation. The delimiter tag must be followed by a leading tag.

During modification of a Repeating group there are the following validations:
1. Tag refers to group validation. Must be mapping group -> tag in dictionary.
2. Duplicate tag validation. All tags inside an entry must be unique.

## Copying
It is possible to copy a repeating group and entry to another message or group.
To copy a repeating group from one FIX message to another, use the method `FixMessage.CopyRepeatingGroup`:
```csharp
RepeatingGroup srcGroup = srcMsg.GetRepeatingGroup(232);
RepeatingGroup copiedGroup = dstMsg.CopyRepeatingGroup(srcGroup, 9); // group for copy and index where group will be inserted
```

To copy an entry of a repeating group to another group, use the method `RepeatingGroup.CopyEntry`:
```csharp
RepeatingGroup groupForCopy = srcMsg.GetRepeatingGroup(454);
RepeatingGroup.Entry entryForCopy = groupForCopy.GetEntry(1);
RepeatingGroup tarGetGroup = dstMsg.GetRepeatingGroup(454);
```

Entries can be copied to the same message or to another message.
To copy a nested repeating group, use the method `Entry.CopyRepeatingGroup`:
```csharp
RepeatingGroup groupForCopy = srcMsg.GetRepeatingGroup(555).GetEntry(0).GetRepeatingGroup(539);
RepeatingGroup.Entry tarGetEntry = dstMsg.GetRepeatingGroup(555).GetEntry(0);

RepeatingGroup copiedGroup = tarGetEntry.CopyRepeatingGroup(groupForCopy);
```

You can insert a nested group in any entry or even in the root of message without limitation.

## Finishing work with Repeating Group API.
If you created a `FixMessage` message from a pool, calling `ReleaseInstance()` will return all RG instances, as well as the message itself, to corresponding pools. Alternatively, you should call the method `InvalidateRepeatingGroupIndex()` at the end of the work with the Repeating Group API. Don't forget to call the methods `Release()` for `RepeatingGroup.Entry` and `RepeatingGroup` if you got it from the pool explicitly.
