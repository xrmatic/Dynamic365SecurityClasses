using System;
using System.Collections.Generic;
using Dynamics365.Security.Models;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Dynamics365.Security.Services
{
    /// <summary>
    /// Queries Dynamics 365 for security roles, privileges, system users and teams.
    /// All methods return strongly-typed model objects from the
    /// <c>Dynamics365.Security.Models</c> namespace.
    /// </summary>
    public sealed class SecurityQueryService
    {
        private readonly IOrganizationService _service;

        // Access-right label lookup – used to parse privilege names such as
        // "prvReadAccount" → AccessRight=Read, Entity="Account".
        // "AppendTo" must come before "Append" so the longer prefix matches first.
        private static readonly (string Label, EntityAccessRight Right)[] AccessRightLabels =
        {
            ("Create",   EntityAccessRight.Create),
            ("Read",     EntityAccessRight.Read),
            ("Write",    EntityAccessRight.Write),
            ("Delete",   EntityAccessRight.Delete),
            ("AppendTo", EntityAccessRight.AppendTo),
            ("Append",   EntityAccessRight.Append),
            ("Assign",   EntityAccessRight.Assign),
            ("Share",    EntityAccessRight.Share)
        };

        /// <summary>
        /// Initialises a new <see cref="SecurityQueryService"/>.
        /// </summary>
        /// <param name="organizationService">
        ///   An authenticated <see cref="IOrganizationService"/> instance.
        ///   Use <see cref="CrmConnectionService.OrganizationService"/> to obtain one.
        /// </param>
        public SecurityQueryService(IOrganizationService organizationService)
        {
            _service = organizationService
                ?? throw new ArgumentNullException(nameof(organizationService));
        }

        // ------------------------------------------------------------------ //
        //  Security Roles
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Retrieves all security roles in the organisation, each populated with
        /// its full list of entity-level privileges.
        /// </summary>
        /// <param name="businessUnitId">
        ///   When provided, only roles belonging to this business unit are returned.
        ///   Pass <see langword="null"/> to retrieve roles across all business units.
        /// </param>
        public IList<SecurityRole> GetSecurityRoles(Guid? businessUnitId = null)
        {
            var query = new QueryExpression("role")
            {
                ColumnSet = new ColumnSet("roleid", "name", "businessunitid"),
                NoLock    = true
            };

            if (businessUnitId.HasValue)
            {
                query.Criteria.AddCondition(
                    "businessunitid", ConditionOperator.Equal, businessUnitId.Value);
            }

            query.AddOrder("name", OrderType.Ascending);

            var results = new List<SecurityRole>();
            var collection = _service.RetrieveMultiple(query);

            foreach (var entity in collection.Entities)
            {
                var role = MapRole(entity);
                PopulateRolePrivileges(role);
                results.Add(role);
            }

            return results;
        }

        /// <summary>
        /// Retrieves a single security role by its identifier, fully populated
        /// with its entity-level privileges.
        /// </summary>
        public SecurityRole GetSecurityRole(Guid roleId)
        {
            var entity = _service.Retrieve("role", roleId,
                new ColumnSet("roleid", "name", "businessunitid"));
            var role = MapRole(entity);
            PopulateRolePrivileges(role);
            return role;
        }

        // ------------------------------------------------------------------ //
        //  System Users
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Retrieves all enabled, non-integration system users together with their
        /// directly assigned security roles (privileges populated).
        /// </summary>
        /// <param name="businessUnitId">
        ///   When provided, only users in this business unit are returned.
        /// </param>
        public IList<SecurityPrincipal> GetUsers(Guid? businessUnitId = null)
        {
            var query = new QueryExpression("systemuser")
            {
                ColumnSet = new ColumnSet("systemuserid", "fullname", "businessunitid"),
                NoLock    = true
            };

            // Exclude disabled users and the SYSTEM / INTEGRATION built-in accounts
            query.Criteria.AddCondition("isdisabled",    ConditionOperator.Equal, false);
            query.Criteria.AddCondition("isintegrationuser", ConditionOperator.Equal, false);

            if (businessUnitId.HasValue)
            {
                query.Criteria.AddCondition(
                    "businessunitid", ConditionOperator.Equal, businessUnitId.Value);
            }

            query.AddOrder("fullname", OrderType.Ascending);

            var results = new List<SecurityPrincipal>();
            var collection = _service.RetrieveMultiple(query);

            foreach (var entity in collection.Entities)
            {
                var principal = new SecurityPrincipal
                {
                    PrincipalId   = entity.Id,
                    Name          = entity.GetAttributeValue<string>("fullname") ?? "(no name)",
                    PrincipalType = PrincipalType.User
                };

                principal.AssignedRoles = GetRolesForUser(principal.PrincipalId);
                results.Add(principal);
            }

            return results;
        }

        // ------------------------------------------------------------------ //
        //  Teams
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Retrieves all owner and access teams together with their directly
        /// assigned security roles (privileges populated).
        /// </summary>
        /// <param name="businessUnitId">
        ///   When provided, only teams in this business unit are returned.
        /// </param>
        public IList<SecurityPrincipal> GetTeams(Guid? businessUnitId = null)
        {
            var query = new QueryExpression("team")
            {
                ColumnSet = new ColumnSet("teamid", "name", "businessunitid"),
                NoLock    = true
            };

            if (businessUnitId.HasValue)
            {
                query.Criteria.AddCondition(
                    "businessunitid", ConditionOperator.Equal, businessUnitId.Value);
            }

            query.AddOrder("name", OrderType.Ascending);

            var results = new List<SecurityPrincipal>();
            var collection = _service.RetrieveMultiple(query);

            foreach (var entity in collection.Entities)
            {
                var principal = new SecurityPrincipal
                {
                    PrincipalId   = entity.Id,
                    Name          = entity.GetAttributeValue<string>("name") ?? "(no name)",
                    PrincipalType = PrincipalType.Team
                };

                principal.AssignedRoles = GetRolesForTeam(principal.PrincipalId);
                results.Add(principal);
            }

            return results;
        }

        // ------------------------------------------------------------------ //
        //  Role lookups for a principal
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Returns the security roles directly assigned to a system user,
        /// each fully populated with privileges.
        /// </summary>
        public IList<SecurityRole> GetRolesForUser(Guid userId)
        {
            var query = new QueryExpression("role")
            {
                ColumnSet = new ColumnSet("roleid", "name", "businessunitid"),
                NoLock    = true
            };

            var link = query.AddLink(
                "systemuserroles", "roleid", "roleid", JoinOperator.Inner);
            link.LinkCriteria.AddCondition(
                "systemuserid", ConditionOperator.Equal, userId);

            query.AddOrder("name", OrderType.Ascending);

            return RetrieveRolesWithPrivileges(query);
        }

        /// <summary>
        /// Returns the security roles directly assigned to a team,
        /// each fully populated with privileges.
        /// </summary>
        public IList<SecurityRole> GetRolesForTeam(Guid teamId)
        {
            var query = new QueryExpression("role")
            {
                ColumnSet = new ColumnSet("roleid", "name", "businessunitid"),
                NoLock    = true
            };

            var link = query.AddLink(
                "teamroles", "roleid", "roleid", JoinOperator.Inner);
            link.LinkCriteria.AddCondition(
                "teamid", ConditionOperator.Equal, teamId);

            query.AddOrder("name", OrderType.Ascending);

            return RetrieveRolesWithPrivileges(query);
        }

        // ------------------------------------------------------------------ //
        //  Private helpers
        // ------------------------------------------------------------------ //

        private IList<SecurityRole> RetrieveRolesWithPrivileges(QueryExpression query)
        {
            var results    = new List<SecurityRole>();
            var collection = _service.RetrieveMultiple(query);

            foreach (var entity in collection.Entities)
            {
                var role = MapRole(entity);
                PopulateRolePrivileges(role);
                results.Add(role);
            }

            return results;
        }

        private SecurityRole MapRole(Entity entity)
        {
            return new SecurityRole
            {
                RoleId         = entity.Id,
                Name           = entity.GetAttributeValue<string>("name") ?? "(no name)",
                BusinessUnitId = entity.GetAttributeValue<EntityReference>("businessunitid")?.Id
                                 ?? Guid.Empty
            };
        }

        /// <summary>
        /// Calls <see cref="RetrieveRolePrivilegesRoleRequest"/> to obtain the depth-aware
        /// privilege list for the role, then resolves each privilege's entity name and
        /// access right by querying the <c>privilege</c> entity in bulk.
        /// </summary>
        private void PopulateRolePrivileges(SecurityRole role)
        {
            // Retrieve the role-privilege assignments (with depth info)
            var request  = new RetrieveRolePrivilegesRoleRequest { RoleId = role.RoleId };
            var response = (RetrieveRolePrivilegesRoleResponse)_service.Execute(request);

            if (response.RolePrivileges == null || response.RolePrivileges.Length == 0)
                return;

            // Build a set of privilege IDs we need to look up
            var privilegeIds = new List<Guid>(response.RolePrivileges.Length);
            foreach (var rp in response.RolePrivileges)
                privilegeIds.Add(rp.PrivilegeId);

            // Query the privilege entity in one call to get names
            var privQuery = new QueryExpression("privilege")
            {
                ColumnSet = new ColumnSet("privilegeid", "name"),
                NoLock    = true
            };
            privQuery.Criteria.AddCondition(
                "privilegeid", ConditionOperator.In, privilegeIds.ToArray());

            var privEntities = _service.RetrieveMultiple(privQuery);

            // Build a lookup dictionary: privilegeId → privilege name
            var nameById = new Dictionary<Guid, string>(privEntities.Entities.Count);
            foreach (var pe in privEntities.Entities)
                nameById[pe.Id] = pe.GetAttributeValue<string>("name") ?? string.Empty;

            // Map each role-privilege to a SecurityRolePrivilege
            foreach (var rp in response.RolePrivileges)
            {
                string privilegeName;
                if (!nameById.TryGetValue(rp.PrivilegeId, out privilegeName))
                    continue;

                string entityName;
                EntityAccessRight accessRight;
                if (!TryParsePrivilegeName(privilegeName, out entityName, out accessRight))
                    continue; // skip global / misc privileges

                role.Privileges.Add(new SecurityRolePrivilege
                {
                    PrivilegeId   = rp.PrivilegeId,
                    PrivilegeName = privilegeName,
                    EntityName    = entityName,
                    AccessRight   = accessRight,
                    Depth         = MapDepth(rp.Depth)
                });
            }
        }

        /// <summary>
        /// Parses a CRM privilege name such as <c>prvReadAccount</c> into its
        /// constituent entity name and access right.
        /// Returns <see langword="false"/> for global/misc privileges that do not
        /// follow the naming convention.
        /// </summary>
        internal static bool TryParsePrivilegeName(
            string privilegeName,
            out string entityName,
            out EntityAccessRight accessRight)
        {
            entityName  = null;
            accessRight = EntityAccessRight.None;

            if (string.IsNullOrEmpty(privilegeName) || !privilegeName.StartsWith("prv"))
                return false;

            string suffix = privilegeName.Substring(3); // remove "prv"

            foreach (var (label, right) in AccessRightLabels)
            {
                if (suffix.StartsWith(label, StringComparison.Ordinal))
                {
                    string candidate = suffix.Substring(label.Length);
                    if (string.IsNullOrEmpty(candidate))
                        return false; // "prvRead" with no entity – skip

                    entityName  = candidate;
                    accessRight = right;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Converts the CRM SDK <see cref="Microsoft.Crm.Sdk.Messages.PrivilegeDepth"/>
        /// integer value to a <see cref="PrivilegeDepthLevel"/>.
        /// </summary>
        private static PrivilegeDepthLevel MapDepth(
            Microsoft.Crm.Sdk.Messages.PrivilegeDepth depth)
        {
            switch (depth)
            {
                case Microsoft.Crm.Sdk.Messages.PrivilegeDepth.Basic:  return PrivilegeDepthLevel.Basic;
                case Microsoft.Crm.Sdk.Messages.PrivilegeDepth.Local:  return PrivilegeDepthLevel.Local;
                case Microsoft.Crm.Sdk.Messages.PrivilegeDepth.Deep:   return PrivilegeDepthLevel.Deep;
                case Microsoft.Crm.Sdk.Messages.PrivilegeDepth.Global: return PrivilegeDepthLevel.Global;
                default:                                                return PrivilegeDepthLevel.None;
            }
        }
    }
}
