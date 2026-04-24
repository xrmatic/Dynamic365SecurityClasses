using System;

namespace Dynamics365.Security.Models
{
    /// <summary>
    /// Represents the type of access right on an entity.
    /// Maps directly to the CRM SDK privilege access right bitmask values.
    /// </summary>
    [Flags]
    public enum EntityAccessRight
    {
        /// <summary>No access.</summary>
        None = 0,

        /// <summary>Ability to create new records of the entity.</summary>
        Create = 1,

        /// <summary>Ability to read existing records of the entity.</summary>
        Read = 2,

        /// <summary>Ability to update existing records of the entity.</summary>
        Write = 4,

        /// <summary>Ability to delete existing records of the entity.</summary>
        Delete = 8,

        /// <summary>
        /// Ability to append (associate) another record to a record of this entity
        /// (e.g., attach a note to an account).
        /// </summary>
        Append = 16,

        /// <summary>
        /// Ability to have a record of another entity appended to this entity's records.
        /// Works in conjunction with <see cref="Append"/>.
        /// </summary>
        AppendTo = 32,

        /// <summary>Ability to assign ownership of a record to another user or team.</summary>
        Assign = 64,

        /// <summary>Ability to share a record with another user or team.</summary>
        Share = 128
    }
}
