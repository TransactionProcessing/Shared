using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Shared.EntityFramework.ConnectionStringConfiguration;

using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

[Obsolete]
[ExcludeFromCodeCoverage]
public class ConnectionStringConfiguration
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    /// <value>
    /// The identifier.
    /// </value>
    [Key]
    public Guid Id { get; set; }

    [Required]
    [Column("externalIdentifier")]
    public String ExternalIdentifier { get; set; }
        
    public String ConnectionStringIdentifier { get; set; }
        
    [Required]
    [Column("connectionString")]
    public String ConnectionString { get; set; }
}