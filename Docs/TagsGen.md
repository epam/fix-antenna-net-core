# Tags Generator tool
## Requirements: 
.NET 6.0

## Command line arguments
Usage: TagsGen --in inputFile1 [inputFile2 inputFile3] --out GeneratedFolderName --ns NameSpace --proj ProjectName --build (Debug|Release)

- --in (-i) - one or more files with FIX dictionaries in the FixAntenna format to be processed - (required, default)
- --out (-o) - path to the directory where generated sources are to be placed (required)
- --ns (-n) - namespace for generated classes (optional)
- --proj (-p) - name of the generated project (optional)
- --build (-b) - if provided, the generated sources will be compiled automatically (optional) and configuration for compilation (Debug or Release) (optional, configuration Debug by default)

Input files could be one or more FixDic files in XML format. A list is space or comma separated. If some of the provided files doesn't exist, a warning will be added to the log. If no files exist - warning to log and exit. If some of the input files couldn't be processed - error to log and exit.

Output directory can be an absolute or relative path. If the provided path exists, a warning is sent to the log and asks the user if the directory content should be overwritten. If Yes, it deletes all existing content. If No, it exits without processing.

Namespace is the prefix for generated namespaces. Each generated source file has a namespace created from the file name by converting to PascalCase. A user-provided namespace will be prepended to the generated part. For example: a dictionary named Dictionary1.xml will produce classes in the Dictionary1 namespace. If a user specifies SomeNamespace as -ns parameter, generated files will be created with a SomeNamespace.Dictionary1 namespace.

The project name parameter will be used as the name of the project for generated source files. If more than one dictionary is provided and no -proj paremeter is provided, then the name of the first dictionary will be used as the name of the project. The namespace will be constructed as [NameSpace].[ProjectName].[DictionaryName].[GeneratedClassName]

The build parameter also executes building of the generated sources. By default, Debug configuration will be used. The user can specify Release configuration if needed.

### Supported dictionaries
"Update" types of FIX Antenna dictionaries are not supported.

TagsGen supports input files in QuickFIX dictionary format. No additional configuration needed.

Please note that QuickFIX dictionary files should follow particular naming conventions. The name of the file should match the FIX version described in the dictionary, i.e. FIX44.xml for FIX 4.4.

## Generated source files organization
The user must provide output directory as a parameter. 
```
OutputDirectory
|-ProjectName.csproj (from first dictionary or from command line argument)

|-Dictionary1
   |-Tags.cs - contains all tags defined in the dictionary
   |-Type1.cs (for example, MsgType.cs) - contains all the constants for the type
   |-Type2.cs
   ...
|-Dictionary2
  |-Tags.cs
  |-Type1.cs
  ...
...
```

## Generated source files structure
For each input, a dictionary list of files should be created:

Tags.cs - with all tags defined in the dictionary, with no duplicates

Type1.cs... (for example, MsgType.cs) - list of files generated from the dictionary

## Tags generation
Tags generated from the ```<fielddef>``` element. 
### Sample dictionary and generated source:

```xml
<fielddef tag="1" name="Account" type="String">
  <descr>
    <p>Account mnemonic as agreed between buy and sell sides, e.g. broker and institution or investor/intermediary and fund manager.</p>
</descr>
</fielddef>
<fielddef tag="2" name="AdvId" type="String">
  <descr>
    <p>Unique identifier of <msgref msgtype="7">Advertisement</msgref> message.</p>
    <p>(Prior to FIX 4.1 this field was of type int)</p>
  </descr>
</fielddef>
```
### Generated source:
```csharp
namespace Namespace.[Project|Dictionary1]
{
  /// <summary>
  /// Contains tags defined in the Dictionary1 file.
  /// </summary>
  public class Tags
  {
    /// <summary>
    /// Account mnemonic as agreed between buy and sell sides, e.g. broker and institution or investor/intermediary and fund manager.
    /// </summary>
    public const int Account = 1;
 
    /// <summary>
    /// Unique identifier of Advertisement message. (Prior to FIX 4.1 this field was of type int)
    /// </summary>
    public const int AdvId = 2;
  }
}
```

## Types generation
Each "type" defined in the dictionary should produce a separate source file, named accordingly. 

Types generated from the ```<valblockdef>``` element or from ```<fileddef>``` element:
```xml
<valblockdef id="Side" name="Side">
  <item val="1">Buy</item>
  <item val="2">Sell</item>
  <item val="3">Buy minus</item>
  <item val="4">Sell plus</item>
  <item val="5">Sell short</item>
  <item val="6">Sell short exempt</item>
  <item val="7" id="UNDISCLOSED">Undisclosed (valid for IOI and List Order messages only)</item>
  <item val="8" id="CROSS">Cross (orders where counterparty is an exchange, valid for all messages except IOIs)</item>
  <item val="9">Cross short</item>
  <item val="A">Cross short exempt</item>
  <item val="B" id="AS_DEFINED">"As Defined" (for use with multileg instruments)</item>
  <item val="C" id="OPPOSITE">"Opposite" (for use with multileg instruments)</item>
  <item val="D" id="SUBSCRIBE">Subscribe (e.g. CIV)</item>
  <item val="E" id="REDEEM">Redeem (e.g. CIV)</item>
  <item val="F" id="LEND">Lend (FINANCING - identifies direction of collateral)</item>
  <item val="G" id="BORROW">Borrow (FINANCING - identifies direction of collateral)</item>
</valblockdef>
 
<fielddef tag="40" name="OrdType" type="char">
  <item val="1">Market</item>
  <item val="2">Limit</item>
  <item val="3" id="STOP">Stop</item>
  <item val="4">Stop limit</item>
  <item val="6">With or without</item>
  <item val="7" id="LIMIT_OR_BETTER">Limit or better (Deprecated)</item>
  <item val="8">Limit with or without</item>
  <item val="9">On basis</item>
  <item val="D">Previously quoted</item>
  <item val="E">Previously indicated</item>
  <item val="G" id="FOREX_SWAP">Forex - Swap</item>
  <item val="I" id="FUNARI">Funari (Limit Day Order with unexecuted portion handled as Market On Close. e.g. Japan)</item>
  <item val="J" id="MIT">Market If Touched (MIT)</item>
  <item val="K" id="MARKET_WITH_LEFTOVER_AS_LIMIT">Market with Leftover as Limit (market order then unexecuted quantity becomes limit order at last price)</item>
  <item val="L" id="PREVIOUS_FUND_VALUATION_POINT">Previous Fund Valuation Point (Historic pricing) (for CIV)</item>
  <item val="M" id="NEXT_FUND_VALUATION_POINT">Next Fund Valuation Point (Forward pricing) (for CIV)</item>
  <item val="P" id="PEGGED">Pegged</item>
  <descr>
    <p>Order type.</p>
  </descr>
</fielddef>
```
### Generated source Side.cs
```csharp
/// <summary>
/// Side
/// </summary>
public class Side
{
  /// <summary>
  /// Buy
  /// </summary>
  public const string Buy = "1";
  /// <summary>
  /// Sell
  /// </summary>
  public const string Sell = "2";
  /// <summary>
  /// Buy minus
  /// </summary>
  public const string BuyMinus = "3";
  /// <summary>
  /// Sell plus
  /// </summary>
  public const string SellPlus = "4";
  /// <summary>
  /// Sell short
  /// </summary>
  public const string SellShort = "5";
  /// <summary>
  /// Sell short exempt
  /// </summary>
  public const string SellShortExempt = "6";
  /// <summary>
  /// Undisclosed (valid for IOI and List Order messages only)
  /// </summary>
  public const string Undisclosed = "7";
  /// <summary>
  /// Cross (orders where counterparty is an exchange, valid for all messages except IOIs)
  /// </summary>
  public const string Cross = "8";
  /// <summary>
  /// Cross short
  /// </summary>
  public const string CrossShort = "9";
  /// <summary>
  /// Cross short exempt
  /// </summary>
  public const string CrossShortExempt = "A";
  /// <summary>
  /// "As Defined" (for use with multileg instruments)
  /// </summary>
  public const string AsDefined = B;
 
...
}
```
