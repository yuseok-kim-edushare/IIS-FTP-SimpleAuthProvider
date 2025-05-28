using CommandLine;

namespace IIS.Ftp.SimpleAuth.ManagementCli.Commands
{
    // User Management Commands

    [Verb("create-user", HelpText = "Create a new FTP user")]
    public class CreateUserOptions
    {
        [Option('f', "file", Required = true, HelpText = "Path to the users JSON file")]
        public string FilePath { get; set; } = string.Empty;

        [Option('u', "user", Required = true, HelpText = "User ID (username)")]
        public string UserId { get; set; } = string.Empty;

        [Option('p', "password", Required = true, HelpText = "User password")]
        public string Password { get; set; } = string.Empty;

        [Option('n', "name", Required = true, HelpText = "Display name")]
        public string DisplayName { get; set; } = string.Empty;

        [Option('h', "home", Default = "/", HelpText = "Home directory path")]
        public string HomeDirectory { get; set; } = "/";

        [Option('r', "read", Default = true, HelpText = "Grant read permission to home directory")]
        public bool CanRead { get; set; }

        [Option('w', "write", Default = false, HelpText = "Grant write permission to home directory")]
        public bool CanWrite { get; set; }

        [Option('i', "iterations", Default = 100000, HelpText = "PBKDF2 iterations for password hashing")]
        public int Iterations { get; set; }
    }

    [Verb("change-password", HelpText = "Change a user's password")]
    public class ChangePasswordOptions
    {
        [Option('f', "file", Required = true, HelpText = "Path to the users JSON file")]
        public string FilePath { get; set; } = string.Empty;

        [Option('u', "user", Required = true, HelpText = "User ID (username)")]
        public string UserId { get; set; } = string.Empty;

        [Option('p', "password", Required = true, HelpText = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Option('i', "iterations", Default = 100000, HelpText = "PBKDF2 iterations for password hashing")]
        public int Iterations { get; set; }
    }

    [Verb("list-users", HelpText = "List all FTP users")]
    public class ListUsersOptions
    {
        [Option('f', "file", Required = true, HelpText = "Path to the users JSON file")]
        public string FilePath { get; set; } = string.Empty;
    }

    [Verb("add-permission", HelpText = "Add or update permission for a user")]
    public class AddPermissionOptions
    {
        [Option('f', "file", Required = true, HelpText = "Path to the users JSON file")]
        public string FilePath { get; set; } = string.Empty;

        [Option('u', "user", Required = true, HelpText = "User ID (username)")]
        public string UserId { get; set; } = string.Empty;

        [Option('p', "path", Required = true, HelpText = "Directory path")]
        public string Path { get; set; } = string.Empty;

        [Option('r', "read", Default = true, HelpText = "Grant read permission")]
        public bool CanRead { get; set; }

        [Option('w', "write", Default = false, HelpText = "Grant write permission")]
        public bool CanWrite { get; set; }
    }

    [Verb("remove-user", HelpText = "Remove a user from the system")]
    public class RemoveUserOptions
    {
        [Option('f', "file", Required = true, HelpText = "Path to the users JSON file")]
        public string FilePath { get; set; } = string.Empty;

        [Option('u', "user", Required = true, HelpText = "User ID (username) to remove")]
        public string UserId { get; set; } = string.Empty;
    }

    // Encryption Commands

    [Verb("generate-key", HelpText = "Generate a new encryption key")]
    public class GenerateKeyOptions
    {
        [Option('o', "output", HelpText = "Output file for the key (if not specified, outputs to console)")]
        public string? OutputFile { get; set; }
    }

    [Verb("encrypt-file", HelpText = "Encrypt a users JSON file")]
    public class EncryptFileOptions
    {
        [Option('i', "input", Required = true, HelpText = "Path to the plain text JSON file")]
        public string InputFile { get; set; } = string.Empty;

        [Option('o', "output", Required = true, HelpText = "Path for the encrypted output file")]
        public string OutputFile { get; set; } = string.Empty;

        [Option('k', "key-env", HelpText = "Environment variable containing encryption key (uses DPAPI if not specified)")]
        public string? KeyEnvironmentVariable { get; set; }
    }

    [Verb("decrypt-file", HelpText = "Decrypt an encrypted users file")]
    public class DecryptFileOptions
    {
        [Option('i', "input", Required = true, HelpText = "Path to the encrypted file")]
        public string InputFile { get; set; } = string.Empty;

        [Option('o', "output", Required = true, HelpText = "Path for the decrypted output file")]
        public string OutputFile { get; set; } = string.Empty;

        [Option('k', "key-env", HelpText = "Environment variable containing encryption key (uses DPAPI if not specified)")]
        public string? KeyEnvironmentVariable { get; set; }
    }

    [Verb("rotate-key", HelpText = "Rotate encryption key for user files")]
    public class RotateKeyOptions
    {
        [Option('f', "file", Required = true, HelpText = "Path to the encrypted users file")]
        public string FilePath { get; set; } = string.Empty;

        [Option('o', "old-key-env", Required = true, HelpText = "Environment variable containing the current encryption key")]
        public string OldKeyEnvironmentVariable { get; set; } = string.Empty;

        [Option('n', "new-key-env", Required = true, HelpText = "Environment variable containing the new encryption key")]
        public string NewKeyEnvironmentVariable { get; set; } = string.Empty;
    }
} 