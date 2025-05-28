using System;
using System.IO;
using IIS.Ftp.SimpleAuth.Core.Security;
using IIS.Ftp.SimpleAuth.Core.Tools;

namespace IIS.Ftp.SimpleAuth.ManagementCli.Commands
{
    public static class EncryptionCommands
    {
        public static int GenerateKey(GenerateKeyOptions options)
        {
            try
            {
                var key = UserManager.GenerateEncryptionKey();
                
                if (!string.IsNullOrEmpty(options.OutputFile))
                {
                    File.WriteAllText(options.OutputFile, key);
                    Console.WriteLine($"✓ Encryption key generated and saved to: {options.OutputFile}");
                    Console.WriteLine("⚠️  Keep this key secure! Store it in an environment variable or secure key vault.");
                }
                else
                {
                    Console.WriteLine("Generated encryption key (256-bit AES):");
                    Console.WriteLine(key);
                    Console.WriteLine();
                    Console.WriteLine("⚠️  Keep this key secure! Store it in an environment variable or secure key vault.");
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"✗ Error generating key: {ex.Message}");
                return 1;
            }
        }

        public static int EncryptFile(EncryptFileOptions options)
        {
            try
            {
                UserManager.EncryptUserFile(
                    options.InputFile,
                    options.OutputFile,
                    options.KeyEnvironmentVariable);

                Console.WriteLine($"✓ File encrypted successfully");
                Console.WriteLine($"  Input:  {options.InputFile}");
                Console.WriteLine($"  Output: {options.OutputFile}");
                
                if (!string.IsNullOrEmpty(options.KeyEnvironmentVariable))
                {
                    Console.WriteLine($"  Key from: ${options.KeyEnvironmentVariable}");
                }
                else
                {
                    Console.WriteLine($"  Encrypted with: DPAPI (machine-specific)");
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"✗ Error encrypting file: {ex.Message}");
                return 1;
            }
        }

        public static int DecryptFile(DecryptFileOptions options)
        {
            try
            {
                UserManager.DecryptUserFile(
                    options.InputFile,
                    options.OutputFile,
                    options.KeyEnvironmentVariable);

                Console.WriteLine($"✓ File decrypted successfully");
                Console.WriteLine($"  Input:  {options.InputFile}");
                Console.WriteLine($"  Output: {options.OutputFile}");
                
                if (!string.IsNullOrEmpty(options.KeyEnvironmentVariable))
                {
                    Console.WriteLine($"  Key from: ${options.KeyEnvironmentVariable}");
                }
                else
                {
                    Console.WriteLine($"  Decrypted with: DPAPI (machine-specific)");
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"✗ Error decrypting file: {ex.Message}");
                return 1;
            }
        }

        public static int RotateKey(RotateKeyOptions options)
        {
            try
            {
                Console.WriteLine("Starting key rotation...");
                Console.WriteLine($"  File: {options.FilePath}");
                Console.WriteLine($"  Old key from: ${options.OldKeyEnvironmentVariable}");
                Console.WriteLine($"  New key from: ${options.NewKeyEnvironmentVariable}");
                Console.WriteLine();
                
                UserManager.RotateEncryptionKey(
                    options.FilePath,
                    options.OldKeyEnvironmentVariable,
                    options.NewKeyEnvironmentVariable);
                
                Console.WriteLine();
                Console.WriteLine("⚠️  Remember to update your configuration to use the new key environment variable!");
                
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"✗ Error rotating key: {ex.Message}");
                return 1;
            }
        }
    }
} 