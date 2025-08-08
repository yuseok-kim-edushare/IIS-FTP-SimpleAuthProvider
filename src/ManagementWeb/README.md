# FTP Management Web UI

ASP.NET MVC 5 web application for managing FTP users and permissions.

## Features

- **User Management**: Create, edit, delete, and manage FTP users
- **Permission Management**: Grant read/write access to specific directories
- **Dashboard**: View system health, authentication metrics, and recent activity
- **Security**: Forms authentication, HTTPS enforcement, anti-forgery tokens
- **Health Monitoring**: `/healthz` endpoint for health checks and `/metrics` for Prometheus

## Requirements

- Windows Server with IIS 8.0 or later
- .NET Framework 4.8
- Application Pool with .NET CLR v4.0, Integrated pipeline mode

## Quick Start

1. **Build the project**:
   ```powershell
   msbuild ManagementWeb.csproj /p:Configuration=Release
   ```

2. **Configure IIS**:
   - Create a new website or application
   - Set the physical path to the project directory
   - Configure HTTPS binding (required)
   - Set application pool to .NET v4.0, Integrated mode

3. **Configure application**:
   Edit `Web.config` with your settings:
   ```xml
   <appSettings>
     <add key="UserStore:Type" value="Json" />
     <add key="UserStore:Path" value="C:\inetpub\ftpusers\users.enc" />
     <add key="AllowedAdmins" value="admin1,admin2" />
   </appSettings>
   ```

4. **Set environment variables (for AES‑256‑GCM at rest)**:
   ```powershell
   [System.Environment]::SetEnvironmentVariable("FTP_USERS_KEY", "your-encryption-key", "Machine")
   ```

## Configuration

### User Store Types

- **JSON**: File-based storage with optional AES‑256‑GCM encryption (set `FTP_USERS_KEY`)
- **SQLite**: Lightweight database storage
- **SQL Server**: Enterprise database storage

### Security Settings

- Forms authentication with configurable timeout
- HTTPS enforcement via URL rewrite
- Content Security Policy headers
- Anti-forgery token validation

### Allowed Administrators

Only users listed in `AllowedAdmins` can access the web UI:
```xml
<add key="AllowedAdmins" value="admin1,admin2,admin3" />
```

## Deployment

### Using Web Deploy

1. Install Web Deploy on target server
2. Create deployment package:
   ```powershell
   msbuild ManagementWeb.csproj /p:DeployOnBuild=true /p:PublishProfile=Release
   ```
3. Deploy to IIS

### Manual Deployment

1. Build in Release mode
2. Copy entire project folder to IIS server
3. Set appropriate permissions on App_Data folder
4. Configure IIS as described above

## Troubleshooting

### Common Issues

1. **500 Internal Server Error**
   - Check Event Viewer for detailed errors
   - Ensure .NET Framework 4.8 is installed
   - Verify app pool settings

2. **Access Denied**
   - Ensure user is listed in AllowedAdmins
   - Check Forms Authentication cookie settings

3. **User Store Connection Failed**
   - Verify encryption key environment variable
   - Check file/database permissions
   - Review connection strings

### Logging

- Application logs: Windows Event Log
- Audit logs: Configured audit log path
- IIS logs: `%SystemDrive%\inetpub\logs\LogFiles`

## API Endpoints

- `GET /healthz` - Health check endpoint
- `GET /metrics` - Prometheus metrics endpoint

## Development

### Local Development

1. Open in Visual Studio 2019 or later
2. Restore NuGet packages
3. Press F5 to run with IIS Express

### Adding New Features

1. Follow MVC 5 patterns
2. Use dependency injection via Unity
3. Add unit tests for new functionality

## License

See LICENSE file in repository root. 