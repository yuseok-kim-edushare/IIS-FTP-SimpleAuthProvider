using System;

namespace IIS.Ftp.SimpleAuth.Core.Domain
{
    /// <summary>
    /// Per-path access rights record for a user.
    /// </summary>
    public class Permission : IEquatable<Permission>
    {
        public string Path { get; set; } = "/";

        public bool CanRead { get; set; }

        public bool CanWrite { get; set; }

        public bool Equals(Permission? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Path == other.Path && CanRead == other.CanRead && CanWrite == other.CanWrite;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Permission)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + (Path?.GetHashCode() ?? 0);
                hash = hash * 23 + CanRead.GetHashCode();
                hash = hash * 23 + CanWrite.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(Permission? left, Permission? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Permission? left, Permission? right)
        {
            return !Equals(left, right);
        }
    }
} 