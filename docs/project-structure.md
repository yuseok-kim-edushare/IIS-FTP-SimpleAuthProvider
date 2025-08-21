# Project Structure Documentation

## 📁 Complete Project Structure

```
IIS-FTP-SimpleAuthProvider/
├── 📁 .git/                          # Git repository
├── 📁 .github/                       # GitHub workflows and templates
├── 📁 .gitmodules                    # Git submodule configuration
├── 📁 config/                        # Configuration templates and examples
├── 📁 deploy/                        # Deployment scripts and automation
├── 📁 docs/                          # Project documentation
│   ├── 📄 architecture diagrams.md   # System architecture diagrams
│   ├── 📄 codebase-summary.md        # Comprehensive codebase overview
│   ├── 📄 installation-and-setup-guide.md # Installation instructions
│   ├── 📄 project-structure.md       # This file - project structure
├── 📁 src/                           # Source code
│   ├── 📁 AuthProvider/              # IIS Integration Layer
│   │   ├── 📄 AuthProvider.csproj    # Project file
│   │   ├── 📄 SimpleFtpAuthenticationProvider.cs # Main auth provider
│   │   ├── 📄 SimpleFtpAuthorizationProvider.cs # Authorization provider
│   │   └── 📄 UserStoreFactory.cs    # Dependency factory
│   ├── 📁 Core/                      # Business Logic Layer
│   │   ├── 📄 Core.csproj            # Project file
│   │   ├── 📁 Configuration/         # Configuration management
│   │   ├── 📁 Domain/                # Domain models
│   │   │   ├── 📄 Permission.cs      # Permission entity
│   │   │   └── 📄 User.cs            # User entity
│   │   ├── 📁 Logging/               # Logging infrastructure
│   │   │   ├── 📄 AuditLogger.cs     # Audit logging
│   │   │   └── 📄 IAuditLogger.cs    # Audit logging interface
│   │   ├── 📁 Monitoring/            # Metrics and monitoring
│   │   │   ├── 📄 IMetricsCollector.cs # Metrics collection interface
│   │   │   ├── 📄 MetricsCollector.cs # Metrics collection implementation
│   │   │   └── 📄 NoOpMetricsCollector.cs # No-op metrics collector
│   │   ├── 📁 Security/              # Security services
│   │   │   ├── 📄 FileEncryption.cs  # File encryption
│   │   │   ├── 📄 IPasswordHasher.cs # Password hashing interface
│   │   │   ├── 📄 PasswordHasher.cs  # Password hashing implementation
│   │   │   └── 📄 SecureMemoryHelper.cs # Secure memory utilities
│   │   ├── 📁 Stores/                # User store implementations
│   │   │   ├── 📄 EncryptedJsonUserStore.cs # Encrypted JSON storage
│   │   │   ├── 📄 EsentUserStore.cs  # ESENT database storage
│   │   │   ├── 📄 InstrumentedUserStore.cs # Metrics wrapper
│   │   │   ├── 📄 IUserStore.cs      # User store interface
│   │   │   ├── 📄 JsonUserStore.cs   # JSON file storage
│   │   │   ├── 📄 SqliteUserStore.cs # SQLite storage
│   │   │   ├── 📄 SqlServerUserStore.cs # SQL Server storage
│   │   │   └── 📄 SqlUserStoreBase.cs # SQL store base class
│   │   └── 📁 Tools/                 # Utility tools
│   │       ├── 📄 UserManger.cs     # User management
│   │       └── 📄 UserManagerService.cs    # User manager service
│   ├── 📁 ManagementCli/             # Command-line interface
│   │   ├── 📁 Commands/              # Command implementations
│   │   │   ├── 📄 CommandOptions.cs # Command options
│   │   │   ├── 📄 EncryptionCommands.cs # Encryption commands
│   │   │   └── 📄 UserCommands.cs # User commands
│   │   ├── 📄 ManagementCli.csproj   # Project file
│   │   ├── 📄 Program.cs             # Main entry point
│   └── 📁 ManagementWeb/             # Web management interface
│       ├── 📄 Global.asax            # Application entry point
│       ├── 📄 Global.asax.cs         # Application configuration
│       ├── 📄 ManagementWeb.csproj   # Project file
│       ├── 📄 README.md              # Web UI documentation
│       ├── 📄 Web.config             # Web configuration
│       ├── 📄 Web.BindingRedirects.config # Binding redirects
│       ├── 📁 App_Start/             # Application startup
│       ├── 📁 Content/               # CSS and static content
│       ├── 📁 Controllers/           # MVC controllers
│       │   ├── 📄 AccountController.cs # Authentication controller
│       │   ├── 📄 DashboardController.cs # Dashboard controller
│       │   ├── 📄 HealthController.cs # Health monitoring
│       │   └── 📄 UsersController.cs # User management
│       ├── 📁 Models/                # View models
│       │   ├── 📄 DashboardViewModel.cs # Dashboard data
│       │   ├── 📄 LoginViewModel.cs  # Login form data
│       │   └── 📄 UserViewModel.cs   # User form data
│       ├── 📁 Scripts/               # JavaScript files
│       ├── 📁 Services/              # Business logic services
│       │   ├── 📄 ApplicationServices.cs # Main service layer
│       │   └── 📄 SystemHealth.cs    # Health monitoring service
│       └── 📁 Views/                 # Razor view templates
├── 📁 tests/                         # Test projects
│   ├── 📁 AuthProvider.Tests/        # Auth provider tests
│   ├── 📁 Core.Tests/                # Core logic tests
│   └── 📁 ManagementWeb.Tests/       # Web interface tests
├── 📁 WelsonJS/                      # External toolkit (submodule)
│   ├── 📁 WelsonJS.Toolkit/          # .NET toolkit components
│   │   ├── 📁 EsentInterop/          # ESENT interop layer
│   │   └── 📁 WelsonJS.Esent/        # ESENT wrapper
│   └── 📄 README.md                  # Toolkit documentation
├── 📄 .gitignore                     # Git ignore patterns
├── 📄 CONTRIBUTING                   # Contribution guidelines
├── 📄 IIS-FTP-SimpleAuthProvider.slnx # Solution file
├── 📄 license                        # MIT license
├── 📄 readme.md                      # Main project readme
└── 📄 temp-users.json                # Temporary user data
```

## 🏗️ Architecture Layers

### 1. **IIS Integration Layer** (`/src/AuthProvider/`)
**Purpose**: Native integration with IIS FTP Server extensibility model.

**Key Components**:
- **SimpleFtpAuthenticationProvider**: Implements `IFtpAuthenticationProvider` interface
- **SimpleFtpAuthorizationProvider**: Implements `IFtpAuthorizationProvider` interface
- **UserStoreFactory**: Factory pattern for creating dependencies

**Dependencies**:
- `Microsoft.Web.FtpServer` (IIS FTP extensibility)
- `Core` project (business logic)

### 2. **Core Business Logic Layer** (`/src/Core/`)
**Purpose**: Central business logic, security, and data access.

**Subsystems**:
- **Domain**: User and Permission entities
- **Security**: Password hashing, encryption, secure memory
- **Stores**: Multiple user store implementations
- **Configuration**: Application settings management
- **Logging**: Audit logging and monitoring
- **Tools**: Utility functions and services

**Dependencies**:
- `BCrypt.Net-Next` (password hashing)
- `System.Text.Json` (configuration)
- `WelsonJS` toolkit (ESENT integration)

### 3. **Management Interfaces** (`/src/ManagementWeb/` & `/src/ManagementCli/`)
**Purpose**: User and system management tools.

**Web Interface**:
- ASP.NET MVC 5 application
- Bootstrap 5 UI framework
- Unity dependency injection

**CLI Tool**:
- Command-line user management
- Minimal dependencies
- Cross-platform compatibility

## 🔗 Project Dependencies

### Solution Dependencies
```
AuthProvider → Core
ManagementWeb → Core
ManagementCli → Core
Core → WelsonJS.Toolkit
```

### Package Dependencies

#### Core Project
- `BCrypt.Net-Next` (4.0.3) - Password hashing
- `System.Collections.Immutable` (9.0.8) - Immutable collections
- `System.Text.Json` (9.0.8) - JSON processing
- `System.Security.Cryptography.ProtectedData` (9.0.8) - DPAPI encryption
- `System.Data.SQLite.Core` (1.0.119) - SQLite support
- `Microsoft.Data.SqlClient` (6.1.1) - SQL Server support
- `Konscious.Security.Cryptography.Argon2` (1.3.1) - Argon2 hashing

#### ManagementWeb Project
- `Microsoft.AspNet.Mvc` (5.2.9) - MVC framework
- `Microsoft.AspNet.Razor` (3.2.9) - Razor view engine
- `Microsoft.AspNet.WebPages` (3.2.9) - Web pages
- `Unity` (5.11.10) - Dependency injection
- `Newtonsoft.Json` (13.0.3) - JSON processing
- `Bootstrap` (5.3.7) - UI framework

## 📊 Build Configurations

### Solution Configurations
- **Debug**: Full build with all projects
- **Release**: Production build with all projects
- **Debug.Pack**: Build excluding ManagementWeb (for packaging)
- **Release.Pack**: Production build excluding ManagementWeb

### Target Frameworks
- **All Projects**: .NET Framework 4.8
- **Language Version**: Latest C# features
- **Nullable Reference Types**: Enabled throughout

## 🚀 Deployment Structure

### IIS Integration
```
IIS FTP Site
├── Provider DLLs (AuthProvider)
├── Configuration Files
└── User Data Storage
```

### Web Management Console
```
IIS Web Site
├── ASP.NET MVC Application
├── Static Content (CSS, JS)
└── Configuration Files
```

### CLI Tools
```
System PATH
├── ftpauth.exe (ManagementCli)
└── Configuration Files
```

## 🔧 Configuration Files

### Primary Configuration
- `ftpauth.config.json` - Main application configuration
- `Web.config` - Web application configuration
- `Web.BindingRedirects.config` - Assembly binding redirects

### User Data Storage
- `users.json` - JSON user store (default)
- `users.db` - SQLite user store
- `users.mdb` - ESENT user store
- Encrypted variants with `.enc` extension

### Environment Variables
- `FTP_USERS_KEY` - Encryption key for user data
- `FTP_CONFIG_PATH` - Configuration file path
- `FTP_LOG_LEVEL` - Logging verbosity

## 📈 Monitoring and Observability

### Metrics Collection
- **Prometheus Format**: Textfile exporter
- **Authentication Metrics**: Success/failure rates
- **Performance Metrics**: Response times, throughput
- **Health Checks**: System and dependency status

### Logging Destinations
- **Windows Event Log**: Native Windows logging
- **File Logs**: Structured JSON logging
- **Debug Output**: Development logging
- **Audit Trail**: Complete action history

### Health Monitoring
- **System Health**: Overall system status
- **Dependency Health**: Database and storage status
- **Performance Metrics**: Resource utilization
- **Alerting**: Configurable thresholds

## 🧪 Testing Structure

### Test Projects
- **AuthProvider.Tests**: IIS integration testing
- **Core.Tests**: Business logic testing
- **ManagementWeb.Tests**: Web interface testing

### Testing Patterns
- **Unit Tests**: Isolated component testing
- **Integration Tests**: End-to-end workflow testing
- **Security Tests**: Authentication and encryption validation
- **Mocking**: Interface-based testing with Moq

## 🔄 Development Workflow

### Build Process
```bash
# Restore packages
dotnet restore

# Build solution
msbuild IIS-FTP-SimpleAuthProvider.slnx

# Build with specific configuration
msbuild IIS-FTP-SimpleAuthProvider.slnx /p:Configuration=Release

# Build specific project
msbuild src/AuthProvider/AuthProvider.csproj

# Clean solution
msbuild IIS-FTP-SimpleAuthProvider.slnx /t:Clean

# Run tests
msbuild IIS-FTP-SimpleAuthProvider.slnx /t:Test
```

### Development Tools
- **Visual Studio**: Full IDE support with IntelliSense
- **Dotnet CLI**: Command-line development and testing
- **VS Code**: Lightweight editing with C# extensions
- **MSBuild**: Primary build system for .NET Framework
- **PowerShell**: Scripting and automation
- **AI Friends**: ChatGPT, Gemini, Cursor, and GitHub Copilot

## 🌟 Key Features

### Security
- Multi-algorithm password hashing (BCrypt, PBKDF2, Argon2)
- Encryption at rest (DPAPI, AES-GCM)
- Secure memory management
- Comprehensive audit logging

### Performance
- Hot-reload configuration
- In-memory caching
- Async operations throughout
- Connection pooling

### Flexibility
- Multiple storage backends
- Configurable algorithms
- Environment-based configuration
- Plugin architecture

### Monitoring
- Prometheus metrics
- Health endpoints
- Structured logging
- Performance tracking

This project structure demonstrates a well-organized, maintainable codebase with clear separation of concerns, comprehensive testing, and modern development practices.
