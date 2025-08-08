# IIS FTP Simple Authentication Provider - Codebase Summary

## 🎯 Project Overview

This is a **native IIS FTP authentication and authorization provider** that provides secure, lightweight user management without requiring Windows/Active Directory accounts. The codebase is built with .NET Framework 4.8 and follows clean architecture principles with clear separation of concerns.

## 🏗️ Architecture Overview

The codebase follows a **layered architecture** with these main components:

```
┌─────────────────────────────────────────────────────────────┐
│                    IIS FTP Server                           │
├─────────────────────────────────────────────────────────────┤
│  AuthProvider (IIS Integration Layer)                      │
│  ├─ SimpleFtpAuthenticationProvider                        │
│  └─ SimpleFtpAuthorizationProvider                         │
├─────────────────────────────────────────────────────────────┤
│  Core (Business Logic Layer)                               │
│  ├─ Domain Models (User, Permission)                       │
│  ├─ Security (PasswordHasher, FileEncryption)              │
│  ├─ Stores (IUserStore implementations)                    │
│  ├─ Configuration (AuthProviderConfig)                     │
│  └─ Tools (UserManager, UserManagerService)                │
├─────────────────────────────────────────────────────────────┤
│  Management Interfaces                                      │
│  ├─ ManagementWeb (ASP.NET MVC Web UI)                     │
│  └─ ManagementCli (Command-line tool)                      │
└─────────────────────────────────────────────────────────────┘
```

## 📁 Project Structure

### `/src/AuthProvider/` - IIS Integration Layer
**Purpose**: Native IIS FTP provider implementations that hook into IIS extensibility model.

**Key Files**:
- `SimpleFtpAuthenticationProvider.cs` - Implements `IFtpAuthenticationProvider`
- `SimpleFtpAuthorizationProvider.cs` - Implements `IFtpAuthorizationProvider`  
- `UserStoreFactory.cs` - Factory for creating user stores and dependencies

**Code Style**:
- **Constructor Pattern**: Parameterless public constructor for IIS + internal constructor for testing
- **Synchronous Wrapper**: Uses `.GetAwaiter().GetResult()` to bridge async Core methods to sync IIS interface
- **Dependency Injection**: Constructor injection with null checks and factory pattern

### `/src/Core/` - Business Logic Layer
**Purpose**: Contains all domain logic, security, data access, and configuration.

#### Domain Models (`/Core/Domain/`)
- `User.cs` - FTP user entity with properties for authentication and permissions
- `Permission.cs` - Path-based access control with read/write flags

**Code Style**:
- **Simple POCOs**: Plain objects with auto-properties
- **Nullable Reference Types**: Enabled throughout for type safety
- **Value Semantics**: `Permission` implements `IEquatable<T>` for comparison

#### Security (`/Core/Security/`)
- `PasswordHasher.cs` - Multi-algorithm password hashing (BCrypt default, PBKDF2; Argon2 planned via Konscious)
- `FileEncryption.cs` - File encryption using DPAPI or AES-GCM
- `IPasswordHasher.cs` - Interface for password hashing operations

**Code Style**:
- **Static Utility Classes**: Pure functions for cryptographic operations
- **Algorithm Detection**: Auto-detects hash format for backward compatibility
- **Constant-Time Comparison**: Prevents timing attacks
- **P/Invoke Integration**: Uses BCrypt for AES-GCM on .NET Framework

#### User Stores (`/Core/Stores/`)
- `IUserStore.cs` - Interface defining user store operations
- `JsonUserStore.cs` - JSON file-based implementation with hot-reload
- `SqliteUserStore.cs` - SQLite database implementation
- `SqlServerUserStore.cs` - SQL Server implementation
- `EsentUserStore.cs` - Windows ESENT database implementation
- `InstrumentedUserStore.cs` - Decorator for metrics collection

**Code Style**:
- **Async-First**: All operations use `Task<T>` for scalability
- **Hot-Reload Pattern**: `FileSystemWatcher` + `Interlocked.Exchange` for atomic cache updates
- **Error Resilience**: Comprehensive exception handling with fallback to existing cache
- **Thread Safety**: Immutable collections and atomic operations

#### Configuration (`/Core/Configuration/`)
- `AuthProviderConfig.cs` - Strongly-typed configuration model

**Code Style**:
- **Hierarchical Structure**: Nested configuration objects
- **Default Values**: Sensible defaults for all settings
- **JSON Serialization**: Uses `System.Text.Json` with proper naming

#### Tools (`/Core/Tools/`)
- `UserManager.cs` - Static utility class for user management operations
- `UserManagerService.cs` - Service wrapper around user management

**Code Style**:
- **Static Utilities**: Pure functions for CLI operations
- **Console Output**: Direct console writes for CLI tooling
- **File Operations**: JSON serialization/deserialization

### `/src/ManagementWeb/` - Web Management Interface
**Purpose**: ASP.NET MVC 5 web application for user management.

**Key Components**:
- **Controllers**: `UsersController`, `DashboardController`, `AccountController`
- **Services**: `ApplicationServices` - Business logic orchestration
- **Models**: View models for web forms and data binding
- **Views**: Razor views with Bootstrap 5 UI

**Code Style**:
- **MVC Pattern**: Clear separation of concerns
- **Async Controllers**: All actions use `async/await`
- **Dependency Injection**: Unity container for service resolution
- **Anti-Forgery Tokens**: CSRF protection on all POST actions
- **Validation**: Both client and server-side validation

### `/src/ManagementCli/` - Command-Line Interface
**Purpose**: Command-line tool for user and encryption management.

**Key Components**:
- **Commands**: `UserCommands`, `EncryptionCommands`
- **Options**: Command-line argument parsing with CommandLineParser
- **Program.cs**: Main entry point with command routing

**Code Style**:
- **Command Pattern**: Each operation is a separate command class
- **Option Classes**: Strongly-typed command-line arguments
- **Error Handling**: Consistent error reporting and exit codes

## 🔐 Security Architecture

### Password Hashing
- **Multi-Algorithm Support**: BCrypt (default) and PBKDF2; Argon2 via `Konscious.Security.Cryptography` planned
- **Auto-Detection**: Automatically detects hash format for migration
- **Constant-Time Comparison**: Prevents timing attacks
- **Configurable Work Factors**: Adjustable iteration counts

### Encryption at Rest
- **DPAPI**: Windows Data Protection API (default)
- **AES-GCM**: 256-bit encryption with environment variable keys
- **Key Rotation**: Support for seamless key rotation
- **P/Invoke Integration**: Uses BCrypt for .NET Framework compatibility

### Audit Logging
- **Windows Event Log**: Native Windows logging
- **File Logging**: Optional append-only log files
- **Structured Logging**: JSON-formatted audit entries
- **Metrics Collection**: Prometheus-compatible metrics

## 🔄 Data Flow Patterns

### Authentication Flow
```
IIS FTP Request → SimpleFtpAuthenticationProvider → IUserStore.ValidateAsync() → PasswordHasher.Verify() → AuditLogger.LogAuthentication()
```

### User Management Flow
```
Web UI/CLI → ApplicationServices → UserManagerService → IUserStore → File/Database
```

### Hot-Reload Flow
```
File Change → FileSystemWatcher → Debounced Reload → Atomic Cache Replacement → Continue Serving
```

## 🎨 Code Style Patterns

### 1. **Constructor Injection Pattern**
```csharp
// Public constructor for IIS (required)
public SimpleFtpAuthenticationProvider() 
    : this(UserStoreFactory.Create(), UserStoreFactory.GetAuditLogger(), UserStoreFactory.GetMetricsCollector())
{
}

// Internal constructor for testing
internal SimpleFtpAuthenticationProvider(IUserStore userStore, AuditLogger auditLogger, MetricsCollector? metricsCollector)
{
    _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
    // ...
}
```

### 2. **Async Bridge Pattern**
```csharp
// Bridge async Core methods to sync IIS interface
var valid = _userStore.ValidateAsync(userName, userPassword).GetAwaiter().GetResult();
```

### 3. **Hot-Reload Pattern**
```csharp
// Atomic cache replacement for thread safety
var newCache = users.ToImmutableDictionary(u => u.UserId, StringComparer.OrdinalIgnoreCase);
Interlocked.Exchange(ref _cache, newCache);
```

### 4. **Factory Pattern**
```csharp
// Centralized dependency creation
public static IUserStore Create()
{
    var config = LoadConfiguration();
    return CreateUserStore(config.UserStore);
}
```

### 5. **Decorator Pattern**
```csharp
// Add cross-cutting concerns without modifying core logic
public class InstrumentedUserStore : IUserStore
{
    private readonly IUserStore _inner;
    private readonly IMetricsCollector _metrics;
    // ...
}
```

## 🧪 Testing Strategy

### Test Projects
- `AuthProvider.Tests/` - Tests for IIS integration layer
- `Core.Tests/` - Tests for business logic and data access
- `ManagementWeb.Tests/` - Tests for web interface

### Testing Patterns
- **Unit Tests**: Isolated component testing
- **Mocking**: Interface-based testing with Moq
- **Integration Tests**: End-to-end workflow testing
- **Security Tests**: Password hashing and encryption validation

## 🚀 Deployment Architecture

### IIS Integration
- **Native Provider**: Direct integration with IIS FTP extensibility
- **Configuration**: JSON-based configuration with environment variable support
- **Hot-Reload**: Zero-downtime configuration updates
- **Multiple Stores**: Support for JSON, SQLite, SQL Server, ESENT

### Management Interfaces
- **Web UI**: ASP.NET MVC application for visual management
- **CLI Tool**: Command-line interface for automation
- **API Endpoints**: Health checks and metrics endpoints

## 🔧 Configuration Management

### Configuration Sources
1. **JSON File**: Primary configuration (`ftpauth.config.json`)
2. **Environment Variables**: Encryption keys and sensitive data
3. **IIS Settings**: Web application configuration
4. **Command Line**: CLI tool arguments

### Configuration Hierarchy
```
Default Values → JSON Config → Environment Variables → Command Line
```

## 📊 Monitoring and Observability

### Metrics Collection
- **Prometheus Format**: Textfile exporter for monitoring
- **Authentication Metrics**: Success/failure rates
- **Performance Metrics**: Response times and throughput
- **Health Checks**: System status endpoints

### Logging Strategy
- **Structured Logging**: JSON-formatted audit entries
- **Multiple Destinations**: Event Log, File Log, Debug Output
- **Log Levels**: Configurable verbosity
- **Audit Trail**: Complete action history

## 🎯 Key Design Principles

1. **Security First**: All security decisions prioritize safety over convenience
2. **Zero Dependencies**: Minimal external dependencies for deployment simplicity
3. **Hot-Reload**: Configuration changes without service interruption
4. **Async-First**: Scalable async operations throughout the stack
5. **Interface-Based**: Dependency injection and testability
6. **Error Resilience**: Graceful degradation and comprehensive error handling
7. **Backward Compatibility**: Support for legacy hash formats and configurations

## 🔄 Development Workflow

### Build Process
```bash
dotnet restore
dotnet build
dotnet test
```

### Deployment Process
1. Build solution in Release mode
2. Copy DLLs to IIS directory
3. Configure IIS FTP site with custom providers
4. Set up configuration files and environment variables
5. Deploy web management interface (optional)

### Testing Process
1. Unit tests for all components
2. Integration tests for user workflows
3. Security tests for authentication and encryption
4. End-to-end tests for complete scenarios

## 📋 Component Responsibilities

### AuthProvider Layer
- **IIS Integration**: Native provider registration and lifecycle
- **Request Routing**: Direct FTP requests to appropriate handlers
- **Synchronous Interface**: Bridge between IIS sync model and async Core

### Core Layer
- **Business Logic**: User validation, permission checking, password management
- **Data Access**: Abstract user store operations
- **Security**: Cryptographic operations and audit logging
- **Configuration**: Application settings and environment management

### Management Layer
- **User Interface**: Web and CLI tools for administration
- **Orchestration**: Coordinate operations across Core components
- **Validation**: Input validation and business rule enforcement
- **Reporting**: System health and audit information

## 🔍 Code Quality Highlights

### Error Handling
- **Comprehensive Try-Catch**: All external operations wrapped
- **Graceful Degradation**: Fallback to existing state on errors
- **Detailed Logging**: Structured error information for debugging
- **User Feedback**: Clear error messages for end users

### Performance
- **Async Operations**: Non-blocking I/O throughout
- **Caching**: In-memory user cache with hot-reload
- **Connection Pooling**: Database connection management
- **Lazy Loading**: Resources loaded on demand

### Maintainability
- **Interface Segregation**: Small, focused interfaces
- **Single Responsibility**: Each class has one clear purpose
- **Dependency Injection**: Loose coupling between components
- **Configuration Externalization**: Settings separate from code

This codebase demonstrates excellent software engineering practices with clear separation of concerns, comprehensive error handling, and a focus on security and maintainability. The architecture supports both simple deployments and complex enterprise scenarios. 