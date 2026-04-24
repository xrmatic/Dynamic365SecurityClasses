namespace Dynamics365.Security.Models
{
    /// <summary>
    /// Identifies whether a security principal is a system user or a team.
    /// </summary>
    public enum PrincipalType
    {
        /// <summary>A Dynamics 365 system user (<c>systemuser</c> entity).</summary>
        User,

        /// <summary>A Dynamics 365 team (<c>team</c> entity).</summary>
        Team
    }
}
