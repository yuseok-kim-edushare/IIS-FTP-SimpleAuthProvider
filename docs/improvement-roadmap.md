### IIS FTP Simple Authentication Provider – Improvement Roadmap (Aug 2025)

This document lists prioritized, actionable improvements based on a quick architecture and code review. Each item links to concrete code locations where applicable.

---

### P1 — Security & Correctness

- **Unify hashing configuration across layers**
  - Core defaults to BCrypt, while Web DI defaults to PBKDF2. Align to one default and make it truly configurable.
  - Evidence:
    - Core default: `PasswordHasher.DefaultAlgorithm = "BCrypt"`
      ```startLine:11:endLine:16:src/Core/Security/PasswordHasher.cs
      private const int DefaultBCryptWorkFactor = 12;
      public const string DefaultAlgorithm = "BCrypt";
      ```
    - Web default in DI: `PBKDF2`
      ```startLine:55:endLine:59:src/ManagementWeb/App_Start/UnityConfig.cs
      Hashing = new HashingConfig
      {
          Algorithm = ConfigurationManager.AppSettings["Hashing:Algorithm"] ?? "PBKDF2",
          Iterations = int.Parse(ConfigurationManager.AppSettings["Hashing:Iterations"] ?? "100000")
      }
      ```
  - Suggested: introduce a `ConfigurablePasswordHasher` using `HashingConfig` (algorithm, iterations, bcrypt cost). Register it in DI and use it in `UserManagerService`.

- **Generate and persist salt only when the algorithm needs it**
  - Current behavior stores a salt even for BCrypt (which embeds salt in the hash); this is confusing and can cause misuse.
  - Evidence:
    ```startLine:31:endLine:41:src/Core/Tools/UserManagerService.cs
    var hashedPassword = _passwordHasher.HashPassword(password);
    var salt = _passwordHasher.GenerateSalt();
    ```
  - Suggested: only call `GenerateSalt()` for PBKDF2; store empty for BCrypt.

- **Encrypted JSON store (at-rest encryption) support in runtime store**
  - `JsonUserStore` reads/writes plain JSON; encryption is only in CLI utilities. In production, the provider should support encrypted stores when `EncryptionKeyEnv` is set.
  - Evidence:
    ```startLine:319:endLine:323:src/Core/Stores/JsonUserStore.cs
    File.WriteAllText(_filePath, json, Encoding.UTF8);
    ```
  - Suggested: add `EncryptedJsonUserStore` (or extend `JsonUserStore`) to decrypt on read and encrypt on save using `FileEncryption` and `UserStoreConfig.EncryptionKeyEnv`.

- **Metrics key naming mismatch (health endpoint)**
  - The metrics collector exports keys prefixed with `ftp_...`, but `ApplicationServices.GetSystemHealthAsync()` looks for `auth_success_total`/`auth_failure_total`.
  - Evidence:
    ```startLine:51:endLine:66:src/Core/Monitoring/MetricsCollector.cs
    IncrementCounter("ftp_auth_success_total");
    IncrementCounter("ftp_auth_failure_total");
    ```
    ```startLine:185:endLine:189:src/ManagementWeb/Services/ApplicationServices.cs
    AuthSuccessCount = metrics.ContainsKey("auth_success_total") ? metrics["auth_success_total"] : 0,
    AuthFailureCount = metrics.ContainsKey("auth_failure_total") ? metrics["auth_failure_total"] : 0,
    ```
  - Suggested: unify on one naming scheme or expose typed getters in `IMetricsCollector` for health.

- **Argon2 is listed as a valid algorithm but not implemented**
  - Evidence:
    ```startLine:47:endLine:51:src/Core/Configuration/AuthProviderConfig.cs
    public string Algorithm { get; set; } = "BCrypt"; // comment lists Argon2
    ```
    ```startLine:136:endLine:150:src/Core/Security/PasswordHasher.cs
    // Detects BCrypt or PBKDF2; Argon2 not supported
    ```
  - Suggested: add Argon2 via `Konscious.Security.Cryptography.Argon2` with sane defaults, and encode parameters with the hash. Library reference: [Konscious.Security.Cryptography](https://github.com/kmaragon/Konscious.Security.Cryptography).

---

### P1 — Dependency Injection & Composition

- **Fix decorator registration for `IUserStore` in Web DI**
  - Current Unity registration registers the decorator as the default `IUserStore` and resolves an `IUserStore` again for its constructor → potential circular resolution.
  - Evidence:
    ```startLine:72:endLine:97:src/ManagementWeb/App_Start/UnityConfig.cs
    container.RegisterType<IUserStore, InstrumentedUserStore>(
        new InjectionConstructor(
            new ResolvedParameter<IUserStore>(),
            new ResolvedParameter<IMetricsCollector>(),
            new ResolvedParameter<IAuditLogger>()
        )
    );
    ```
  - Suggested: register the concrete store with a named mapping (e.g., `"InnerUserStore"`), then register the default `IUserStore` as `InstrumentedUserStore` constructed with the named inner. Repeat per store type.

- **Make hashing truly configurable in DI**
  - `PasswordHasherService` hardcodes BCrypt regardless of `HashingConfig`.
  - Evidence:
    ```startLine:27:endLine:43:src/Core/Security/IPasswordHasher.cs
    public class PasswordHasherService : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            return PasswordHasher.HashPasswordBCrypt(password);
        }
    }
    ```
  - Suggested: `ConfigurablePasswordHasher` consumes `HashingConfig` via DI.

---

### P2 — Robustness & Performance

- **Atomic writes for JSON store**
  - Replace `File.WriteAllText` with temp-file + `File.Replace` to avoid partial writes and watcher races.

- **Debounce improvements for hot-reload**
  - Current debounce spawns untracked tasks per event; use a single timer or `CancellationTokenSource` to coalesce events.

- **Path normalization improvements**
  - In `SimpleFtpAuthorizationProvider.NormalizePath`, consider converting `\` → `/` and normalizing repeated slashes.
  - Evidence:
    ```startLine:69:endLine:82:src/AuthProvider/SimpleFtpAuthorizationProvider.cs
    if (!path.StartsWith("/")) path = "/" + path;
    if (!path.EndsWith("/")) path += "/";
    ```

---

### P2 — Testing & CI/CD

- **Unify test frameworks**
  - Core/AuthProvider use NUnit; Web tests use MSTest. Standardize on one (prefer NUnit per repo).

- **Add CI**
  - Add a Windows runner workflow that builds, runs tests, and (optionally) packs artifacts.

- **Integration tests for encrypted JSON store**
  - Cover read/write with `FTP_USERS_KEY` set; assert hot-reload with encryption.

---

### P3 — Observability & Ops

- **Typed health metrics**
  - Extend `IMetricsCollector` with typed getters for success/failure/authz counts to avoid stringly-typed key lookups.

- **Audit log rotation & retention**
  - If file logging enabled, consider simple size-based rotation to prevent unbounded growth.

---

### P3 — Documentation & UX

- **Document the finalized hashing policy**
  - Default, algorithm switching, BCrypt cost, PBKDF2 iterations, and migration notes.

- **Deployment checklist**
  - IIS provider registration, permissions on user store, environment variable setup, metrics exporter path ACLs.

---

### Quick Wins Summary

- Align hashing default and make it configurable (P1).
- Fix Unity decorator registration for `IUserStore` (P1).
- Implement encrypted JSON store support or a new store type (P1).
- Unify metrics key names or add typed getters (P1).
- Atomic file writes and improved debounce in `JsonUserStore` (P2).
- Normalize backslashes in authz path matching (P2).
- Unify test frameworks and add CI (P2).


