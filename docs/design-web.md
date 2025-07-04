# Management Web UI – Design Document

## 1 Goals
* Provide an intuitive, secure web interface to manage FTP users and permissions.
* Run natively under Windows Server IIS without additional middleware.
* Re-use the existing Core library for all business logic (hashing, encryption, user-store access).
* Keep operational footprint small (single site + app pool).
* Allow future extensibility (groups, SSO, alternative back-ends).

## 2 Technology Stack & Rationale
| Concern                | Choice                           | Reason |
|------------------------|----------------------------------|--------|
| Framework              | .NET Framework 4.8               | Ships with Windows Server; no extra runtime install. |
| Web Model              | ASP.NET MVC 5 with Razor Views   | Razor syntax familiarity; MVC abstractions; compatible with .NET Framework. |
| UI Toolkit             | Bootstrap 5                      | Responsive layout, accessible components. |
| Authentication         | Forms Authentication (cookies)   | Simple, proven with IIS; can be swapped for Windows/AD later. |
| Dependency Injection   | `Microsoft.Extensions.DependencyInjection` via `UnityBootstrapper` | Aligns with Core project patterns. |
| Build & CI             | GitHub Actions → MSBuild         | Stays consistent with existing pipeline. |

> **Note** : Razor Pages proper is ASP.NET Core–only. Under classic .NET Framework we'll use MVC 5 with Razor views which offers similar ergonomics.

## 3 High-Level Architecture
```
Browser ──> Controller ──> Application Services ──> Core Library ──> User Store
                    ▲                                       │
                    │                                       ▼
             Razor Views                            Encryption / Hashing
```

* Controllers expose JSON APIs and HTML views.
* Application services orchestrate Core calls (e.g., add-user → hash password → save to store).
* `SimpleFtpAuthenticationProvider` continues to work unchanged; the web app only manipulates the underlying data.

## 4 Key Pages & Features
1. **Login** – Forms auth, anti-brute-force lockout.
2. **Dashboard** – User count, recent audit entries, quick actions.
3. **User List** – Filter/search, paginate, sort.
4. **User Detail / Edit**
   * Change password (client-side strength meter).
   * Set home directory.
   * Manage path permissions (read/write checkboxes).
5. **Add User Wizard** – Generates salt + hash, previews permissions.
6. **Key Rotation** – Trigger re-encryption of JSON/SQLite store.
7. **Audit Log Viewer** – Timeline with filters (user, operation, IP).
8. **Health & Metrics** – `/healthz` JSON, `/metrics` Prometheus text.

## 5 Security Considerations
* **TLS Required** – Enforce HTTPS via `web.config` rewrite.
* **Strong Password Rules** – Configurable length, complexity, Pwned-Passwords API.
* **Anti-Forgery Tokens** – MVC `@Html.AntiForgeryToken()` on all POSTs.
* **Content Security Policy** – Header middleware denies inline script.
* **Role-Based Authorization** – Only administrators can access the UI; roles stored alongside users or via Windows groups.
* **Audit Trails** – Web operations append to the same `auth.log` as FTP provider.

## 6 Integration Points
* `IUserStore` – Web app references Core and injects the configured store (JSON/SQLite/SQL Server).
* `PasswordHasher` & `FileEncryption` – Used directly for user creation and key rotation.
* `MetricsCollector` – Push UI metrics into the existing Prometheus exporter.

## 7 Deployment & Hosting
1. Build → `ManagementWeb\bin\Release`.
2. Copy to IIS site directory (or use Web Deploy package).
3. App Pool: `.NET CLR v4.0`, *Integrated* pipeline, **Enable 32-bit** = false.
4. Set environment variables (`FTP_USERS_KEY`, connection strings) via IIS configuration editor.
5. Apply URL Rewrite rule to force HTTPS.

## 8 Configuration Sample (`web.config` excerpt)
```xml
<appSettings>
  <add key="UserStore:Type" value="Json" />
  <add key="UserStore:Path" value="C:\inetpub\ftpusers\users.enc" />
  <add key="Hashing:Iterations" value="100000" />
  <add key="AllowedAdmins" value="admin1,admin2" />
</appSettings>
```

## 9 Continuous Integration / Delivery
* **Build** – GitHub Actions `windows-latest` → `msbuild ManagementWeb.csproj /p:Configuration=Release`.
* **Unit Tests** – xUnit for controllers & services.
* **Artifact** – Zip output + `web.config` transform.
* **Deploy (optional)** – `msdeploy` to target IIS server.

## 10 Testing Strategy
* **Unit** – Controllers (model validation), application services.
* **Integration** – In-memory hosting via `Microsoft.Owin.TestHost`.
* **E2E** – Playwright tests (headless Chromium) covering CRUD flows.
* **Security** – OWASP ZAP baseline scan in CI.

## 11 Roadmap / Phases
| Phase | Scope |
|-------|-------|
| MVP  | Login, User List, Add User, Change Password |
| v1   | Permission editor, Audit viewer, Key rotation UI |
| v2   | Group management, SSO (AAD), API endpoints |

## 12 Open Questions
* Should we include self-registration for users (disabled by default)?
* Do we need multi-factor authentication for the admin portal?
* Is .NET Framework 4.8 sufficient or should we evaluate ASP.NET Core LTS with Hosting Bundle?

---

*Draft created: 2025-07-04* 