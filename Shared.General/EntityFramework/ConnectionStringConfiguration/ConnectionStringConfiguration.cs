namespace Shared.General.EntityFramework.ConnectionStringConfiguration
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

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

        /// <summary>
        /// Gets or sets the connection string identifier.
        /// </summary>
        /// <value>
        /// The connection string identifier.
        /// </value>
        [Required]
        [Column("externalIdentifier")]
        public String ExternalIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the connection string type identifier.
        /// </summary>
        /// <value>
        /// The connection string type identifier.
        /// </value>
        public Int32 ConnectionStringTypeId { get; set; }

        /// <summary>
        /// Gets or sets the type of the connection string.
        /// </summary>
        /// <value>
        /// The type of the connection string.
        /// </value>
        [ForeignKey(nameof(ConnectionStringConfiguration.ConnectionStringTypeId))]
        public virtual ConnectionStringType ConnectionStringType { get; set; }


        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        [Required]
        [Column("connectionString")]
        public String ConnectionString { get; set; }
    }
}
