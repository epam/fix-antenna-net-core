# 09 TLS / Secure FIX Session

> Targets **Epam.FixAntenna.NetCore 1.2.3**. See `../SKILL.md` for root API rules.

## Pattern

A FIX session that uses TLS for transport security, optionally with mutual authentication (client certificates). Handles cert rotation and reconnect after TLS errors.

```
[client] ──TLS handshake──► [server]
   │           │
   │           ├─► server cert validated by client
   │           └─► client cert validated by server (mTLS)
   │
   └─► FIX traffic over encrypted channel
```

## When to use

- Any internet-routed FIX connection.
- Regulated environments where in-transit encryption is mandated.
- Cross-cloud / cross-DC connections.

## When NOT to use

- Loopback / same-host (use the unix socket pattern if available, or plain TCP).
- Inside a private VPN where the network is already encrypted (still defensible to do mTLS for defense-in-depth).

## Configuration (in `fixengine.properties`)

Property keys below are **verified against the 1.2.3 `Config` class** (string constants exposed by `Config.Ssl*`):

```properties
sessionIDs = tlsInit

sessions.tlsInit.sessionType    = initiator
sessions.tlsInit.senderCompID   = BUYSIDE
sessions.tlsInit.targetCompID   = VENUE
sessions.tlsInit.host           = fix.broker.com
sessions.tlsInit.port           = 9876

# Per-session TLS (all keys verified against Config.Ssl* string constants)
sessions.tlsInit.requireSSL                    = true   # Config.RequireSsl
sessions.tlsInit.sslProtocol                   = Tls12  # see "sslProtocol values" below
sessions.tlsInit.sslCertificate                = ./certs/client.pfx
sessions.tlsInit.sslCertificatePassword        = ...
sessions.tlsInit.sslServerName                 = fix.broker.com   # SNI / host check
sessions.tlsInit.sslCheckCertificateRevocation = true
sessions.tlsInit.sslValidatePeerCertificate    = true             # verify peer

# Trust store (for validating peer cert chain)
sessions.tlsInit.sslCaCertificate = ./certs/broker-ca.pem
```

> ⚠️ There is no `sslMode` property. Use `requireSSL` (verified `Config.RequireSsl`).
> ⚠️ Per-session keys use the `sessions.<id>.<key>` prefix — not `<id>.<key>` (that malformed form is ignored). A key with **no prefix at all** is NOT ignored: it is a valid top-level key that acts as a process-wide default inherited by every session (per-session values override it). The acceptor TLS keys below rely on exactly that.

### `sslProtocol` values

Per `Docs/TlsSupport.md`, the **default is `None` — let .NET pick the best mutually supported protocol**. Shipped samples use `Tls12`. The parser accepts the .NET `SslProtocols` enum string names (`None`, `Tls`, `Tls11`, `Tls12`, `Tls13`). `Tls13` is the natural spelling but **is not used in any shipped sample**, so its end-to-end behavior is not observed.

For an explicit TLS-1.3-only requirement: set `sslProtocol = Tls13` AND verify by capturing the negotiated protocol after handshake (don't trust silent downgrade rejection).

> ⚠️ Parsing is **case-sensitive** — the engine calls `Enum.TryParse<SslProtocols>` without `ignoreCase` (verified `ConfigurationAdapter.SslProtocol`). Use the exact .NET enum casing: `Tls12`, not `tls12` / `TLS12`. An unrecognized value throws `ArgumentException` (`"Property sslProtocol have wrong value:..."`) at connect time.

### Acceptor-side TLS: decided per PORT, configured at TOP level

On the acceptor, TLS is **not** a per-session decision. An incoming connection is wrapped in `SslStream` only if the local port it arrived on is listed in `sslPort` (verified: `TcpAcceptorTransport` checks `IsSslPort(LocalEndPoint.Port)` and otherwise hands back the plain `NetworkStream`). And because the TLS handshake completes **before** the FIX Logon arrives, the acceptor's certificate/validation settings are read from the **global (top-level) configuration** the `FixServer` was created with — the engine cannot know which session is connecting until after the handshake, so per-session cert keys are never consulted for it. This matches the official 1.2.3 `Docs/TlsSupport.md` examples, which put all cert/trust keys at top level and only `requireSSL` per session.

```properties
# Acceptor with mTLS — cert/trust keys at TOP level (no sessions. prefix)
port    = 5000              # optional plaintext listener(s)
sslPort = 5443              # TLS listener(s); comma-separated list allowed, e.g. 5443,5444

sslProtocol                = Tls12
sslCertificate             = ./certs/server.pfx
sslCertificatePassword     = ${SERVER_PFX_PASSWORD}
sslValidatePeerCertificate = true                    # require + validate client certs (mTLS)
sslCaCertificate           = ./certs/clients-ca.pem  # which CAs are trusted

# Per-session: reject the session at Logon if its connection arrived on a non-TLS port
sessions.default.requireSSL = true
```

For TLS-only operation, omit `port` and set only `sslPort`. If the same port number appears in both `port` and `sslPort`, **TLS wins**: the engine logs `Server on port N has been configured already. Configuration will be overriden.` and starts that port as secure (verified `Docs/TlsSupport.md`).

> The `${SERVER_PFX_PASSWORD}` syntax is real engine behavior, not shell expansion: the properties loader substitutes `${NAME}` from environment variables, falling back to other properties defined in the file (verified `TemplatePropertiesWrapper`).

> ⚠️ **The broken pattern to avoid:** putting the TLS keys under `sessions.default.*` and pointing counterparties at the plain `port` listener. A port not listed in `sslPort` accepts plaintext only — **no TLS handshake is performed at all** — and session-scoped cert keys are ignored by the acceptor's handshake. The result is an unencrypted listener that merely *looks* configured for TLS.

#### Key scoping: initiator vs acceptor

| Key | Initiator semantics | Acceptor semantics |
|---|---|---|
| `sslCertificate` (+ `sslCertificatePassword`) | Client cert presented to server (mTLS) — per-session OK | **Server cert** presented to every incoming client — **top level only** |
| `sslValidatePeerCertificate` | Validate the venue's server cert — per-session OK | **Require + validate** client cert (mTLS server side) — **top level only** |
| `sslCaCertificate` | Trust anchor for the venue's cert — per-session OK | Trust anchor for client certs — **top level only** |
| `sslProtocol` | Pin TLS version — per-session OK | Pin accepted TLS version — **top level only** |
| `sslServerName` | SNI / server-name check — per-session OK | n/a — initiator-side key |
| `sslPort` | Ignored | Which listening port(s) speak TLS — top level |
| `requireSSL` | Negotiate TLS on connect — per-session OK | **Per-session Logon-time check**, applied *after* the port already decided the handshake question: a session with `requireSSL=true` whose Logon arrived over a non-TLS connection is rejected (`Session ... configured as secure, but connected on unsecured connection...` logged at Error, transport closed — verified `FixConnectionHandler`). It does **not** make a plaintext port refuse connections. |

Why the asymmetry: the **initiator** builds its transport from the session's own configuration (verified `TcpTransport(host, port, parameters)` reads `parameters.Configuration`), so every `Ssl*` key can be scoped `sessions.<id>.*` there. The **acceptor** builds one connection authenticator from the configuration its `FixServer` was constructed with (the global config by default — verified `FixServer` / `TcpServer`), before any session is identified. If different counterparties need different server certs or trust anchors on the acceptor side, run separate `FixServer(Config)` instances (or separate processes) — or terminate TLS in a sidecar.

> ⚠️ **Per-client cert fingerprint pinning is NOT a documented 1.2.3 feature.** Trust is at the CA level — if a CA that signed `client-A` is trusted, every other cert that CA signed is also trusted (which is the same trust model as any TLS server). For *per-counterparty* cert pinning (e.g. "session SUB01 may only present cert with thumbprint X"), the supported path is to **terminate TLS in a sidecar** (stunnel / Envoy / HAProxy) that does fingerprint pinning, and run the engine in plaintext behind it. Do not invent a `sessions.<id>.expectedClientCertFingerprint` property — it doesn't exist.

(For the `Tls13` token-spelling caveat see the "`sslProtocol` values" subsection above.)

## Certificate hygiene

1. **Cert files outside the source repo.** Mount via secret manager, env-driven path, or k8s secret. Never commit `.pfx` / `.pem` to source control.
2. **Passwords from secret manager.** `sslCertificatePassword` in plaintext properties is a stopgap; production should use indirection (env var, vault).
3. **Rotation cadence.** Match the issuing CA's policy. 90-day or 1-year is typical. Test rotation in staging. Note that `sslCertificate` / `sslCaCertificate` accept not only file paths but also a Windows certificate-store distinguished name (`CN=...`) or a subject-name fragment (verified `Config.SslCertificate` / `Docs/TlsSupport.md`; Local Machine store searched first, then Current User) — store-based certs can be rotated by OS tooling without touching `fixengine.properties`.
4. **Pin trust narrowly.** Trust ONLY the CA(s) that should issue the peer's cert. Don't trust system root store unless required.
5. **Hostname verification on.** Always validate that the cert matches the expected peer hostname / SAN.
6. **Revocation checking.** OCSP or CRL. Be aware that revocation checks can fail open silently — monitor.

## Reconnect behavior on TLS errors

| Error | Reconnect? |
|---|---|
| Cert expired | NO. Replace cert, then restart. Reconnecting fails the same way. |
| Peer cert untrusted | NO. Verify trust chain. |
| Handshake timeout / transient | YES, with engine reconnect logic. |
| Protocol downgrade / version mismatch | NO. Fix config. |

Configure `autoreconnectAttempts` and `autoreconnectDelayInMs` in `fixengine.properties` (verified `Config.AutoreconnectAttempts` / `Config.AutoreconnectDelayInMs`). `autoreconnectAttempts = -1` disables reconnect; `0` = infinite; positive `N` = N attempts. ⚠️ **The default is `-1` — reconnect is OFF out of the box** (verified `[DefaultValue(NoAutoreconnect)]`, `NoAutoreconnect = "-1"`), so the "YES, reconnect" rows above only apply once `autoreconnectAttempts` is set explicitly. ⚠️ There is **no** `enableAutoreconnect` property — the engine silently ignores it if set. Reconnect is controlled by the value of `autoreconnectAttempts` alone. Have alerts on "TLS error reconnect loop" — a stuck reconnect loop on cert errors is a noisy failure mode.

## Common LLM mistakes

1. **Disabling cert validation "to make it work."** Sets `ServerCertificateValidationCallback => true` or equivalent. This is a critical security hole. Fix the trust chain instead.
2. **Sharing one cert across many sessions to different counterparties.** Each peer expects the cert to identify this side specifically. If counterparty A and B expect different identities, use different certs.
3. **Logging the cert password.** Sanitize logs.
4. **Using `Ssl3` / `Tls10` / `Tls11`.** Deprecated; will fail handshake with modern peers. Use `Tls12` or `Tls13`.
5. **Hardcoding cert paths.** Move to config so rotation doesn't require code change.
6. **Forgetting mutual TLS on acceptor side.** A server that doesn't verify client certs accepts any client that knows the host/port. For private venues, require client certs.
7. **Treating TLS errors as ordinary disconnects.** Log them at a higher level; they require operator action.

## Operational checklist

- [ ] Cert and key file paths configured, files exist, permissions restricted.
- [ ] Trust store contains only required CAs.
- [ ] Hostname verification enabled.
- [ ] Reconnect policy bounded (not infinite on cert errors).
- [ ] Monitoring: cert expiry < 30 days alerts; TLS handshake failures alert.
- [ ] Cert rotation tested in staging.

## See also

- Root `SKILL.md` "Configuration: fixengine.properties" — general structure.
- `02-minimal-initiator.md` — base pattern; this reference adds TLS on top.
