using System;
using System.Collections.Generic;
using CommandLine;
using IIS.Ftp.SimpleAuth.ManagementCli.Commands;

namespace IIS.Ftp.SimpleAuth.ManagementCli
{
    class Program
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<
                CreateUserOptions,
                ChangePasswordOptions,
                ListUsersOptions,
                AddPermissionOptions,
                RemoveUserOptions,
                GenerateKeyOptions,
                EncryptFileOptions,
                DecryptFileOptions,
                RotateKeyOptions>(args)
                .MapResult(
                    (CreateUserOptions opts) => UserCommands.CreateUser(opts),
                    (ChangePasswordOptions opts) => UserCommands.ChangePassword(opts),
                    (ListUsersOptions opts) => UserCommands.ListUsers(opts),
                    (AddPermissionOptions opts) => UserCommands.AddPermission(opts),
                    (RemoveUserOptions opts) => UserCommands.RemoveUser(opts),
                    (GenerateKeyOptions opts) => EncryptionCommands.GenerateKey(opts),
                    (EncryptFileOptions opts) => EncryptionCommands.EncryptFile(opts),
                    (DecryptFileOptions opts) => EncryptionCommands.DecryptFile(opts),
                    (RotateKeyOptions opts) => EncryptionCommands.RotateKey(opts),
                    errs => 1);
        }
    }
} 