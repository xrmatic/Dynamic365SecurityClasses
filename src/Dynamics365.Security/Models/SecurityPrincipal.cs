using System;
using System.Collections.Generic;

namespace Dynamics365.Security.Models
{
    /// <summary>
    /// Represents a security principal (user or team) together with the security roles
    /// that have been directly assigned to it.
    /// </summary>
    public sealed class SecurityPrincipal
    {
        /// <summary>
        /// The unique identifier of the <c>systemuser</c> or <c>team</c> record.
        /// </summary>
        public Guid PrincipalId { get; set; }

        /// <summary>
        /// The display name of the user or team.
        /// For users this is the full name; for teams it is the team name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>Whether this principal represents a user or a team.</summary>
        public PrincipalType PrincipalType { get; set; }

        /// <summary>
        /// The security roles directly assigned to this principal.
        /// Note: users may also inherit roles through team membership – those are
        /// not included here unless explicitly resolved by the caller.
        /// </summary>
        public IList<SecurityRole> AssignedRoles { get; set; }
            = new List<SecurityRole>();

        /// <inheritdoc />
        public override string ToString() =>
            $"[{PrincipalType}] {Name} ({PrincipalId})";
    }
}
