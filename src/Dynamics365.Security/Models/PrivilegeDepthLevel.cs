namespace Dynamics365.Security.Models
{
    /// <summary>
    /// Represents the depth (scope) to which a privilege is granted within a security role.
    /// Maps to the CRM SDK <c>Microsoft.Crm.Sdk.Messages.PrivilegeDepth</c> enumeration.
    /// </summary>
    public enum PrivilegeDepthLevel
    {
        /// <summary>
        /// The privilege is not granted at any level.
        /// </summary>
        None = -1,

        /// <summary>
        /// Basic (User) scope: access is limited to records owned by the user
        /// or shared with the user. Maps to <c>PrivilegeDepth.Basic</c> (value 0).
        /// </summary>
        Basic = 0,

        /// <summary>
        /// Local (Business Unit) scope: access is limited to records owned by
        /// the user or by other users in the same business unit.
        /// Maps to <c>PrivilegeDepth.Local</c> (value 1).
        /// </summary>
        Local = 1,

        /// <summary>
        /// Deep (Parent: Child Business Units) scope: access covers the user's
        /// own business unit and all child business units.
        /// Maps to <c>PrivilegeDepth.Deep</c> (value 2).
        /// </summary>
        Deep = 2,

        /// <summary>
        /// Global (Organization) scope: access covers all records in the organisation,
        /// regardless of business unit. Maps to <c>PrivilegeDepth.Global</c> (value 3).
        /// </summary>
        Global = 3
    }
}
