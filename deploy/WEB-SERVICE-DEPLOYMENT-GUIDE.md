# Web Service Local Deployment Guide

This guide explains how to deploy and test the IIS FTP SimpleAuthProvider Management Web Service locally on IIS.

## Prerequisites

Before running the deployment scripts, ensure you have:

1. **Windows 10/11** with IIS installed
2. **Administrator privileges** (required for IIS operations)
3. **.NET Framework 4.8** or higher
4. **Visual Studio 2022** or **Build Tools** (required for MSBuild)
5. **PowerShell 5.1** or higher

## Quick Start

### 1. Test the Build Process (Recommended First Step)

Before deploying, test that the build process works:

```powershell
# Navigate to the deploy directory
cd deploy

# Test the build process (no admin privileges needed)
.\test-build.ps1
```

This will:
- Check all prerequisites
- Test MSBuild build and publish process
- Verify build output without deploying

### 2. Deploy the Web Service

After successful build test, run the deployment script as Administrator:

```powershell
# Run the deployment script (as Administrator)
.\deploy-web-service-local.ps1
```

This will:
- Check all prerequisites
- Build the ManagementWeb project using MSBuild
- Deploy to IIS on port 8080
- Test the web service
- Show deployment summary

### 2. Access the Web Service

After successful deployment, open your browser and navigate to:
```
http://localhost:8080
```

### 3. Cleanup (when needed)

To remove the deployment:

```powershell
# Run the cleanup script (as Administrator)
.\cleanup-web-service-local.ps1
```

## Available Scripts

### test-build.ps1

A standalone script to test the build process without deploying:

```powershell
.\test-build.ps1 -Configuration Release
```

**Parameters:**
- `-Configuration`: Build configuration (Debug/Release, default: Release)

**Use Cases:**
- Verify build tools are working before deployment
- Test build process without admin privileges
- Debug build issues independently

### deploy-web-service-local.ps1

The main deployment script that builds and deploys to IIS.

## Script Parameters

### deploy-web-service-local.ps1

| Parameter | Default | Description |
|-----------|---------|-------------|
| `-Configuration` | "Release" | Build configuration (Debug/Release) |
| `-IISSiteName` | "ftpauth" | Name of the IIS website |
| `-IISAppPoolName` | "ftpauth-pool" | Name of the IIS application pool |
| `-Port` | "8080" | Port number for the website |
| `-SkipBuild` | false | Skip the build step |
| `-SkipDeploy` | false | Skip the deployment step |
| `-SkipTest` | false | Skip the testing step |

### Examples

```powershell
# Deploy with custom settings
.\deploy-web-service-local.ps1 -Port 9000 -Configuration Debug

# Skip testing
.\deploy-web-service-local.ps1 -SkipTest

# Only deploy (skip build and test)
.\deploy-web-service-local.ps1 -SkipBuild -SkipTest
```

## Build Process

The deployment script uses MSBuild exclusively:

1. **Build Method**: Uses MSBuild for building the project
2. **Publish Method**: Uses MSBuild Web Publishing Pipeline for publishing to the output directory
3. **Project Targeting**: Builds only the ManagementWeb project, not the entire solution

### Build Requirements

- **Visual Studio 2022** or **Build Tools**: Required for MSBuild.exe
- **MSBuild**: The primary build tool for .NET Framework projects

## What Gets Deployed

The script deploys only the **ManagementWeb** project, which includes:

- Web application files (Views, Controllers, Models)
- Static content (CSS, JavaScript, Images)
- Configuration files (Web.config)
- Dependencies and assemblies

## IIS Resources Created

The deployment creates:

1. **Application Pool**: `ftpauth-pool`
   - .NET Framework 4.0 runtime
   - ApplicationPoolIdentity security

2. **Website**: `ftpauth`
   - Port: 8080 (configurable)
   - Physical path: `[ProjectRoot]\publish\ManagementWeb`
   - Application pool: `ftpauth-pool`

## Troubleshooting

### Common Issues

1. **"Access Denied" errors**
   - Ensure you're running PowerShell as Administrator

2. **MSBuild not found**
   - Install Visual Studio 2022 Build Tools or Visual Studio
   - Ensure MSBuild.exe is available in standard installation paths

3. **IIS not installed**
   - Enable IIS in Windows Features
   - Install Web Server (IIS) role

4. **Port already in use**
   - Change the port using `-Port` parameter
   - Check what's using the port: `netstat -an | findstr :8080`

5. **Build failures**
   - Check that all dependencies are available
   - Verify .NET Framework version
   - Check project file syntax
   - Ensure MSBuild is working: `msbuild /version`

### Debug Mode

For troubleshooting, you can:

```powershell
# Deploy in Debug mode
.\deploy-web-service-local.ps1 -Configuration Debug

# Check IIS configuration
Get-Website -Name "ftpauth"
Get-WebAppPool -Name "ftpauth-pool"

# Check application pool status
Get-WebAppPoolState -Name "ftpauth-pool"
```

### Logs and Monitoring

- **IIS Logs**: Check `%SystemDrive%\inetpub\logs\LogFiles\W3SVC*`
- **Application Pool Logs**: Check Event Viewer > Windows Logs > Application
- **Build Output**: Check the console output for detailed build information

## Security Considerations

- The web service runs under `ApplicationPoolIdentity`
- Forms authentication is configured in Web.config
- SSL is not enabled by default (configure as needed)
- Admin users are configured via `AllowedAdmins` setting

## Next Steps

After successful deployment:

1. **Test the web interface** by navigating to `http://localhost:8080`
2. **Configure user authentication** if needed
3. **Set up SSL** for production use
4. **Configure logging and monitoring**
5. **Test FTP authentication integration**

## Support

If you encounter issues:

1. Check the troubleshooting section above
2. Review the script output for error messages
3. Check IIS configuration in IIS Manager
4. Verify all prerequisites are met
5. Check Windows Event Viewer for system errors
