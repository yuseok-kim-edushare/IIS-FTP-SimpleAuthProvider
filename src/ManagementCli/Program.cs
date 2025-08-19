using System;
using System.Collections.Generic;
using System.Linq;
using IIS.Ftp.SimpleAuth.ManagementCli.Commands;

namespace IIS.Ftp.SimpleAuth.ManagementCli
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowHelp();
                return 0;
            }

            try
            {
                var command = args[0].ToLowerInvariant();
                var commandArgs = args.Skip(1).ToArray();

                return command switch
                {
                    "create-user" => HandleCreateUser(commandArgs),
                    "change-password" => HandleChangePassword(commandArgs),
                    "list-users" => HandleListUsers(commandArgs),
                    "add-permission" => HandleAddPermission(commandArgs),
                    "remove-user" => HandleRemoveUser(commandArgs),
                    "generate-key" => HandleGenerateKey(commandArgs),
                    "encrypt-file" => HandleEncryptFile(commandArgs),
                    "decrypt-file" => HandleDecryptFile(commandArgs),
                    "rotate-key" => HandleRotateKey(commandArgs),
                    "help" => ShowHelp(),
                    "--help" => ShowHelp(),
                    "-h" => ShowHelp(),
                    _ => ShowUnknownCommand(command)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("Use 'help' command for usage information.");
                return 1;
            }
        }

        static int ShowHelp()
        {
            Console.WriteLine("IIS FTP Simple Authentication Provider Management Tool");
            Console.WriteLine("Usage: ftpauth <command> [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  create-user     Create a new FTP user");
            Console.WriteLine("  change-password Change a user's password");
            Console.WriteLine("  list-users      List all FTP users");
            Console.WriteLine("  add-permission  Add or update permission for a user");
            Console.WriteLine("  remove-user     Remove a user from the system");
            Console.WriteLine("  generate-key    Generate a new encryption key");
            Console.WriteLine("  encrypt-file    Encrypt a users JSON file");
            Console.WriteLine("  decrypt-file    Decrypt an encrypted users file");
            Console.WriteLine("  rotate-key      Rotate encryption key for user files");
            Console.WriteLine("  help            Show this help message");
            Console.WriteLine();
            Console.WriteLine("Use 'ftpauth <command> --help' for detailed command help.");
            return 0;
        }

        static int ShowUnknownCommand(string command)
        {
            Console.WriteLine($"Unknown command: {command}");
            Console.WriteLine("Use 'help' command for available commands.");
            return 1;
        }

        static int HandleCreateUser(string[] args)
        {
            if (args.Contains("--help") || args.Contains("-h"))
            {
                Console.WriteLine("create-user: Create a new FTP user");
                Console.WriteLine("Usage: create-user --file <path> --user <username> --password <password> --name <displayname> [options]");
                Console.WriteLine();
                Console.WriteLine("Required options:");
                Console.WriteLine("  --file <path>     Path to the users JSON file");
                Console.WriteLine("  --user <username> User ID (username)");
                Console.WriteLine("  --password <pwd>  User password");
                Console.WriteLine("  --name <name>     Display name");
                Console.WriteLine();
                Console.WriteLine("Optional options:");
                Console.WriteLine("  --home <path>     Home directory path (default: /)");
                Console.WriteLine("  --read            Grant read permission (default: true)");
                Console.WriteLine("  --write           Grant write permission (default: false)");
                Console.WriteLine("  --iterations <n>  PBKDF2 iterations (default: 100000)");
                return 0;
            }

            var options = ParseCreateUserOptions(args);
            if (options == null) return 1;

            UserCommands.CreateUser(options);
            return 0;
        }

        static int HandleChangePassword(string[] args)
        {
            if (args.Contains("--help") || args.Contains("-h"))
            {
                Console.WriteLine("change-password: Change a user's password");
                Console.WriteLine("Usage: change-password --file <path> --user <username> --password <newpassword> [options]");
                Console.WriteLine();
                Console.WriteLine("Required options:");
                Console.WriteLine("  --file <path>     Path to the users JSON file");
                Console.WriteLine("  --user <username> User ID (username)");
                Console.WriteLine("  --password <pwd>  New password");
                Console.WriteLine();
                Console.WriteLine("Optional options:");
                Console.WriteLine("  --iterations <n>  PBKDF2 iterations (default: 100000)");
                return 0;
            }

            var options = ParseChangePasswordOptions(args);
            if (options == null) return 1;

            UserCommands.ChangePassword(options);
            return 0;
        }

        static int HandleListUsers(string[] args)
        {
            if (args.Contains("--help") || args.Contains("-h"))
            {
                Console.WriteLine("list-users: List all FTP users");
                Console.WriteLine("Usage: list-users --file <path>");
                Console.WriteLine();
                Console.WriteLine("Required options:");
                Console.WriteLine("  --file <path>     Path to the users JSON file");
                return 0;
            }

            var options = ParseListUsersOptions(args);
            if (options == null) return 1;

            UserCommands.ListUsers(options);
            return 0;
        }

        static int HandleAddPermission(string[] args)
        {
            if (args.Contains("--help") || args.Contains("-h"))
            {
                Console.WriteLine("add-permission: Add or update permission for a user");
                Console.WriteLine("Usage: add-permission --file <path> --user <username> --path <dirpath> [options]");
                Console.WriteLine();
                Console.WriteLine("Required options:");
                Console.WriteLine("  --file <path>     Path to the users JSON file");
                Console.WriteLine("  --user <username> User ID (username)");
                Console.WriteLine("  --path <dirpath>  Directory path");
                Console.WriteLine();
                Console.WriteLine("Optional options:");
                Console.WriteLine("  --read            Grant read permission (default: true)");
                Console.WriteLine("  --write           Grant write permission (default: false)");
                return 0;
            }

            var options = ParseAddPermissionOptions(args);
            if (options == null) return 1;

            UserCommands.AddPermission(options);
            return 0;
        }

        static int HandleRemoveUser(string[] args)
        {
            if (args.Contains("--help") || args.Contains("-h"))
            {
                Console.WriteLine("remove-user: Remove a user from the system");
                Console.WriteLine("Usage: remove-user --file <path> --user <username>");
                Console.WriteLine();
                Console.WriteLine("Required options:");
                Console.WriteLine("  --file <path>     Path to the users JSON file");
                Console.WriteLine("  --user <username> User ID (username) to remove");
                return 0;
            }

            var options = ParseRemoveUserOptions(args);
            if (options == null) return 1;

            UserCommands.RemoveUser(options);
            return 0;
        }

        static int HandleGenerateKey(string[] args)
        {
            if (args.Contains("--help") || args.Contains("-h"))
            {
                Console.WriteLine("generate-key: Generate a new encryption key");
                Console.WriteLine("Usage: generate-key [options]");
                Console.WriteLine();
                Console.WriteLine("Optional options:");
                Console.WriteLine("  --output <path>   Output file for the key (if not specified, outputs to console)");
                return 0;
            }

            var options = ParseGenerateKeyOptions(args);
            EncryptionCommands.GenerateKey(options);
            return 0;
        }

        static int HandleEncryptFile(string[] args)
        {
            if (args.Contains("--help") || args.Contains("-h"))
            {
                Console.WriteLine("encrypt-file: Encrypt a users JSON file");
                Console.WriteLine("Usage: encrypt-file --input <inputfile> --output <outputfile> [options]");
                Console.WriteLine();
                Console.WriteLine("Required options:");
                Console.WriteLine("  --input <file>    Path to the plain text JSON file");
                Console.WriteLine("  --output <file>   Path for the encrypted output file");
                Console.WriteLine();
                Console.WriteLine("Optional options:");
                Console.WriteLine("  --key-env <var>   Environment variable containing encryption key (uses DPAPI if not specified)");
                return 0;
            }

            var options = ParseEncryptFileOptions(args);
            if (options == null) return 1;

            EncryptionCommands.EncryptFile(options);
            return 0;
        }

        static int HandleDecryptFile(string[] args)
        {
            if (args.Contains("--help") || args.Contains("-h"))
            {
                Console.WriteLine("decrypt-file: Decrypt an encrypted users file");
                Console.WriteLine("Usage: decrypt-file --input <inputfile> --output <outputfile> [options]");
                Console.WriteLine();
                Console.WriteLine("Required options:");
                Console.WriteLine("  --input <file>    Path to the encrypted file");
                Console.WriteLine("  --output <file>   Path for the decrypted output file");
                Console.WriteLine();
                Console.WriteLine("Optional options:");
                Console.WriteLine("  --key-env <var>   Environment variable containing encryption key (uses DPAPI if not specified)");
                return 0;
            }

            var options = ParseDecryptFileOptions(args);
            if (options == null) return 1;

            EncryptionCommands.DecryptFile(options);
            return 0;
        }

        static int HandleRotateKey(string[] args)
        {
            if (args.Contains("--help") || args.Contains("-h"))
            {
                Console.WriteLine("rotate-key: Rotate encryption key for user files");
                Console.WriteLine("Usage: rotate-key --file <path> --old-key-env <var> --new-key-env <var>");
                Console.WriteLine();
                Console.WriteLine("Required options:");
                Console.WriteLine("  --file <path>         Path to the encrypted users file");
                Console.WriteLine("  --old-key-env <var>  Environment variable containing the current encryption key");
                Console.WriteLine("  --new-key-env <var>  Environment variable containing the new encryption key");
                return 0;
            }

            var options = ParseRotateKeyOptions(args);
            if (options == null) return 1;

            EncryptionCommands.RotateKey(options);
            return 0;
        }

        // Option parsing methods
        static CreateUserOptions? ParseCreateUserOptions(string[] args)
        {
            var options = new CreateUserOptions();
            
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--file":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --file requires a value"); return null; }
                        options.FilePath = args[++i]; break;
                    case "--user":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --user requires a value"); return null; }
                        options.UserId = args[++i]; break;
                    case "--password":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --password requires a value"); return null; }
                        options.Password = args[++i]; break;
                    case "--name":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --name requires a value"); return null; }
                        options.DisplayName = args[++i]; break;
                    case "--home":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --home requires a value"); return null; }
                        options.HomeDirectory = args[++i]; break;
                    case "--read":
                        options.CanRead = true; break;
                    case "--write":
                        options.CanWrite = true; break;
                    case "--iterations":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --iterations requires a value"); return null; }
                        if (!int.TryParse(args[++i], out int iterations)) { Console.WriteLine("Error: --iterations must be a number"); return null; }
                        options.Iterations = iterations; break;
                }
            }

            // Validate required options
            if (string.IsNullOrEmpty(options.FilePath)) { Console.WriteLine("Error: --file is required"); return null; }
            if (string.IsNullOrEmpty(options.UserId)) { Console.WriteLine("Error: --user is required"); return null; }
            if (string.IsNullOrEmpty(options.Password)) { Console.WriteLine("Error: --password is required"); return null; }
            if (string.IsNullOrEmpty(options.DisplayName)) { Console.WriteLine("Error: --name is required"); return null; }

            return options;
        }

        static ChangePasswordOptions? ParseChangePasswordOptions(string[] args)
        {
            var options = new ChangePasswordOptions();
            
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--file":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --file requires a value"); return null; }
                        options.FilePath = args[++i]; break;
                    case "--user":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --user requires a value"); return null; }
                        options.UserId = args[++i]; break;
                    case "--password":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --password requires a value"); return null; }
                        options.NewPassword = args[++i]; break;
                    case "--iterations":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --iterations requires a value"); return null; }
                        if (!int.TryParse(args[++i], out int iterations)) { Console.WriteLine("Error: --iterations must be a number"); return null; }
                        options.Iterations = iterations; break;
                }
            }

            if (string.IsNullOrEmpty(options.FilePath)) { Console.WriteLine("Error: --file is required"); return null; }
            if (string.IsNullOrEmpty(options.UserId)) { Console.WriteLine("Error: --user is required"); return null; }
            if (string.IsNullOrEmpty(options.NewPassword)) { Console.WriteLine("Error: --password is required"); return null; }

            return options;
        }

        static ListUsersOptions? ParseListUsersOptions(string[] args)
        {
            var options = new ListUsersOptions();
            
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLowerInvariant() == "--file")
                {
                    if (i + 1 >= args.Length) { Console.WriteLine("Error: --file requires a value"); return null; }
                    options.FilePath = args[++i];
                }
            }

            if (string.IsNullOrEmpty(options.FilePath)) { Console.WriteLine("Error: --file is required"); return null; }
            return options;
        }

        static AddPermissionOptions? ParseAddPermissionOptions(string[] args)
        {
            var options = new AddPermissionOptions();
            
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--file":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --file requires a value"); return null; }
                        options.FilePath = args[++i]; break;
                    case "--user":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --user requires a value"); return null; }
                        options.UserId = args[++i]; break;
                    case "--path":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --path requires a value"); return null; }
                        options.Path = args[++i]; break;
                    case "--read":
                        options.CanRead = true; break;
                    case "--write":
                        options.CanWrite = true; break;
                }
            }

            if (string.IsNullOrEmpty(options.FilePath)) { Console.WriteLine("Error: --file is required"); return null; }
            if (string.IsNullOrEmpty(options.UserId)) { Console.WriteLine("Error: --user is required"); return null; }
            if (string.IsNullOrEmpty(options.Path)) { Console.WriteLine("Error: --path is required"); return null; }

            return options;
        }

        static RemoveUserOptions? ParseRemoveUserOptions(string[] args)
        {
            var options = new RemoveUserOptions();
            
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--file":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --file requires a value"); return null; }
                        options.FilePath = args[++i]; break;
                    case "--user":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --user requires a value"); return null; }
                        options.UserId = args[++i]; break;
                }
            }

            if (string.IsNullOrEmpty(options.FilePath)) { Console.WriteLine("Error: --file is required"); return null; }
            if (string.IsNullOrEmpty(options.UserId)) { Console.WriteLine("Error: --user is required"); return null; }

            return options;
        }

        static GenerateKeyOptions ParseGenerateKeyOptions(string[] args)
        {
            var options = new GenerateKeyOptions();
            
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLowerInvariant() == "--output")
                {
                    if (i + 1 >= args.Length) { Console.WriteLine("Warning: --output requires a value, ignoring"); continue; }
                    options.OutputFile = args[++i];
                }
            }

            return options;
        }

        static EncryptFileOptions? ParseEncryptFileOptions(string[] args)
        {
            var options = new EncryptFileOptions();
            
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--input":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --input requires a value"); return null; }
                        options.InputFile = args[++i]; break;
                    case "--output":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --output requires a value"); return null; }
                        options.OutputFile = args[++i]; break;
                    case "--key-env":
                        if (i + 1 >= args.Length) { Console.WriteLine("Warning: --key-env requires a value, ignoring"); continue; }
                        options.KeyEnvironmentVariable = args[++i]; break;
                }
            }

            if (string.IsNullOrEmpty(options.InputFile)) { Console.WriteLine("Error: --input is required"); return null; }
            if (string.IsNullOrEmpty(options.OutputFile)) { Console.WriteLine("Error: --output is required"); return null; }

            return options;
        }

        static DecryptFileOptions? ParseDecryptFileOptions(string[] args)
        {
            var options = new DecryptFileOptions();
            
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--input":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --input requires a value"); return null; }
                        options.InputFile = args[++i]; break;
                    case "--output":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --output requires a value"); return null; }
                        options.OutputFile = args[++i]; break;
                    case "--key-env":
                        if (i + 1 >= args.Length) { Console.WriteLine("Warning: --key-env requires a value, ignoring"); continue; }
                        options.KeyEnvironmentVariable = args[++i]; break;
                }
            }

            if (string.IsNullOrEmpty(options.InputFile)) { Console.WriteLine("Error: --input is required"); return null; }
            if (string.IsNullOrEmpty(options.OutputFile)) { Console.WriteLine("Error: --output is required"); return null; }

            return options;
        }

        static RotateKeyOptions? ParseRotateKeyOptions(string[] args)
        {
            var options = new RotateKeyOptions();
            
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--file":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --file requires a value"); return null; }
                        options.FilePath = args[++i]; break;
                    case "--old-key-env":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --old-key-env requires a value"); return null; }
                        options.OldKeyEnvironmentVariable = args[++i]; break;
                    case "--new-key-env":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --new-key-env requires a value"); return null; }
                        options.NewKeyEnvironmentVariable = args[++i]; break;
                }
            }

            if (string.IsNullOrEmpty(options.FilePath)) { Console.WriteLine("Error: --file is required"); return null; }
            if (string.IsNullOrEmpty(options.OldKeyEnvironmentVariable)) { Console.WriteLine("Error: --old-key-env is required"); return null; }
            if (string.IsNullOrEmpty(options.NewKeyEnvironmentVariable)) { Console.WriteLine("Error: --new-key-env is required"); return null; }

            return options;
        }
    }
} 