using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Dynamics365.Security.Models
{
    /// <summary>
    /// The top-level grid that shows, for each user/team (row), the depth level granted
    /// for every role × entity × access-right combination (column).
    /// <para>
    /// Rows  = principals (users / teams) — displayed vertically.<br/>
    /// Columns = security role broken down to individual entity permission levels
    ///           — displayed horizontally.
    /// </para>
    /// </summary>
    public sealed class SecurityPermissionGrid
    {
        /// <summary>All column definitions, in display order.</summary>
        public IReadOnlyList<SecurityGridColumn> Columns { get; }

        /// <summary>All row definitions, in display order.</summary>
        public IReadOnlyList<SecurityGridRow> Rows { get; }

        /// <summary>
        /// The UTC timestamp at which this grid was built.
        /// </summary>
        public DateTime GeneratedAt { get; } = DateTime.UtcNow;

        /// <summary>Initialises a new <see cref="SecurityPermissionGrid"/>.</summary>
        public SecurityPermissionGrid(
            IEnumerable<SecurityGridColumn> columns,
            IEnumerable<SecurityGridRow> rows)
        {
            Columns = (columns ?? throw new ArgumentNullException(nameof(columns)))
                          .ToList().AsReadOnly();
            Rows    = (rows    ?? throw new ArgumentNullException(nameof(rows)))
                          .ToList().AsReadOnly();
        }

        /// <summary>
        /// Returns the <see cref="PrivilegeDepthLevel"/> for the given row and column.
        /// Returns <see cref="PrivilegeDepthLevel.None"/> when no access is granted.
        /// </summary>
        public PrivilegeDepthLevel GetCell(SecurityGridRow row, SecurityGridColumn column)
        {
            if (row    == null) throw new ArgumentNullException(nameof(row));
            if (column == null) throw new ArgumentNullException(nameof(column));
            return row.GetCell(column);
        }

        // ------------------------------------------------------------------ //
        //  Display helpers
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Returns the depth level as a compact display string used when rendering
        /// the grid in the console.
        /// </summary>
        public static string DepthToDisplayString(PrivilegeDepthLevel depth)
        {
            switch (depth)
            {
                case PrivilegeDepthLevel.Basic:  return "User";
                case PrivilegeDepthLevel.Local:  return "BU";
                case PrivilegeDepthLevel.Deep:   return "Deep";
                case PrivilegeDepthLevel.Global: return "Org";
                default:                         return "";
            }
        }

        /// <summary>
        /// Writes the grid as a pipe-separated table to <paramref name="writer"/>.
        /// </summary>
        public void WriteTable(TextWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            // Header row
            var header = new StringBuilder("Principal");
            foreach (var col in Columns)
                header.Append($" | {col.RoleName}:{col.EntityName}:{col.AccessRight}");
            writer.WriteLine(header);

            // Separator
            writer.WriteLine(new string('-', Math.Min(header.Length, 200)));

            // Data rows
            foreach (var row in Rows)
            {
                var line = new StringBuilder(row.Principal.Name);
                foreach (var col in Columns)
                    line.Append($" | {DepthToDisplayString(row.GetCell(col)),4}");
                writer.WriteLine(line);
            }
        }

        /// <summary>
        /// Exports the grid to CSV format and returns the content as a string.
        /// </summary>
        public string ToCsv()
        {
            var sb = new StringBuilder();

            // Header
            sb.Append("\"PrincipalType\",\"PrincipalName\"");
            foreach (var col in Columns)
                sb.Append($",\"{EscapeCsv(col.RoleName)} | {EscapeCsv(col.EntityName)} | {col.AccessRight}\"");
            sb.AppendLine();

            // Data rows
            foreach (var row in Rows)
            {
                sb.Append($"\"{row.Principal.PrincipalType}\",\"{EscapeCsv(row.Principal.Name)}\"");
                foreach (var col in Columns)
                    sb.Append($",\"{DepthToDisplayString(row.GetCell(col))}\"");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string EscapeCsv(string value)
            => value?.Replace("\"", "\"\"") ?? string.Empty;
    }
}
