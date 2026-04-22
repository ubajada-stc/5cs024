using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;


namespace WhistleblowerPlatform.Domain.Entities;

/// <summary>
/// Identity user for authentication. Links to Investigator entity for domain data.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// Link to the Investigator domain entity (if this user is an investigator).
    /// Nullable because we might add Admin users later.
    /// </summary>
    public Guid? InvestigatorId { get; set; }

    /// <summary>
    /// Navigation property to Investigator entity.
    /// </summary>
    public virtual Investigator? Investigator { get; set; }

    /// <summary>
    /// Link to the Admin domain entity (if this user is an admin).
    /// </summary>
    public Guid? AdminId { get; set; }

    /// <summary>
    /// Navigation property to Admin entity.
    /// </summary>
    public virtual Admin? Admin { get; set; }
}
