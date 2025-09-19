namespace Shared.EventStore.EventStore;

using System;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class EventStoreConnectionSettings
{
    #region Public Properties

    public String ConnectionString { get; set; }

    public String IpAddress { get; set; }

    public Int32 TcpPort { get; set; }

    public Int32 HttpPort { get; set; }
        
    public String UserName { get; set; }

    public String Password { get; set; }

    #endregion

    #region Constructors

    public EventStoreConnectionSettings()
    {

    }

    private EventStoreConnectionSettings(String connectionString, Int32 httpPort)
    {
        // Parse Connection String
        this.ParseConnectionString(connectionString);

        this.ConnectionString = connectionString;
        this.HttpPort = httpPort;
    }
        
    #endregion

    #region Public Methods

    #region public static EventStoreConnectionSettings Create()        
    public static EventStoreConnectionSettings Create(String connectionString,
                                                      Int32 httpPort)
    {
        return new EventStoreConnectionSettings(connectionString, httpPort);
    }

    public static EventStoreConnectionSettings Create(String connectionString)
    {
        return new EventStoreConnectionSettings(connectionString, 2113);
    }
    #endregion

    #endregion

    #region Private Methods

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
}