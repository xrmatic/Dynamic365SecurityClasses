using System;
using System.Collections.Generic;
using System.IO;
using Dynamics365.Security.Models;
using Dynamics365.Security.Services;

namespace Dynamics365.Security.ConsoleApp
{
    /// <summary>
    /// Console application demonstrating usage of the Dynamics365.Security library.
    /// <para>
    /// Run the application with the following command-line arguments:
    /// </para>
    /// <code>
    ///   Dynamics365.Security.ConsoleApp.exe [connectionString] [mode] [options]
    /// </code>
    /// <para>
    /// Where mode is one of: <c>users</c>, <c>teams</c>, <c>all</c>.<br/>
    /// Add <c>--csv</c> to export the grid to a CSV file.<br/>
    /// Add <c>--entities EntityA,EntityB</c> to filter columns to specific entities.<br/>
    /// Add <c>--rights Create,Read,Write</c> to filter columns to specific access rights.
    /// </para>
    /// </summary>
    internal static class Program
    {
        // ------------------------------------------------------------------ //
        //  Entry Point
        // ------------------------------------------------------------------ //

        private static int Main(string[] args)
        {
            Console.WriteLine("=======================================================");
            Console.WriteLine("  Dynamics 365 Security Permission Grid");
            Console.WriteLine("=======================================================");
            Console.WriteLine();

            // ------------------------------------------------------------------
            // Parse arguments
            // ------------------------------------------------------------------
            var options = ParseArgs(args);
            if (options == null)
            {
                PrintUsage();
                return 1;
            }

            // ------------------------------------------------------------------
            // Run the selected demo
            // ------------------------------------------------------------------
            try
            {
                switch (options.Mode)
                {
                    case RunMode.Demo:
                        RunDemo();
                        break;

                    case RunMode.Users:
                        RunLive(options, includeUsers: true,  includeTeams: false);
                        break;

                    case RunMode.Teams:
                        RunLive(options, includeUsers: false, includeTeams: true);
                        break;

                    case RunMode.All:
                        RunLive(options, includeUsers: true,  includeTeams: true);
                        break;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.ResetColor();

                if (options.Verbose)
                    Console.WriteLine(ex.ToString());

                return 2;
            }
        }

        // ------------------------------------------------------------------ //
        //  Demo mode (no live CRM required)
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Demonstrates the grid API using entirely in-memory (stub) data so that
        /// the library can be explored without a live Dynamics 365 environment.
        /// </summary>
        private static void RunDemo()
        {
            Console.WriteLine("Running in DEMO mode (no CRM connection required).");
            Console.WriteLine("In production, replace this stub data with calls to");
            Console.WriteLine("SecurityQueryService.GetUsers() / GetTeams().");
            Console.WriteLine();

            // ---- Build stub security roles ----------------------------------
            var roleSalesRep   = BuildRole("Sales Representative",
                ("Account",  EntityAccessRight.Create,   PrivilegeDepthLevel.Basic),
                ("Account",  EntityAccessRight.Read,     PrivilegeDepthLevel.Local),
                ("Account",  EntityAccessRight.Write,    PrivilegeDepthLevel.Basic),
                ("Contact",  EntityAccessRight.Create,   PrivilegeDepthLevel.Basic),
                ("Contact",  EntityAccessRight.Read,     PrivilegeDepthLevel.Local),
                ("Contact",  EntityAccessRight.Write,    PrivilegeDepthLevel.Basic),
                ("Lead",     EntityAccessRight.Create,   PrivilegeDepthLevel.Basic),
                ("Lead",     EntityAccessRight.Read,     PrivilegeDepthLevel.Local),
                ("Lead",     EntityAccessRight.Delete,   PrivilegeDepthLevel.Basic));

            var roleSalesMgr   = BuildRole("Sales Manager",
                ("Account",  EntityAccessRight.Create,   PrivilegeDepthLevel.Local),
                ("Account",  EntityAccessRight.Read,     PrivilegeDepthLevel.Global),
                ("Account",  EntityAccessRight.Write,    PrivilegeDepthLevel.Local),
                ("Account",  EntityAccessRight.Delete,   PrivilegeDepthLevel.Local),
                ("Contact",  EntityAccessRight.Create,   PrivilegeDepthLevel.Local),
                ("Contact",  EntityAccessRight.Read,     PrivilegeDepthLevel.Global),
                ("Contact",  EntityAccessRight.Write,    PrivilegeDepthLevel.Local),
                ("Contact",  EntityAccessRight.Delete,   PrivilegeDepthLevel.Local),
                ("Lead",     EntityAccessRight.Create,   PrivilegeDepthLevel.Local),
                ("Lead",     EntityAccessRight.Read,     PrivilegeDepthLevel.Global),
                ("Lead",     EntityAccessRight.Delete,   PrivilegeDepthLevel.Local));

            var roleSystemAdmin = BuildRole("System Administrator",
                ("Account",  EntityAccessRight.Create,   PrivilegeDepthLevel.Global),
                ("Account",  EntityAccessRight.Read,     PrivilegeDepthLevel.Global),
                ("Account",  EntityAccessRight.Write,    PrivilegeDepthLevel.Global),
                ("Account",  EntityAccessRight.Delete,   PrivilegeDepthLevel.Global),
                ("Account",  EntityAccessRight.Assign,   PrivilegeDepthLevel.Global),
                ("Contact",  EntityAccessRight.Create,   PrivilegeDepthLevel.Global),
                ("Contact",  EntityAccessRight.Read,     PrivilegeDepthLevel.Global),
                ("Contact",  EntityAccessRight.Write,    PrivilegeDepthLevel.Global),
                ("Contact",  EntityAccessRight.Delete,   PrivilegeDepthLevel.Global),
                ("Lead",     EntityAccessRight.Create,   PrivilegeDepthLevel.Global),
                ("Lead",     EntityAccessRight.Read,     PrivilegeDepthLevel.Global),
                ("Lead",     EntityAccessRight.Delete,   PrivilegeDepthLevel.Global));

            // ---- Build stub principals --------------------------------------
            var principals = new List<SecurityPrincipal>
            {
                new SecurityPrincipal
                {
                    PrincipalId   = Guid.NewGuid(),
                    Name          = "Alice Johnson",
                    PrincipalType = PrincipalType.User,
                    AssignedRoles = new List<SecurityRole> { roleSalesRep }
                },
                new SecurityPrincipal
                {
                    PrincipalId   = Guid.NewGuid(),
                    Name          = "Bob Smith",
                    PrincipalType = PrincipalType.User,
                    AssignedRoles = new List<SecurityRole> { roleSalesRep, roleSalesMgr }
                },
                new SecurityPrincipal
                {
                    PrincipalId   = Guid.NewGuid(),
                    Name          = "Carol White",
                    PrincipalType = PrincipalType.User,
                    AssignedRoles = new List<SecurityRole> { roleSystemAdmin }
                },
                new SecurityPrincipal
                {
                    PrincipalId   = Guid.NewGuid(),
                    Name          = "Sales West Team",
                    PrincipalType = PrincipalType.Team,
                    AssignedRoles = new List<SecurityRole> { roleSalesRep }
                }
            };

            // ---- Example 1: Full grid (all entities, all rights) ------------
            Console.WriteLine("Example 1 – Full permission grid (all entities and access rights)");
            Console.WriteLine("----------------------------------------------------------------");
            var grid = new SecurityGridBuilder().Build(principals);
            PrintGrid(grid);
            Console.WriteLine();

            // ---- Example 2: Filter to Account entity only -------------------
            Console.WriteLine("Example 2 – Filtered to 'Account' entity only");
            Console.WriteLine("----------------------------------------------");
            var builder2 = new SecurityGridBuilder
            {
                EntityFilter = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { "Account" }
            };
            var grid2 = builder2.Build(principals);
            PrintGrid(grid2);
            Console.WriteLine();

            // ---- Example 3: Filter to Create and Read rights only -----------
            Console.WriteLine("Example 3 – Filtered to Create and Read access rights only");
            Console.WriteLine("-----------------------------------------------------------");
            var builder3 = new SecurityGridBuilder
            {
                AccessRightFilter = new HashSet<EntityAccessRight>
                    { EntityAccessRight.Create, EntityAccessRight.Read }
            };
            var grid3 = builder3.Build(principals);
            PrintGrid(grid3);
            Console.WriteLine();

            // ---- Example 4: Export full grid to CSV -------------------------
            Console.WriteLine("Example 4 – Exporting the full grid to CSV");
            Console.WriteLine("------------------------------------------");
            string csv = grid.ToCsv();
            string csvPath = Path.Combine(Path.GetTempPath(), "security_grid_demo.csv");
            File.WriteAllText(csvPath, csv);
            Console.WriteLine($"CSV exported to: {csvPath}");
            Console.WriteLine();

            // ---- Example 5: Programmatic cell access ------------------------
            Console.WriteLine("Example 5 – Programmatic cell access");
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("Querying individual cells from the full grid:");
            foreach (var row in grid.Rows)
            {
                Console.Write($"  {row.Principal.Name,-20} has {row.Cells.Count} granted privileges");

                // Find the first column for Account/Read
                foreach (var col in grid.Columns)
                {
                    if (col.EntityName == "Account" && col.AccessRight == EntityAccessRight.Read)
                    {
                        var depth = grid.GetCell(row, col);
                        Console.Write($"  |  Account Read = {SecurityPermissionGrid.DepthToDisplayString(depth)}");
                        break;
                    }
                }
                Console.WriteLine();
            }
        }

        // ------------------------------------------------------------------ //
        //  Live CRM mode
        // ------------------------------------------------------------------ //

        private static void RunLive(ProgramOptions options,
                                     bool includeUsers,
                                     bool includeTeams)
        {
            Console.WriteLine($"Connecting to Dynamics 365...");

            using (var conn = new CrmConnectionService(options.ConnectionString))
            {
                Console.WriteLine($"Connected to: {conn.OrganizationFriendlyName}");
                Console.WriteLine();

                var queryService = new SecurityQueryService(conn.OrganizationService);
                var principals   = new List<SecurityPrincipal>();

                if (includeUsers)
                {
                    Console.WriteLine("Retrieving users...");
                    var users = queryService.GetUsers();
                    Console.WriteLine($"  → {users.Count} users found.");
                    principals.AddRange(users);
                }

                if (includeTeams)
                {
                    Console.WriteLine("Retrieving teams...");
                    var teams = queryService.GetTeams();
                    Console.WriteLine($"  → {teams.Count} teams found.");
                    principals.AddRange(teams);
                }

                Console.WriteLine();
                Console.WriteLine("Building permission grid...");

                var builder = new SecurityGridBuilder
                {
                    EntityFilter      = options.EntityFilter,
                    AccessRightFilter = options.AccessRightFilter
                };

                var grid = builder.Build(principals);

                Console.WriteLine($"Grid: {grid.Rows.Count} rows × {grid.Columns.Count} columns");
                Console.WriteLine();

                PrintGrid(grid);

                if (options.ExportCsv)
                {
                    string csvPath = options.CsvPath
                        ?? Path.Combine(Directory.GetCurrentDirectory(), "security_grid.csv");
                    File.WriteAllText(csvPath, grid.ToCsv());
                    Console.WriteLine();
                    Console.WriteLine($"CSV exported to: {csvPath}");
                }
            }
        }

        // ------------------------------------------------------------------ //
        //  Console grid renderer
        // ------------------------------------------------------------------ //

        private static void PrintGrid(SecurityPermissionGrid grid)
        {
            if (grid.Rows.Count == 0)
            {
                Console.WriteLine("  (no data)");
                return;
            }

            // Calculate column widths
            const int principalColWidth = 28;
            const int cellWidth         = 5;

            // --- Print column headers (three rows: role / entity / right) ---
            var header1 = new System.Text.StringBuilder(new string(' ', principalColWidth));
            var header2 = new System.Text.StringBuilder(new string(' ', principalColWidth));
            var header3 = new System.Text.StringBuilder(new string(' ', principalColWidth));

            foreach (var col in grid.Columns)
            {
                header1.Append(Truncate(col.RoleName,   cellWidth).PadRight(cellWidth));
                header2.Append(Truncate(col.EntityName, cellWidth).PadRight(cellWidth));
                header3.Append(Truncate(col.AccessRight.ToString(), cellWidth).PadRight(cellWidth));
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(header1);
            Console.WriteLine(header2);
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(header3);
            Console.ResetColor();

            string separator = new string('-', principalColWidth + grid.Columns.Count * cellWidth);
            Console.WriteLine(separator);

            // --- Print data rows ---
            foreach (var row in grid.Rows)
            {
                string principalLabel = $"[{row.Principal.PrincipalType.ToString()[0]}] "
                                       + row.Principal.Name;
                Console.Write(Truncate(principalLabel, principalColWidth)
                              .PadRight(principalColWidth));

                foreach (var col in grid.Columns)
                {
                    string cell = SecurityPermissionGrid.DepthToDisplayString(row.GetCell(col));

                    if (!string.IsNullOrEmpty(cell))
                    {
                        Console.ForegroundColor = DepthColor(row.GetCell(col));
                        Console.Write(cell.PadRight(cellWidth));
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.Write(new string(' ', cellWidth));
                    }
                }

                Console.WriteLine();
            }

            Console.WriteLine(separator);
            Console.WriteLine($"Grid generated at {grid.GeneratedAt:u}");
        }

        // ------------------------------------------------------------------ //
        //  Argument parsing
        // ------------------------------------------------------------------ //

        private static ProgramOptions ParseArgs(string[] args)
        {
            if (args == null || args.Length == 0)
                return new ProgramOptions { Mode = RunMode.Demo };

            // First arg with no leading '--' that is not a known mode word is
            // treated as the connection string.
            var opts = new ProgramOptions();
            var i    = 0;

            // Connection string (first positional)
            if (args.Length > 0 && !args[0].StartsWith("--"))
            {
                opts.ConnectionString = args[0];
                i = 1;
            }

            // Mode (second positional)
            if (args.Length > i && !args[i].StartsWith("--"))
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "users": opts.Mode = RunMode.Users; break;
                    case "teams": opts.Mode = RunMode.Teams; break;
                    case "all":   opts.Mode = RunMode.All;   break;
                    default:
                        Console.WriteLine($"Unknown mode: {args[i]}");
                        return null;
                }
                i++;
            }
            else if (string.IsNullOrEmpty(opts.ConnectionString))
            {
                opts.Mode = RunMode.Demo;
            }
            else
            {
                opts.Mode = RunMode.All; // default to all when connection string supplied
            }

            // Flags
            for (; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--csv":
                        opts.ExportCsv = true;
                        break;

                    case "--csv-path":
                        i++;
                        if (i < args.Length) opts.CsvPath = args[i];
                        opts.ExportCsv = true;
                        break;

                    case "--entities":
                        i++;
                        if (i < args.Length)
                        {
                            opts.EntityFilter = new HashSet<string>(
                                args[i].Split(','),
                                StringComparer.OrdinalIgnoreCase);
                        }
                        break;

                    case "--rights":
                        i++;
                        if (i < args.Length)
                        {
                            opts.AccessRightFilter = new HashSet<EntityAccessRight>();
                            foreach (var s in args[i].Split(','))
                            {
                                EntityAccessRight r;
                                if (Enum.TryParse(s.Trim(), true, out r))
                                    opts.AccessRightFilter.Add(r);
                            }
                        }
                        break;

                    case "--verbose":
                        opts.Verbose = true;
                        break;

                    default:
                        Console.WriteLine($"Unknown option: {args[i]}");
                        return null;
                }
            }

            return opts;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  demo (no args)");
            Console.WriteLine("    Run a self-contained demo with in-memory stub data.");
            Console.WriteLine();
            Console.WriteLine("  <connectionString> [users|teams|all] [options]");
            Console.WriteLine("    Connect to Dynamics 365 and build a permission grid.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --csv                 Export grid to CSV (security_grid.csv)");
            Console.WriteLine("  --csv-path <path>     Export grid to the specified CSV path");
            Console.WriteLine("  --entities A,B,...    Filter columns to these entity names");
            Console.WriteLine("  --rights R1,R2,...    Filter columns to these access rights");
            Console.WriteLine("                        (Create|Read|Write|Delete|Append|AppendTo|Assign|Share)");
            Console.WriteLine("  --verbose             Print full exception details on error");
            Console.WriteLine();
            Console.WriteLine("Connection string examples:");
            Console.WriteLine("  OAuth (online):   \"AuthType=OAuth;Url=https://yourorg.crm.dynamics.com;...\"");
            Console.WriteLine("  Office365 (online): \"AuthType=Office365;Username=user@tenant.com;Password=***;Url=https://...\"");
            Console.WriteLine("  AD (on-premises): \"AuthType=AD;Url=http://server/org;Domain=DOM;Username=u;Password=p\"");
        }

        // ------------------------------------------------------------------ //
        //  Helpers
        // ------------------------------------------------------------------ //

        private static SecurityRole BuildRole(
            string name,
            params (string entity, EntityAccessRight right, PrivilegeDepthLevel depth)[] privs)
        {
            var role = new SecurityRole
            {
                RoleId         = Guid.NewGuid(),
                Name           = name,
                BusinessUnitId = Guid.NewGuid()
            };

            foreach (var (entity, right, depth) in privs)
            {
                role.Privileges.Add(new SecurityRolePrivilege
                {
                    PrivilegeId   = Guid.NewGuid(),
                    PrivilegeName = $"prv{right}{entity}",
                    EntityName    = entity,
                    AccessRight   = right,
                    Depth         = depth
                });
            }

            return role;
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 1) + "…";
        }

        private static ConsoleColor DepthColor(PrivilegeDepthLevel depth)
        {
            switch (depth)
            {
                case PrivilegeDepthLevel.Basic:  return ConsoleColor.White;
                case PrivilegeDepthLevel.Local:  return ConsoleColor.Yellow;
                case PrivilegeDepthLevel.Deep:   return ConsoleColor.Magenta;
                case PrivilegeDepthLevel.Global: return ConsoleColor.Green;
                default:                         return ConsoleColor.Gray;
            }
        }

        // ------------------------------------------------------------------ //
        //  Inner types
        // ------------------------------------------------------------------ //

        private enum RunMode { Demo, Users, Teams, All }

        private sealed class ProgramOptions
        {
            public RunMode                     Mode              { get; set; } = RunMode.Demo;
            public string                      ConnectionString  { get; set; }
            public bool                        ExportCsv         { get; set; }
            public string                      CsvPath           { get; set; }
            public ISet<string>                EntityFilter      { get; set; }
            public ISet<EntityAccessRight>     AccessRightFilter { get; set; }
            public bool                        Verbose           { get; set; }
        }
    }
}
