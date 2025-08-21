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
│  ├─ SimpleFtpAuthorizationProvider                         │
│  └─ UserStoreFactory                                       │
├─────────────────────────────────────────────────────────────┤
│  Core (Business Logic Layer)                               │
│  ├─ Domain Models (User, Permission)                       │
│  ├─ Security (PasswordHasher, FileEncryption)              │
│  ├─ Stores (IUserStore implementations)                    │
│  ├─ Configuration (AuthProviderConfig)                     │
│  ├─ Logging & Monitoring (AuditLogger, MetricsCollector)   │
│  └─ Tools (UserManager, UserManagerService)                │
├─────────────────────────────────────────────────────────────┤
│  Management Interfaces                                      │
│  ├─ ManagementWeb (ASP.NET MVC 5 Web UI)                   │
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
- **Error Handling**: Comprehensive try-catch with audit logging and metrics collection

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
- `PasswordHasher.cs` - Multi-algorithm password hashing (BCrypt default, PBKDF2, Argon2 via Konscious)
- `FileEncryption.cs` - File encryption using DPAPI or AES-GCM
- `IPasswordHasher.cs` - Interface for password hashing operations
- `SecureMemoryHelper.cs` - Secure memory management utilities

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
- `EncryptedJsonUserStore.cs` - Encrypted JSON file storage
- `InstrumentedUserStore.cs` - Decorator for metrics collection
- `SqlUserStoreBase.cs` - Base class for SQL-based implementations

**Code Style**:
- **Async-First**: All operations use `Task<T>` for scalability
- **Hot-Reload Pattern**: `FileSystemWatcher` + `Interlocked.Exchange` for atomic cache updates
- **Error Resilience**: Comprehensive exception handling with fallback to existing cache
- **Thread Safety**: Immutable collections and atomic operations
- **Decorator Pattern**: Metrics collection without modifying core store logic

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

#### Logging & Monitoring (`/Core/Logging/` & `/Core/Monitoring/`)
- `AuditLogger.cs` - Comprehensive audit logging to Windows Event Log and files
- `MetricsCollector.cs` - Prometheus-compatible metrics collection
- `ILogger.cs` - Logging interface abstraction

**Code Style**:
- **Structured Logging**: JSON-formatted log entries
- **Multiple Destinations**: Event Log, File Log, Debug Output
- **Performance Metrics**: Authentication success/failure rates, response times

### `/src/ManagementWeb/` - Web Management Interface
**Purpose**: ASP.NET MVC 5 web application for user management.

**Key Components**:
- **Controllers**: `UsersController`, `DashboardController`, `AccountController`, `HealthController`
- **Services**: `ApplicationServices` - Business logic orchestration, `SystemHealth` - Health monitoring
- **Models**: View models for web forms and data binding (`UserViewModel`, `DashboardViewModel`, `LoginViewModel`)
- **Views**: Razor views with Bootstrap 5 UI
- **Configuration**: Web.config with binding redirects and IIS integration

**Code Style**:
- **MVC Pattern**: Clear separation of concerns
- **Async Controllers**: All actions use `async/await`
- **Dependency Injection**: Unity container for service resolution
- **Anti-Forgery Tokens**: CSRF protection on all POST actions
- **Validation**: Both client and server-side validation
- **Health Endpoints**: System status and monitoring endpoints

### `/src/ManagementCli/` - Command-Line Interface
**Purpose**: Command-line tool for user and encryption management.

**Key Components**:
- **Commands**: User management operations
- **Options**: Command-line argument parsing
- **Program.cs**: Main entry point with command routing

**Code Style**:
- **Command Pattern**: Each operation is a separate command class
- **Option Classes**: Strongly-typed command-line arguments
- **Error Handling**: Consistent error reporting and exit codes
- **Minimal Dependencies**: Only depends on Core project

## 🔐 Security Architecture

### Password Hashing
- **Multi-Algorithm Support**: BCrypt (default), PBKDF2, and Argon2 via `Konscious.Security.Cryptography`
- **Auto-Detection**: Automatically detects hash format for migration
- **Constant-Time Comparison**: Prevents timing attacks
- **Configurable Work Factors**: Adjustable iteration counts
- **Salt Generation**: Cryptographically secure random salt generation

### Encryption at Rest
- **DPAPI**: Windows Data Protection API (default)
- **AES-GCM**: 256-bit encryption with environment variable keys
- **Key Rotation**: Support for seamless key rotation
- **P/Invoke Integration**: Uses BCrypt for .NET Framework compatibility
- **Secure Memory**: Secure memory handling for sensitive data

### Access Control
- **Path-Based Permissions**: Granular read/write access control per directory
- **User Isolation**: Separate home directories for each user
- **Permission Inheritance**: Hierarchical permission structure
- **Audit Logging**: Complete action history with Windows Event Log integration

### Audit Logging
- **Windows Event Log**: Native Windows logging integration
- **File Logging**: Optional append-only log files
- **Structured Logging**: JSON-formatted audit entries
- **Metrics Collection**: Prometheus-compatible metrics for monitoring

## 🔄 Data Flow Patterns

### Authentication Flow
```
IIS FTP Request → SimpleFtpAuthenticationProvider → UserStoreFactory → IUserStore.ValidateAsync() → PasswordHasher.Verify() → AuditLogger.LogAuthentication() + MetricsCollector.RecordAuth()
```

### User Management Flow
```
Web UI/CLI → ApplicationServices → UserManagerService → IUserStore → File/Database Storage
```

### Hot-Reload Flow
```
File Change → FileSystemWatcher → Debounced Reload → Atomic Cache Replacement → Continue Serving
```

### Authorization Flow
```
FTP Operation → SimpleFtpAuthorizationProvider → Permission Check → Path Validation → Access Grant/Deny
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

### 6. **Strategy Pattern**
```csharp
// Multiple user store implementations
public interface IUserStore
{
    Task<User?> FindAsync(string userId);
    Task<bool> ValidateAsync(string userId, string password);
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

### Build & Deployment
- **SDK-Style Projects**: Modern .csproj files for easier maintenance
- **Mixed Build Configurations**: Support for both full builds and pack-only builds
- **Package Management**: NuGet package references with proper versioning
- **IIS Integration**: Web application packaging and deployment

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

### Configuration Options
- **User Store Type**: JSON, SQLite, SQL Server, ESENT
- **Encryption Settings**: Algorithm selection and key management
- **Hashing Configuration**: Algorithm and work factor settings
- **Logging Options**: Event log, file logging, and verbosity
- **Performance Tuning**: Cache settings and connection pooling

## 📊 Monitoring and Observability

### Metrics Collection
- **Prometheus Format**: Textfile exporter for monitoring
- **Authentication Metrics**: Success/failure rates, response times
- **Performance Metrics**: Throughput and latency measurements
- **Health Checks**: System status and dependency health

### Logging Strategy
- **Structured Logging**: JSON-formatted audit entries
- **Multiple Destinations**: Event Log, File Log, Debug Output
- **Log Levels**: Configurable verbosity and filtering
- **Audit Trail**: Complete action history with correlation IDs

### Health Monitoring
- **System Health**: Overall system status and component health
- **Dependency Health**: Database and storage backend status
- **Performance Metrics**: Response times and resource utilization
- **Alerting**: Configurable thresholds and notifications

## 🎯 Key Design Principles

1. **Security First**: All security decisions prioritize safety over convenience
2. **Zero Dependencies**: Minimal external dependencies for deployment simplicity
3. **Hot-Reload**: Configuration changes without service interruption
4. **Async-First**: Scalable async operations throughout the stack
5. **Interface-Based**: Dependency injection and testability
6. **Error Resilience**: Graceful degradation and comprehensive error handling
7. **Backward Compatibility**: Support for legacy hash formats and configurations
8. **Performance**: Efficient caching and connection management
9. **Observability**: Comprehensive logging and metrics collection
10. **Maintainability**: Clean architecture and separation of concerns

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
- **Error Handling**: Comprehensive error handling with audit logging

### Core Layer
- **Business Logic**: User validation, permission checking, password management
- **Data Access**: Abstract user store operations with multiple backends
- **Security**: Cryptographic operations and audit logging
- **Configuration**: Application settings and environment management
- **Monitoring**: Metrics collection and health monitoring

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
- **Modern .NET Features**: Nullable reference types, latest C# features

### Security
- **Cryptographic Best Practices**: Industry-standard algorithms and implementations
- **Secure Memory Management**: Proper handling of sensitive data
- **Input Validation**: Comprehensive validation and sanitization
- **Audit Logging**: Complete audit trail for compliance

## 🌟 Recent Improvements

### Modern SDK-Style Projects
- **Updated Project Files**: Converted to modern SDK-style .csproj format
- **Package Management**: NuGet package references with proper versioning
- **Build Configurations**: Support for mixed builds and pack-only configurations
- **Dependency Resolution**: Improved package reference conflict resolution

### Enhanced Security
- **Multiple Hash Algorithms**: Support for BCrypt, PBKDF2, and Argon2
- **Encryption Options**: DPAPI and AES-GCM encryption support
- **Secure Memory**: Enhanced secure memory handling utilities
- **Audit Logging**: Comprehensive Windows Event Log integration

### Improved Monitoring
- **Metrics Collection**: Prometheus-compatible metrics
- **Health Endpoints**: System health and dependency monitoring
- **Performance Tracking**: Response time and throughput metrics
- **Structured Logging**: JSON-formatted log entries

This codebase demonstrates excellent software engineering practices with clear separation of concerns, comprehensive error handling, and a focus on security and maintainability. The architecture supports both simple deployments and complex enterprise scenarios, with modern .NET features and comprehensive monitoring capabilities. 