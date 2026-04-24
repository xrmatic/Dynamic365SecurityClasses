using System;
using System.Collections.Generic;

namespace Dynamics365.Security.Models
{
    /// <summary>
    /// Represents a single row in the <see cref="SecurityPermissionGrid"/>.
    /// Each row corresponds to one <see cref="SecurityPrincipal"/> (user or team).
    /// </summary>
    public sealed class SecurityGridRow
    {
        /// <summary>The principal (user or team) that this row describes.</summary>
        public SecurityPrincipal Principal { get; }

        /// <summary>
        /// Cell values for this row, keyed by <see cref="SecurityGridColumn.Key"/>.
        /// A missing key means the principal has no privilege for that column.
        /// </summary>
        public IDictionary<string, PrivilegeDepthLevel> Cells { get; }
            = new Dictionary<string, PrivilegeDepthLevel>(StringComparer.Ordinal);

        /// <summary>Initialises a new <see cref="SecurityGridRow"/>.</summary>
        public SecurityGridRow(SecurityPrincipal principal)
        {
            Principal = principal ?? throw new ArgumentNullException(nameof(principal));
        }

        /// <summary>
        /// Returns the <see cref="PrivilegeDepthLevel"/> for the specified column,
        /// or <see cref="PrivilegeDepthLevel.None"/> if the principal has no access.
        /// </summary>
        public PrivilegeDepthLevel GetCell(SecurityGridColumn column)
        {
            if (column == null) throw new ArgumentNullException(nameof(column));

            PrivilegeDepthLevel depth;
            return Cells.TryGetValue(column.Key, out depth) ? depth : PrivilegeDepthLevel.None;
        }

        /// <inheritdoc />
        public override string ToString() => Principal.ToString();
    }
}
