using System;

namespace Dynamics365.Security.Models
{
    /// <summary>
    /// Represents a single privilege (entity + access right combination) that is assigned
    /// to a <see cref="SecurityRole"/> at a specific depth level.
    /// </summary>
    public sealed class SecurityRolePrivilege
    {
        /// <summary>
        /// The unique identifier of the underlying CRM <c>privilege</c> record.
        /// </summary>
        public Guid PrivilegeId { get; set; }

        /// <summary>
        /// The raw CRM privilege name (e.g. <c>prvReadAccount</c>).
        /// </summary>
        public string PrivilegeName { get; set; }

        /// <summary>
        /// The schema name of the entity to which this privilege applies
        /// (e.g. <c>Account</c>, <c>Contact</c>). Will be <see langword="null"/> for
        /// global (non-entity) privileges.
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        /// The type of access right this privilege represents.
        /// </summary>
        public EntityAccessRight AccessRight { get; set; }

        /// <summary>
        /// The depth (scope) at which this privilege has been granted within the role.
        /// </summary>
        public PrivilegeDepthLevel Depth { get; set; }

        /// <summary>
        /// Returns a human-readable description such as <c>Account – Read (Global)</c>.
        /// </summary>
        public override string ToString()
        {
            return string.IsNullOrEmpty(EntityName)
                ? $"{PrivilegeName} ({Depth})"
                : $"{EntityName} – {AccessRight} ({Depth})";
        }
    }
}
