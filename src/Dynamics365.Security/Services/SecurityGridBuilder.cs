using System;
using System.Collections.Generic;
using System.Linq;
using Dynamics365.Security.Models;

namespace Dynamics365.Security.Services
{
    /// <summary>
    /// Builds a <see cref="SecurityPermissionGrid"/> from a collection of
    /// <see cref="SecurityPrincipal"/> objects (users and/or teams) that have
    /// already been populated with their assigned roles and privileges.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each <b>column</b> represents a unique combination of
    /// security role × entity name × access right. Columns are ordered first
    /// by role name, then by entity name, then by access right value.
    /// </para>
    /// <para>
    /// Each <b>row</b> represents one principal. The cell value is the
    /// <see cref="PrivilegeDepthLevel"/> granted to that principal through
    /// any of their directly assigned roles. When the same privilege appears
    /// in multiple roles the highest (widest-scope) depth is used.
    /// </para>
    /// </remarks>
    public sealed class SecurityGridBuilder
    {
        // ------------------------------------------------------------------ //
        //  Options
        // ------------------------------------------------------------------ //

        /// <summary>
        /// When set, only columns for the listed entity names are included.
        /// Pass <see langword="null"/> or an empty set to include all entities.
        /// </summary>
        public ISet<string> EntityFilter { get; set; }

        /// <summary>
        /// When set, only columns for the listed access rights are included.
        /// Pass <see langword="null"/> or an empty set to include all access rights.
        /// </summary>
        public ISet<EntityAccessRight> AccessRightFilter { get; set; }

        // ------------------------------------------------------------------ //
        //  Build
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Builds and returns a <see cref="SecurityPermissionGrid"/> from the
        /// supplied principals.
        /// </summary>
        /// <param name="principals">
        ///   The users and/or teams to include as rows. Each must have its
        ///   <see cref="SecurityPrincipal.AssignedRoles"/> (and their
        ///   <see cref="SecurityRole.Privileges"/>) already populated.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   Thrown when <paramref name="principals"/> is null.
        /// </exception>
        public SecurityPermissionGrid Build(IEnumerable<SecurityPrincipal> principals)
        {
            if (principals == null) throw new ArgumentNullException(nameof(principals));

            var principalList = principals.ToList();

            // ---- 1.  Collect all unique columns --------------------------------
            // A column is identified by (roleId, roleName, entityName, accessRight).
            // We want deterministic ordering: role name → entity name → access right.
            var columnSet = new SortedDictionary<string, SecurityGridColumn>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var principal in principalList)
            {
                foreach (var role in principal.AssignedRoles)
                {
                    foreach (var priv in role.Privileges)
                    {
                        if (!IncludeColumn(priv.EntityName, priv.AccessRight))
                            continue;

                        var col = new SecurityGridColumn(
                            role.RoleId, role.Name,
                            priv.EntityName, priv.AccessRight);

                        if (!columnSet.ContainsKey(col.Key))
                            columnSet[col.Key] = col;
                    }
                }
            }

            // Sort columns: role name asc → entity name asc → access right asc
            var columns = columnSet.Values
                .OrderBy(c => c.RoleName,    StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.EntityName,   StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => (int)c.AccessRight)
                .ToList();

            // ---- 2.  Build rows -------------------------------------------------
            var rows = new List<SecurityGridRow>(principalList.Count);

            foreach (var principal in principalList)
            {
                var row = new SecurityGridRow(principal);

                foreach (var role in principal.AssignedRoles)
                {
                    foreach (var priv in role.Privileges)
                    {
                        if (!IncludeColumn(priv.EntityName, priv.AccessRight))
                            continue;

                        var key = $"{role.RoleId}|{priv.EntityName}|{priv.AccessRight}";

                        // If the same privilege appears in multiple roles take the
                        // highest (widest) depth.
                        PrivilegeDepthLevel existing;
                        if (!row.Cells.TryGetValue(key, out existing)
                            || (int)priv.Depth > (int)existing)
                        {
                            row.Cells[key] = priv.Depth;
                        }
                    }
                }

                rows.Add(row);
            }

            return new SecurityPermissionGrid(columns, rows);
        }

        // ------------------------------------------------------------------ //
        //  Private helpers
        // ------------------------------------------------------------------ //

        private bool IncludeColumn(string entityName, EntityAccessRight accessRight)
        {
            if (EntityFilter != null && EntityFilter.Count > 0
                && !EntityFilter.Contains(entityName))
                return false;

            if (AccessRightFilter != null && AccessRightFilter.Count > 0
                && !AccessRightFilter.Contains(accessRight))
                return false;

            return true;
        }
    }
}
