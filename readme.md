# IIS-FTP-SimpleAuthProvider
This is a simple authentication provider for IIS FTP.

## Features
- Simple authentication (not bind with Active Directory or Windows Account)
- use only user name(or ID) and password
- Easy to use
- provide folder permission with this authprovider's user list

## Requirement
- IIS 10.0 or later
- .NET Framework 4.8 or later
### for Build
- Visual Studio 2019 or later | else Visual Studio Code with C# extension
    - Also .NET 4.8 SDK is required.
- Also for build, You need to install FTP service on your windows machine.
    - Cause, this project use some classes in `Microsoft.Web.FtpServer` assembly.
      and this assembly is only installed with FTP service. Not Redistributable.
