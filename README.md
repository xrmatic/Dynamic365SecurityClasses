# Dynamics365SecurityClasses

A .NET 4.6.2 C# library and console application for querying Dynamics 365 security roles
and rendering a **permission grid** that shows, for each user or team (rows), the exact
privilege depth granted by every security role, broken down to individual entity × access-right
combinations (columns).

---

## Repository Structure

```
src/
├── Dynamics365.Security/               # Class library (net462)
│   ├── Models/
│   │   ├── EntityAccessRight.cs        # Enum  – Create/Read/Write/Delete/Append/AppendTo/Assign/Share
│   │   ├── PrivilegeDepthLevel.cs      # Enum  – None / Basic(User) / Local(BU) / Deep / Global(Org)
│   │   ├── SecurityRolePrivilege.cs    # Entity + access right + depth for one privilege in a role
│   │   ├── SecurityRole.cs             # Role id/name/BU + list of privileges
│   │   ├── PrincipalType.cs            # Enum  – User / Team
│   │   ├── SecurityPrincipal.cs        # User or team + assigned roles
│   │   ├── SecurityGridColumn.cs       # Column header: role × entity × access right
│   │   ├── SecurityGridRow.cs          # Row: principal + per-column depth values
│   │   └── SecurityPermissionGrid.cs   # Full grid + CSV export + console table renderer
│   └── Services/
│       ├── CrmConnectionService.cs     # Wraps CrmServiceClient (CRM SDK auth / connection)
│       ├── SecurityQueryService.cs     # Queries users, teams, roles, privileges from CRM
│       └── SecurityGridBuilder.cs      # Assembles the grid from populated principals
└── Dynamics365.Security.ConsoleApp/    # Console app (net462) with usage examples
    └── Program.cs
```

---

## Grid Layout

| Principal ↓ / Permission → | **Sales Rep \| Account \| Read** | **Sales Rep \| Contact \| Create** | **Sales Mgr \| Account \| Delete** | … |
|---|---|---|---|---|
| Alice Johnson (User) | BU | User | | … |
| Bob Smith (User) | Org | BU | Local | … |
| Sales West (Team) | User | | | … |

* **Rows** = users and/or teams (principals) — vertical axis.  
* **Columns** = security role name → entity name → access right — horizontal axis.  
* **Cell value** = depth level granted: `User` (Basic), `BU` (Local), `Deep`, `Org` (Global), or empty (no access).  

---

## Prerequisites

* .NET Framework 4.6.2 (Windows) or compatible build toolchain  
* Dynamics 365 / Power Platform environment  
* An app registration or user account with the *Read Security Role* privilege  

---

## Building

```bash
dotnet restore
dotnet build
```

---

## Console App Usage

### Demo mode (no CRM connection required)

Run the executable with no arguments to see a fully self-contained demo using in-memory stub
data:

```cmd
Dynamics365.Security.ConsoleApp.exe
```

Output shows five examples:
1. Full grid (all entities, all access rights)  
2. Grid filtered to the `Account` entity only  
3. Grid filtered to `Create` and `Read` access rights only  
4. CSV export  
5. Programmatic cell access  

---

### Live CRM mode

```
Dynamics365.Security.ConsoleApp.exe <connectionString> [users|teams|all] [options]
```

| Argument | Description |
|---|---|
| `<connectionString>` | CRM SDK connection string (see examples below) |
| `users` | Include system users (default when mode omitted) |
| `teams` | Include teams |
| `all` | Include both users and teams |
| `--csv` | Export grid to `security_grid.csv` in the current directory |
| `--csv-path <path>` | Export grid to the specified path |
| `--entities A,B,...` | Limit columns to these entity schema names |
| `--rights R1,R2,...` | Limit columns to these access rights |
| `--verbose` | Print full exception details on error |

#### Connection string examples

**OAuth / modern auth (Dynamics 365 online)**
```
"AuthType=OAuth;Url=https://yourorg.crm.dynamics.com;AppId=<your-app-id>;RedirectUri=app://58145B91-0C36-4500-8554-080854F2AC97;LoginPrompt=Auto"
```

**Username + password (Dynamics 365 online – legacy)**
```
"AuthType=Office365;Username=admin@yourtenant.onmicrosoft.com;Password=yourPassword;Url=https://yourorg.crm.dynamics.com"
```

**Active Directory (on-premises)**
```
"AuthType=AD;Url=http://yourserver/yourorg;Domain=YOURDOMAIN;Username=yourUser;Password=yourPassword"
```

#### Example commands

```cmd
# All users and teams, export to CSV, filter to Account + Contact entities
Dynamics365.Security.ConsoleApp.exe "AuthType=OAuth;..." all --entities Account,Contact --csv

# Only teams, filter to Read + Write rights
Dynamics365.Security.ConsoleApp.exe "AuthType=OAuth;..." teams --rights Read,Write

# All principals with full grid, export to a custom CSV path
Dynamics365.Security.ConsoleApp.exe "AuthType=OAuth;..." all --csv-path C:\Reports\roles.csv
```

---

## Using the Library Directly

```csharp
using Dynamics365.Security.Models;
using Dynamics365.Security.Services;

// 1. Connect
using (var conn = new CrmConnectionService(
    "AuthType=OAuth;Url=https://yourorg.crm.dynamics.com;..."))
{
    var queryService = new SecurityQueryService(conn.OrganizationService);

    // 2. Retrieve principals with their assigned roles and privileges
    var users = queryService.GetUsers();          // all enabled users
    var teams = queryService.GetTeams();          // all teams
    var principals = users.Concat(teams).ToList();

    // 3. Build the grid
    var builder = new SecurityGridBuilder
    {
        // Optional filters
        EntityFilter      = new HashSet<string> { "Account", "Contact", "Lead" },
        AccessRightFilter = new HashSet<EntityAccessRight>
            { EntityAccessRight.Create, EntityAccessRight.Read, EntityAccessRight.Write }
    };

    SecurityPermissionGrid grid = builder.Build(principals);

    // 4a. Render as a console table
    grid.WriteTable(Console.Out);

    // 4b. Export to CSV
    File.WriteAllText("security_grid.csv", grid.ToCsv());

    // 4c. Programmatic access
    foreach (SecurityGridRow row in grid.Rows)
    {
        foreach (SecurityGridColumn col in grid.Columns)
        {
            PrivilegeDepthLevel depth = grid.GetCell(row, col);
            Console.WriteLine($"{row.Principal.Name} | {col} | {depth}");
        }
    }
}
```

---

## Key Classes

### `SecurityPermissionGrid`
Top-level grid model. Exposes `Columns`, `Rows`, `GetCell(row, col)`, `WriteTable(writer)`
and `ToCsv()`.

### `SecurityGridColumn`
Identifies one column as a combination of role id/name + entity schema name + access right.
The `Key` property (`{roleId}|{entityName}|{accessRight}`) is used for fast cell lookup.

### `SecurityGridRow`
One row (principal). The `Cells` dictionary maps column keys to `PrivilegeDepthLevel` values.
`GetCell(column)` returns `PrivilegeDepthLevel.None` for any column not in the dictionary.

### `SecurityQueryService`
Wraps the CRM SDK to retrieve:
* `GetUsers()` / `GetTeams()` — principals with their assigned roles and full privilege lists.
* `GetRolesForUser(userId)` / `GetRolesForTeam(teamId)` — roles for a specific principal.
* `GetSecurityRoles()` / `GetSecurityRole(roleId)` — all or specific roles with privileges.

Internally calls `RetrieveRolePrivilegesRoleRequest` (CRM SDK) for accurate depth information.

### `SecurityGridBuilder`
Builds the grid from a collection of `SecurityPrincipal` objects. Supports optional
`EntityFilter` and `AccessRightFilter` to reduce the column count.

### `CrmConnectionService`
Thin wrapper around `CrmServiceClient` from the CRM SDK's XrmTooling package.
Implements `IDisposable`; exposes `OrganizationService` for use with `SecurityQueryService`.
