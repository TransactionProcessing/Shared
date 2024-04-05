namespace Shared.IntegrationTesting.Tests
{
    using Ductus.FluentDocker.Services;
    using NLog;
    using Reqnroll;
    using Shared.Logger;
    using Shouldly;

    [Binding]
    public class Setup
    {
        public static IContainerService DatabaseServerContainer;
        public static INetworkService DatabaseServerNetwork;
        public static (String usename, String password) SqlCredentials = ("sa", "thisisalongpassword123!");
        public static (String url, String username, String password) DockerCredentials = ("https://www.docker.com", "stuartferguson", "Sc0tland");
        [BeforeTestRun]
        protected static void GlobalSetup() {
            ShouldlyConfiguration.DefaultTaskTimeout = TimeSpan.FromMinutes(1);

            DockerHelper dockerHelper = new TestDockerHelper();
            dockerHelper.RequiredDockerServices = DockerServices.SqlServer;
            

            NlogLogger logger = new NlogLogger();
            logger.Initialise(LogManager.GetLogger("Reqnroll"), "Reqnroll");
            LogManager.AddHiddenAssembly(typeof(NlogLogger).Assembly);
            dockerHelper.Logger = logger;
            dockerHelper.SqlCredentials = Setup.SqlCredentials;
            dockerHelper.DockerCredentials = Setup.DockerCredentials;
            dockerHelper.SqlServerContainerName = "sharedsqlserver";

            String? isCi = Environment.GetEnvironmentVariable("IsCI");
            dockerHelper.Logger.LogInformation($"IsCI [{isCi}]");
            if (String.Compare(isCi, Boolean.TrueString, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                // override teh SQL Server image
                dockerHelper.Logger.LogInformation("Sql Image overridden");
                dockerHelper.SetImageDetails(ContainerType.SqlServer, ("mssqlserver:2022-ltsc2022", false));
            }

            Setup.DatabaseServerNetwork = dockerHelper.SetupTestNetwork("sharednetwork");
            Setup.DatabaseServerContainer = dockerHelper.SetupSqlServerContainer(Setup.DatabaseServerNetwork).Result;
        }

        //public static String GetConnectionString(String databaseName)
        //{
        //    return $"server={Setup.DatabaseServerContainer.Name};database={databaseName};user id={Setup.SqlCredentials.usename};password={Setup.SqlCredentials.password}";
        //}

        //public static String GetLocalConnectionString(String databaseName)
        //{
        //    Int32 databaseHostPort = Setup.DatabaseServerContainer.ToHostExposedEndpoint("1433/tcp").Port;

        //    return $"server=localhost,{databaseHostPort};database={databaseName};user id={Setup.SqlCredentials.usename};password={Setup.SqlCredentials.password}";
        //}
    }
}
