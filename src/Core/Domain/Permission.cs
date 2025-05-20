namespace IIS.Ftp.SimpleAuth.Core.Domain
{
    /// <summary>
    /// Per-path access rights record for a user.
    /// </summary>
    public class Permission
    {
        public string Path { get; set; } = "/";

        public bool CanRead { get; set; }

        public bool CanWrite { get; set; }
    }
} 