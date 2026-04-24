using System;

namespace Dynamics365.Security.Models
{
    /// <summary>
    /// Defines a single column in the <see cref="SecurityPermissionGrid"/>.
    /// Each column represents one combination of security role + entity + access right.
    /// </summary>
    public sealed class SecurityGridColumn
    {
        /// <summary>
        /// Unique key used to look up this column in a row's cell dictionary.
        /// Composed as <c>{RoleId}|{EntityName}|{AccessRight}</c>.
        /// </summary>
        public string Key { get; }

        /// <summary>The unique identifier of the security role for this column.</summary>
        public Guid RoleId { get; }

        /// <summary>The display name of the security role for this column.</summary>
        public string RoleName { get; }

        /// <summary>
        /// The entity schema name this column pertains to (e.g. <c>Account</c>).
        /// </summary>
        public string EntityName { get; }

        /// <summary>The access right this column represents.</summary>
        public EntityAccessRight AccessRight { get; }

        /// <summary>Initialises a new <see cref="SecurityGridColumn"/>.</summary>
        public SecurityGridColumn(Guid roleId, string roleName,
                                   string entityName, EntityAccessRight accessRight)
        {
            RoleId      = roleId;
            RoleName    = roleName ?? throw new ArgumentNullException(nameof(roleName));
            EntityName  = entityName ?? throw new ArgumentNullException(nameof(entityName));
            AccessRight = accessRight;
            Key         = $"{roleId}|{entityName}|{accessRight}";
        }

        /// <summary>
        /// Returns a human-readable label such as
        /// <c>Sales Manager | Account | Read</c>.
        /// </summary>
        public override string ToString() => $"{RoleName} | {EntityName} | {AccessRight}";
    }
}
