# IIS-FTP Simple Authentication Provider – Design Document

## 1 Design Goals
* Keep IIS integration 100 % native (no custom IIS modules beyond the FTP provider surface).
* Zero dependency on Windows/Active Directory accounts.
* Security first – salted & hashed passwords, encrypted at rest.
* Hot-reload of the user store with zero downtime.
* Simple DevOps story (CLI + CI build).

## 2 High-Level Architecture
`IIS FTP` loads two custom classes that implement the extensibility interfaces:
* `IFtpAuthenticationProvider` → validates credentials
* `IFtpAuthorizationProvider`  (optional) → per-path read/write permissions

Behind those interfaces sit three logical components:

```
┌──────────────┐         ┌───────────────┐          ┌───────────────┐
│ Configuration│ ←json┐  │  User Store   │ ←JSON/DB │   CryptoSvc    │
└──────┬───────┘       │  └───────────────┘          └──────┬────────┘
       │               │          ▲                          │
       ▼               │          │                          ▼
  Provider classes ────┴──────────┴────────────────────────>Hash / Salt
```

The providers remain dependency-injection friendly (constructor injection) even though IIS instantiates them via reflection.

## 3 Data Model
```
User
 ├─ UserId        (string, key)
 ├─ DisplayName   (string)
 ├─ SaltedHash    (string – PBKDF2)
 ├─ HomeDirectory (string)
 └─ Permissions[] (Path, [Read, Write])
```
Optional: introduce `Group` entity + group membership to avoid duplicate permission entries.

## 4 User Store Abstraction
```csharp
public interface IUserStore
{
    Task<User?> FindAsync(string userId);
    Task<bool> ValidateAsync(string userId, string password); // constant-time compare
    Task<IEnumerable<Permission>> GetPermissionsAsync(string userId);
}
```
Back-ends (pluggable):
* JSON file (MVP)
* SQLite
* SQL Server

Hot-reload pattern for JSON:
* `FileSystemWatcher` → debounce 1-2 s → reload into immutable in-memory cache → `Interlocked.Exchange`.

## 5 Crypto & Security
* Use `Rfc2898DeriveBytes` (PBKDF2 100 000 iterations) or stronger (bcrypt/Argon2 via libsodium).
* Never store plain salts; keep them next to hashes.
* Encrypt the JSON/SQLite file with DPAPI-NG or AES-GCM; key path supplied via environment variable.
* Constant-time compare for password validation.
* Log failed auth to Windows Event Log (do **not** echo passwords).

## 6 Configuration File Example
```json
{
  "UserStore": {
    "Type": "Json",
    "Path": "C:\\inetpub\\ftpusers\\users.enc",
    "EncryptionKeyEnv": "FTP_USERS_KEY"
  },
  "Hashing": {
    "Algorithm": "PBKDF2",
    "Iterations": 100000
  }
}
```

## 7 Repository & Build Layout
```
/ src
    /AuthProvider      ← IIS-facing classes
    /Core              ← Domain, crypto, user store
    /ManagementCli     ← dotnet-tool for add-user, change-pwd (future)
/tests
/docs
    design.md          ← this file
/readme.md
.github/workflows/ci.yml
```

## 8 Testing Strategy
* **Unit** – hashing functions, user store CRUD, hot-reload.
* **Integration** – spin up IIS Express in-proc with sample site & FTP.
* **Security** – ZAP or similar for brute-force / credential-stuffing simulation.

## 9 Operational Concerns
* Key rotation ⇒ CLI command to re-encrypt user store.
* Audit trail – append-only `auth.log` (JSON lines).
* Metrics – Prometheus textfile exporter (`auth_success_total`, `auth_failure_total`).
* Rate-limit by source IP (IIS FTP has built-in throttling; document how to enable).

## 10 README To-Do List
1. Shields (build, license, NuGet).
2. Quick Start (5 lines to get running).
3. Configuration section (link to example above).
4. Security notes (hashing algorithm, key rotation).
5. CLI usage examples.
6. Contributing guide.
7. License.

---

*Last updated: 2025-05-28* 