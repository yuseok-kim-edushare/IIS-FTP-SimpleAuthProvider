namespace IIS.Ftp.SimpleAuth.ManagementCli.Commands
{
    // User Management Commands

    public class CreateUserOptions
    {
        public string FilePath { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string HomeDirectory { get; set; } = "/";
        public bool CanRead { get; set; } = true;
        public bool CanWrite { get; set; } = false;
        public int Iterations { get; set; } = 100000;
    }

    public class ChangePasswordOptions
    {
        public string FilePath { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public int Iterations { get; set; } = 100000;
    }

    public class ListUsersOptions
    {
        public string FilePath { get; set; } = string.Empty;
    }

    public class AddPermissionOptions
    {
        public string FilePath { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool CanRead { get; set; } = true;
        public bool CanWrite { get; set; } = false;
    }

    public class RemoveUserOptions
    {
        public string FilePath { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    // Encryption Commands

    public class GenerateKeyOptions
    {
        public string? OutputFile { get; set; }
    }

    public class EncryptFileOptions
    {
        public string InputFile { get; set; } = string.Empty;
        public string OutputFile { get; set; } = string.Empty;
        public string? KeyEnvironmentVariable { get; set; }
    }

    public class DecryptFileOptions
    {
        public string InputFile { get; set; } = string.Empty;
        public string OutputFile { get; set; } = string.Empty;
        public string? KeyEnvironmentVariable { get; set; }
    }

    public class RotateKeyOptions
    {
        public string FilePath { get; set; } = string.Empty;
        public string OldKeyEnvironmentVariable { get; set; } = string.Empty;
        public string NewKeyEnvironmentVariable { get; set; } = string.Empty;
    }
} 