namespace Shared.EntityFramework.ConnectionStringConfiguration;

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

[Obsolete]
[ExcludeFromCodeCoverage]
public class ConnectionStringType
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    /// <value>
    /// The identifier.
    /// </value>
    [Key]
    public Int32 Id { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    /// <value>
    /// The description.
    /// </value>
    [Required]
    public String Description { get; set; }
}