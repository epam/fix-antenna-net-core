## FIX Antenna .NET Core features

### FIX Engine
- Supports FIX 4.0 - FIX 4.4, FIX 5.0, FIX 5.0 SP1, FIX 5.0 SP2
- Supports all FIX message types (pre-trade, trade, post-trade, market data, etc.)
- Customizable FIX protocol with user-defined tags and message types
- Sequence numbers management
- Microseconds in FIX tags (MIFID II)
- Smart resend request handling
- Validation against FIX dictionaries
- Unregistered acceptors handling
- Standard FIX routing based on DeliverTo and OnBehalfOf fields
- Store messages while disconnected
- Switch to the backup connection
- Round-robin reconnect option to multiple backup destinations for an initiator session
- Administrative Plugin for Fix Antenna

### Sessions Processing
- Session Qualifiers
- Multiple FIX sessions
- Initiator/Acceptor Sessions
- Auto-reconnect Session
- Transient Sessions
- Restore state after failure (Persistent Session)
- Pluggable session level

### Storage
- Supported Storage Types:
  - Null storage
  - Persistent storage
  - Persistent Memory Mapped based storage
- Storage management (get a message by seq. num, get creation time)

### Performance Tuning
- Ability to enable or disable Nagle's algorithm to minimize latency or maximize throughput
- Ability to manipulate the internal outgoing queue size to get maximum throughput (process messages in batch) or lower latency (minimal time in queue)
- Ability to use different levels of message validation to balance between reasonable correctness and good performance:
  - Well formedness validation
  - Validation of allowed message fields
  - Validation of required message fields
  - Validation of message fields order
  - Validation of duplicated message fields
  - Validation of field values according to defined data types
  - Validation of repeating group fields
  - Conditionally required fields
- Ability to choose the sending mode. Synchronous sending gives lower latency, but asynchronous is preferable for getting a better throughput
- Ability to configure CPU affinity to enable the binding and unbinding a thread to a CPU, so that the thread will execute only on the designated CPU rather than any CPU
- Setting send and receive socket buffer sizes for TCP transport options

### Message composition API
- Provides the following functionality:
  - Creating a message from a raw FIX string (parse)
  - Serializing a FIX object message to raw FIX string
  - Adding, removing, modifying FIX fields and repeating groups
  - The ability of internal pooling to reduce memory allocations
  - FIX flat message model - generic model to work with the abstract `FixMessage` class via fields and groups getters and setters, which gives the highest performance
- RG API - API for working with repeating groups. Similar to FIX flat message model but allow to work with structures
- Prepared messages - message template for the faster sending the messages with the same structure but with different values
- Efficient getters and setters for working with values as with primitive type to reduce garbage production

### Security
- Logon Customization
- Standard FIX authorization utilizing username and password fields in FIX Logon message
- Strategies for accepting incoming sessions:
  - Auto-accept incoming sessions (to simplify development and testing)
  - Auto-decline non-pre-configured sessions
- IP range based whitelisting for incoming connections
- CME Secure Logon support