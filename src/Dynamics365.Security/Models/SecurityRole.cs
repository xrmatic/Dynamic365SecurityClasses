using System;
using System.Collections.Generic;

namespace Dynamics365.Security.Models
{
    /// <summary>
    /// Represents a Dynamics 365 security role, including the full list of
    /// entity-level privileges that have been assigned to it.
    /// </summary>
    public sealed class SecurityRole
    {
        /// <summary>The unique identifier of the security role record.</summary>
        public Guid RoleId { get; set; }

        /// <summary>The display name of the security role.</summary>
        public string Name { get; set; }

        /// <summary>The unique identifier of the business unit that owns this role.</summary>
        public Guid BusinessUnitId { get; set; }

        /// <summary>
        /// All privileges assigned to this role, keyed by privilege GUID for fast lookup.
        /// </summary>
        public IList<SecurityRolePrivilege> Privileges { get; set; }
            = new List<SecurityRolePrivilege>();

        /// <summary>
        /// Returns a <see cref="SecurityRolePrivilege"/> for the given entity and access
        /// right combination, or <see langword="null"/> if the role does not grant it.
        /// </summary>
        public SecurityRolePrivilege GetPrivilege(string entityName, EntityAccessRight accessRight)
        {
            if (string.IsNullOrEmpty(entityName))
                return null;

            foreach (var p in Privileges)
            {
                if (string.Equals(p.EntityName, entityName, StringComparison.OrdinalIgnoreCase)
                    && p.AccessRight == accessRight)
                    return p;
            }
            return null;
        }

        /// <inheritdoc />
        public override string ToString() => $"{Name} ({RoleId})";
    }
}
