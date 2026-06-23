# 05 Custom Dictionary / Custom Tags

> Targets **Epam.FixAntenna.NetCore 1.2.3**. See `../SKILL.md` for root API rules.

## Pattern

Add venue-specific custom tags or custom message types via FIX dictionary XML, so they validate and parse like standard fields.

```
[FIX dictionary XML]    ──load──►  [engine]
       ▲                              │
       │                              ▼
  add custom tag X            FixMessage.GetTagValueAsString(X) works
  add custom message YYY      MsgType="YYY" is accepted, not rejected
```

## When to use

- Venue ships a "FIX 4.4 + extensions" spec with tags 5000+.
- An exchange-proprietary message type is needed (e.g., custom mass-quote variant).
- Modeling internal-only messages between in-house FIX systems.

## When NOT to use

- The "custom" tag is actually a standard tag missed. Always check the FIX spec for the version first.
- The custom tag is one-off and only needed in one message — `FixMessage.AddTag(5001, value)` still works without dictionary changes, but validation is lost. Acceptable for prototypes; not for production.

## Dictionary mechanics

FIX Antenna loads a dictionary XML at startup (per FIX version). To extend it:

1. **Locate the base dictionary** for the FIX version (e.g., `fixdic44.xml`, shipped under the engine's `Dictionaries/` folder; root element `<fixdic>`).
2. **Add custom field definitions** in the `<fielddic>` section. The element is **`<fielddef>`**, the tag-number attribute is **`tag`**, and `type` uses the dictionary's PascalCase type names (`String`, `int`, `char`, `Price`, `Qty`, `Amt`, `Length`, `Boolean`, …) — **not** `<field number=... type="STRING">`:
   ```xml
   <fielddef tag="5001" name="MyCustomTag" type="String"/>
   ```
3. **Add custom messages** in the `<msgdic>` section as `<msgdef>`, referencing fields with `<field tag=... req="Y|N"/>` (note: inside `<msgdef>` a field reference is `<field tag="..." req="...">`, distinct from the `<fielddef>` definition in step 2):
   ```xml
   <msgdef msgtype="U1" name="MyCustomMessage">
     <field tag="5001" req="Y"/>
   </msgdef>
   ```
4. **Reference the extended dictionary** in `fixengine.properties` via the verified `customFixVersions` pattern (see below).
5. **Restart the session** — dictionaries load at start, not hot.

### Verified attach pattern (1.2.3)

Verified against `FixAntenna/NetCore/Configuration/Config.cs` constants (`CustomFixVersions`, `CustomFixVersionPrefix`, `CustomFixVersionVersionSuffix`, `CustomFixVersionFileNameSuffix`) and `Docs/Configuration.md`.

```properties
# 0) Master validation switch — default false in 1.2.3 (Config.cs: "validation").
#    Without this line the engine does NOT validate inbound messages against any
#    dictionary, custom or standard.
validation = true

# 1) Declare custom-version aliases (comma-separated list)
customFixVersions = FIX44Custom, FIX50Custom

# 2) For each alias, declare (a) the base standard FIX version it extends
#    and (b) the extended dictionary XML file — a COMPLETE dictionary, see below.
#    A relative fileName is resolved under ./Dictionaries/ first (resolution
#    order below); place the files there, or use an absolute path.
customFixVersion.FIX44Custom.fixVersion = FIX.4.4
customFixVersion.FIX44Custom.fileName   = fixdic44-custom.xml

customFixVersion.FIX50Custom.fixVersion = FIX.5.0
customFixVersion.FIX50Custom.fileName   = fixdic50-custom.xml

# 3) Assign the alias to a session via `sessions.<id>.fixVersion`.
sessions.acmeSession.fixVersion = FIX44Custom

# For FIX 5.0SP2 over FIXT.1.1, use BOTH fixVersion (FIXT11) and appVersion:
sessions.acmeT11Session.fixVersion = FIXT11
sessions.acmeT11Session.appVersion = FIX50Custom
```

Important properties of this scheme:
- A `customFixVersion.<alias>.fileName` must be a **complete** dictionary — it fully replaces the base `fixdic*.xml` for that alias. To ship only the delta, use the extension mechanism below instead.
- `fileName` resolution (verified in `Common/ResourceLoading/ResourceLoader.cs`, dictionary loader chain): an absolute path loads directly; a relative path is looked up under `./Dictionaries/` in the working directory, then `Dictionaries/` next to the engine assembly, then the embedded resource dictionaries.
- One file per base FIX version. Sharing the same physical XML across FIX 4.2/4.4/5.0 is structurally wrong (different base dictionaries) — declare a separate alias for each base version, even if the custom extensions are identical in content.
- QuickFIX-format dictionaries are accepted too: the loader auto-detects the QuickFIX XML format and converts it on load via the embedded `qfix2fixdic.xsl` transform (`Common/ResourceLoading/DictionaryLoader.cs`).

### Validation is OFF by default (1.2.3)

The master switch `validation` defaults to **false** (`Config.cs`). With the default, the engine attaches the dictionary for parsing/group navigation but performs **no validation** of inbound messages — unknown tags are accepted as-is, with no error. To validate inbound messages against the (custom) dictionary:

```properties
validation = true
```

The granular toggles — `allowedFieldsValidation`, `requiredFieldsValidation`, `fieldTypeValidation`, `conditionalValidation`, `groupValidation`, `duplicateFieldsValidation`, `fieldOrderValidation`, `wellformenessValidation` (each defaults to true) — only act when `validation = true`. When `validation = false` they all read as false; setting `allowedFieldsValidation = false` by itself is a no-op in a default config.

### Shipping only the delta — extension dictionaries

Instead of maintaining a full copy of `fixdic44.xml`, attach an extension file on top of a base dictionary (verified in `Configuration/FixVersionContainerFactory.cs`):

```properties
# <dictionaryId> is the standard id, e.g. FIX44 for FIX.4.4.
# Same fileName resolution as above: place the file in ./Dictionaries/
# or use an absolute path.
validation.FIX44.additionalDictionaryFileName = fixdic44-extension.xml
# true (default) = merge the extension over the base dictionary
# false          = the file replaces the base dictionary entirely
validation.FIX44.additionalDictionaryUpdate   = true
```

With `additionalDictionaryUpdate = true` the extension XML needs to contain only the custom `<fielddef>`/`<msgdef>` entries — the base dictionary supplies everything else. (Despite the `validation.` prefix, this is how the dictionary itself is composed; it applies to parsing/group navigation too.)

### Running without a custom dictionary (legacy / prototype only)

If authoring an extended XML is not feasible for a one-off case, nothing needs to be disabled in a default 1.2.3 config: with `validation = false` (the default) the engine already accepts unknown tag numbers without complaint. What is lost without a dictionary entry:

- No validation of the custom block — missing required tags or wrong types pass silently (and stay unvalidated even with `validation = true`, since the dictionary doesn't know the tags).
- No structural access — `IsRepeatingGroupExists(7010)` returns `false`. Repeating-group recognition is **dictionary-driven** (`Message/IndexedStorage.cs` initializes the group storage from the loaded dictionary), independent of the validation toggles: the group is invisible because tag 7010 isn't declared as a NumInGroup leading tag in the loaded dictionary. Code falls back to flat-tag access via `FixMessage.GetTagValueAsString(int)`.

This is a prototype-grade fallback, not a production answer.

## Custom tag rules

| Range | Meaning |
|---|---|
| **1–4999** | Reserved for FIX standard (don't use). |
| **5000–9999** | User-defined range, per FIX spec. Use these. |
| **10000+** | "Reserved 100" range, conventionally for further user extensions. |

## Custom message types

- Use single-letter or multi-character `MsgType` not already taken by FIX (avoid `A`, `D`, `8`, `0`, `1`, `2`, `3`, `4`, `5`, etc.).
- Document required tags in the dictionary so the engine validates incoming instances.
- The dictionary defines which fields are required, optional, or part of repeating groups.

## Reading / writing a custom tag

Tag constants for a custom dictionary can be generated with the **TagsGen** tool that ships in the source repo (`Docs/TagsGen.md`, tool under `FixAntenna/Tools/TagsGen`) — custom tags then get compiler-checked constants instead of magic numbers.

```csharp
const int MyCustomTag = 5001;

// Read
var value = message.GetTagValueAsString(MyCustomTag);

// Write — AddTag appends a new occurrence. To overwrite an existing tag
// on a message, use Set(int, value) (inherited from ExtendedIndexedStorage)
// or UpdateValue(int, value, IndexedStorage.MissingTagHandling.AddIfNotExists).
message.AddTag(MyCustomTag, "some-value");
```

For repeating groups with custom fields inside, use the engine's group navigation API (`GetRepeatingGroup` / `RepeatingGroup.Entry`) — do NOT iterate the message as a flat tag list. See root `SKILL.md` "Repeating groups" section for the verified pattern.

## Common LLM mistakes

1. **Inventing tag numbers in the 1–4999 range.** Will collide with FIX standard, will silently misvalidate, will get rejected by counterparties.
2. **Not adding the tag to the dictionary, then wondering why validation (with `validation = true`) rejects it — or why group navigation can't see it.** Extend the dictionary; the engine is behaving correctly. (Conversely: expecting rejection of unknown tags in a default config — validation is off by default, see above.)
3. **Forgetting required-field declarations.** A custom message with all-optional fields means the engine won't catch missing required data — bug surfaces at the venue.
4. **Mixing dictionaries between FIX versions.** Each version has its own dictionary. Custom extensions to `FIX44` don't apply to `FIX50`.
5. **Hot-reloading dictionaries.** Not supported. Restart sessions.

## Reference

- Dictionary XML files ship with the product. The FIX Antenna installation includes `fixdic*.xml` for each supported version.
- Property reference: see `fixengine.properties` documentation in the source repo.

## See also

- Root `SKILL.md` "Hard rules — use the dictionary".
- `03-order-entry-client.md` — using `Tags.*` constants vs custom tag numbers.
