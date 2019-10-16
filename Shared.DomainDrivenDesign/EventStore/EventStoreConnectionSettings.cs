namespace Shared.DomainDrivenDesign.EventStore
{
    using System;

    public class EventStoreConnectionSettings
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        public String ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the ip address.
        /// </summary>
        /// <value>
        /// The ip address.
        /// </value>
        public String IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the tcp port.
        /// </summary>
        /// <value>
        /// The tcp port.
        /// </value>
        public Int32 TcpPort { get; set; }

        /// <summary>
        /// Gets or sets the HTTP port.
        /// </summary>
        /// <value>
        /// The HTTP port.
        /// </value>
        public Int32 HttpPort { get; set; }

        /// <summary>
        /// Gets or sets the name of the connection.
        /// </summary>
        /// <value>
        /// The name of the connection.
        /// </value>
        public String ConnectionName { get; set; }

        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        /// <value>
        /// The name of the user.
        /// </value>
        public String UserName { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>
        /// The password.
        /// </value>
        public String Password { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStoreConnectionSettings"/> class.
        /// </summary>
        public EventStoreConnectionSettings()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStoreConnectionSettings"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="connectionName">Name of the connection.</param>
        /// <param name="httpPort">The HTTP port.</param>
        private EventStoreConnectionSettings(String connectionString, String connectionName = null, Int32 httpPort=2113)
        {
            // Parse Connection String
            this.ParseConnectionString(connectionString);

            this.ConnectionString = connectionString;
            this.ConnectionName = connectionName;
            this.HttpPort = httpPort;
        }

        #endregion

        #region Public Methods

        #region public static EventStoreConnectionSettings Create()        
        /// <summary>
        /// Creates the specified connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="connectionName">Name of the connection.</param>
        /// <param name="httpPort">The HTTP port.</param>
        /// <returns></returns>
        public static EventStoreConnectionSettings Create(String connectionString, String connectionName = null,
            Int32 httpPort = 2113)
        {
            return new EventStoreConnectionSettings(connectionString,connectionName, httpPort);
        }
        #endregion

        #endregion

        #region Private Methods

        #region private void ParseConnectionString(String connectionString)        
        /// <summary>
        /// Parses the connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        private void ParseConnectionString(String connectionString)
        {
            // Check for the presence of the @ symbol as this is the start of the address
            Int32 addressStart = connectionString.IndexOf("@", StringComparison.Ordinal);

            if (addressStart <= 0)
            {
                // Invalid Connection String, no address
                throw new ArgumentException("Invalid Connection String - no address separator (@) present");
            }

            // Get the port seperator, this is the end of the address
            Int32 portStart = connectionString.IndexOf(":", addressStart, StringComparison.Ordinal);

            if (portStart <= 0)
            {
                // Invalid Connection String, no port
                throw new ArgumentException("Invalid Connection String - no port separator (:) present");
            }

            Int32 portEnd = connectionString.IndexOf(";", portStart, StringComparison.Ordinal);

            if (portEnd <= 0)
            {
                // Invalid Connection String, invalid format
                throw new ArgumentException("Invalid Connection String - no section end separator (;) present after the port");
            }
            
            // Get the port
            var portValue = connectionString.Substring(portStart + 1, (portEnd - portStart) - 1);

            // Check the separator is a valid one, not another section separator
            if (portValue.Length > 5)
            {
                // Invalid Connection String, invalid format
                throw new ArgumentException("Invalid Connection String - no valid section end separator (;) present after the port");
            }

            // Now get the address
            this.IpAddress = connectionString.Substring(addressStart + 1, (portStart - addressStart)-1);
            this.TcpPort = Int32.Parse(portValue);

            // Now handle the user credentials
            Int32 userCredentialsStart = connectionString.IndexOf("//", StringComparison.Ordinal);

            if (userCredentialsStart <= 0)
            {
                // Invalid Connection String
                throw new ArgumentException("Invalid Connection String - no protocol/ user credentials separator (//) present");
            }

            // Get the credentials
            String userDetails =
                connectionString.Substring(userCredentialsStart + 2, (addressStart - userCredentialsStart)-2);

            String[] splitUserDetails = userDetails.Split(":");

            if (splitUserDetails.Length != 2)
            {
                // Invalid Connection String
                throw new ArgumentException("Invalid Connection String - user credentials require username and password");
            }

            this.UserName = splitUserDetails[0];
            this.Password = splitUserDetails[1];
        }
        #endregion

        #endregion
    }
}