namespace Shared.IntegrationTesting
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Ductus.FluentDocker.Builders;
    using Ductus.FluentDocker.Model.Builders;
    using Ductus.FluentDocker.Services;
    using Ductus.FluentDocker.Services.Extensions;
    using Logger;
    using Microsoft.Data.SqlClient;

    /// <summary>
    /// 
    /// </summary>
    public abstract class DockerHelper
    {
        #region Fields

        protected String CallbackHandlerContainerName;

        /// <summary>
        /// The client details
        /// </summary>
        protected (String clientId, String clientSecret) ClientDetails;

        /// <summary>
        /// The docker credentials
        /// </summary>
        protected (String URL, String UserName, String Password)? DockerCredentials;

        /// <summary>
        /// The estate management container name
        /// </summary>
        protected String EstateManagementContainerName;

        /// <summary>
        /// The estate reporting container name
        /// </summary>
        protected String EstateReportingContainerName;

        /// <summary>
        /// The event store container name
        /// </summary>
        protected String EventStoreContainerName;

        /// <summary>
        /// The host trace folder
        /// </summary>
        protected String HostTraceFolder;

        /// <summary>
        /// The logger
        /// </summary>
        protected ILogger Logger;

        /// <summary>
        /// The messaging service container name
        /// </summary>
        protected String MessagingServiceContainerName;

        /// <summary>
        /// The security service container name
        /// </summary>
        protected String SecurityServiceContainerName;

        /// <summary>
        /// The SQL server details
        /// </summary>
        protected (String sqlServerContainerName, String sqlServerUserName, String sqlServerPassword) SqlServerDetails;

        /// <summary>
        /// The test host container name
        /// </summary>
        protected String TestHostContainerName;

        /// <summary>
        /// The transaction processor acl container name
        /// </summary>
        protected String TransactionProcessorACLContainerName;

        /// <summary>
        /// The transaction processor container name
        /// </summary>
        protected String TransactionProcessorContainerName;

        /// <summary>
        /// The voucher management acl container name
        /// </summary>
        protected String VoucherManagementACLContainerName;

        /// <summary>
        /// The voucher management container name
        /// </summary>
        protected String VoucherManagementContainerName;

        #endregion

        #region Methods

        public IContainerService SetupCallbackHandlerContainer(String imageName,
                                                               List<INetworkService> networkServices,
                                                               Boolean forceLatestImage = false,
                                                               List<String> additionalEnvironmentVariables = null)
        {
            this.Logger.LogInformation("About to Start Callback Handler Container");

            List<String> environmentVariables = new List<String>();
            environmentVariables.Add($"EventStoreSettings:ConnectionString={this.GenerateEventStoreConnectionString()}");

            if (additionalEnvironmentVariables != null)
            {
                environmentVariables.AddRange(additionalEnvironmentVariables);
            }

            ContainerBuilder callbackHandlerContainer = new Builder().UseContainer().WithName(this.CallbackHandlerContainerName)
                                                                     .WithEnvironment(environmentVariables.ToArray()).UseImage(imageName, forceLatestImage)
                                                                     .ExposePort(DockerHelper.CallbackHandlerDockerPort).UseNetwork(networkServices.ToArray());

            callbackHandlerContainer = this.MountHostFolder(callbackHandlerContainer);
            callbackHandlerContainer = this.SetDockerCredentials(callbackHandlerContainer);

            // Now build and return the container                
            IContainerService builtContainer = callbackHandlerContainer.Build().Start().WaitForPort($"{DockerHelper.CallbackHandlerDockerPort}/tcp", 30000);

            this.Logger.LogInformation("Callback Handler Container Started");

            return builtContainer;
        }

        /// <summary>
        /// Setups the estate management container.
        /// </summary>
        /// <param name="imageName">Name of the image.</param>
        /// <param name="networkServices">The network services.</param>
        /// <param name="forceLatestImage">if set to <c>true</c> [force latest image].</param>
        /// <param name="securityServicePort">The security service port.</param>
        /// <param name="additionalEnvironmentVariables">The additional environment variables.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        public virtual IContainerService SetupEstateManagementContainer(String imageName,
                                                                        List<INetworkService> networkServices,
                                                                        Boolean forceLatestImage = false,
                                                                        Int32 securityServicePort = DockerHelper.SecurityServiceDockerPort,
                                                                        List<String> additionalEnvironmentVariables = null)
        {
            this.Trace("About to Start Estate Management Container");

            List<String> environmentVariables = new List<String>();
            environmentVariables.Add($"EventStoreSettings:ConnectionString={this.GenerateEventStoreConnectionString()}");
            environmentVariables.Add($"AppSettings:SecurityService=https://{this.SecurityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"SecurityConfiguration:Authority=https://{this.SecurityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"urls=http://*:{DockerHelper.EstateManagementDockerPort}");
            environmentVariables
                .Add($"ConnectionStrings:EstateReportingReadModel=\"server={this.SqlServerDetails.sqlServerContainerName};user id={this.SqlServerDetails.sqlServerUserName};password={this.SqlServerDetails.sqlServerPassword};database=EstateReportingReadModel\"");

            if (additionalEnvironmentVariables != null)
            {
                environmentVariables.AddRange(additionalEnvironmentVariables);
            }

            ContainerBuilder estateManagementContainer = new Builder().UseContainer().WithName(this.EstateManagementContainerName)
                                                                      .WithEnvironment(environmentVariables.ToArray()).UseImage(imageName, forceLatestImage)
                                                                      .ExposePort(DockerHelper.EstateManagementDockerPort).UseNetwork(networkServices.ToArray());

            estateManagementContainer = this.MountHostFolder(estateManagementContainer);
            estateManagementContainer = this.SetDockerCredentials(estateManagementContainer);

            // Now build and return the container                
            IContainerService builtContainer = estateManagementContainer.Build().Start().WaitForPort($"{DockerHelper.EstateManagementDockerPort}/tcp", 30000);

            this.Trace("Estate Management Container Started");

            return builtContainer;
        }

        /// <summary>
        /// Setups the estate reporting container.
        /// </summary>
        /// <param name="imageName">Name of the image.</param>
        /// <param name="networkServices">The network services.</param>
        /// <param name="forceLatestImage">if set to <c>true</c> [force latest image].</param>
        /// <param name="securityServicePort">The security service port.</param>
        /// <param name="additionalEnvironmentVariables">The additional environment variables.</param>
        /// <returns></returns>
        public virtual IContainerService SetupEstateReportingContainer(String imageName,
                                                                       List<INetworkService> networkServices,
                                                                       Boolean forceLatestImage = false,
                                                                       Int32 securityServicePort = DockerHelper.SecurityServiceDockerPort,
                                                                       List<String> additionalEnvironmentVariables = null)
        {
            this.Trace("About to Start Estate Reporting Container");

            List<String> environmentVariables = new List<String>();
            environmentVariables.Add($"AppSettings:SecurityService=https://{this.SecurityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"EventStoreSettings:ConnectionString={this.GenerateEventStoreConnectionString()}");
            environmentVariables.Add($"SecurityConfiguration:Authority=https://{this.SecurityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"urls=http://*:{DockerHelper.EstateReportingDockerPort}");
            environmentVariables
                .Add($"ConnectionStrings:EstateReportingReadModel=\"server={this.SqlServerDetails.sqlServerContainerName};user id={this.SqlServerDetails.sqlServerUserName};password={this.SqlServerDetails.sqlServerPassword};database=EstateReportingReadModel\"");

            if (additionalEnvironmentVariables != null)
            {
                environmentVariables.AddRange(additionalEnvironmentVariables);
            }

            ContainerBuilder estateReportingContainer = new Builder().UseContainer().WithName(this.EstateReportingContainerName)
                                                                     .WithEnvironment(environmentVariables.ToArray()).UseImage(imageName, forceLatestImage)
                                                                     .ExposePort(DockerHelper.EstateReportingDockerPort).UseNetwork(networkServices.ToArray());

            estateReportingContainer = this.MountHostFolder(estateReportingContainer);
            estateReportingContainer = this.SetDockerCredentials(estateReportingContainer);

            // Now build and return the container                
            IContainerService builtContainer = estateReportingContainer.Build().Start().WaitForPort($"{DockerHelper.EstateReportingDockerPort}/tcp", 30000);

            this.Trace("Estate Reporting Container Started");

            return builtContainer;
        }

        /// <summary>
        /// Setups the event store container.
        /// </summary>
        /// <param name="imageName">Name of the image.</param>
        /// <param name="networkService">The network service.</param>
        /// <param name="forceLatestImage">if set to <c>true</c> [force latest image].</param>
        /// <returns></returns>
        public virtual IContainerService SetupEventStoreContainer(String imageName,
                                                                  INetworkService networkService,
                                                                  Boolean forceLatestImage = false)
        {
            this.Trace("About to Start Event Store Container");

            List<String> environmentVariables = new List<String>();
            environmentVariables.Add("EVENTSTORE_RUN_PROJECTIONS=all");
            environmentVariables.Add("EVENTSTORE_START_STANDARD_PROJECTIONS=true");
            environmentVariables.Add("EVENTSTORE_INSECURE=true");
            environmentVariables.Add("EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP=true");
            environmentVariables.Add("EVENTSTORE_ENABLE_EXTERNAL_TCP=true");

            var eventStoreContainerBuilder = new Builder().UseContainer().UseImage(imageName, forceLatestImage).ExposePort(DockerHelper.EventStoreHttpDockerPort)
                                                          .ExposePort(DockerHelper.EventStoreTcpDockerPort).WithName(this.EventStoreContainerName)
                                                          .WithEnvironment(environmentVariables.ToArray()).UseNetwork(networkService);

            eventStoreContainerBuilder = this.MountHostFolder(eventStoreContainerBuilder, "/var/log/eventstore");

            IContainerService eventStoreContainer = eventStoreContainerBuilder.Build().Start().WaitForPort("2113/tcp", 30000);

            this.Trace("Event Store Container Started");

            return eventStoreContainer;
        }

        /// <summary>
        /// Setups the messaging service container.
        /// </summary>
        /// <param name="imageName">Name of the image.</param>
        /// <param name="networkServices">The network services.</param>
        /// <param name="forceLatestImage">if set to <c>true</c> [force latest image].</param>
        /// <param name="securityServicePort">The security service port.</param>
        /// <param name="additionalEnvironmentVariables">The additional environment variables.</param>
        /// <returns></returns>
        public virtual IContainerService SetupMessagingServiceContainer(String imageName,
                                                                        List<INetworkService> networkServices,
                                                                        Boolean forceLatestImage = false,
                                                                        Int32 securityServicePort = DockerHelper.SecurityServiceDockerPort,
                                                                        List<String> additionalEnvironmentVariables = null)
        {
            this.Trace("About to Start Messaging Service Container");

            List<String> environmentVariables = new List<String>();
            environmentVariables.Add($"EventStoreSettings:ConnectionString={this.GenerateEventStoreConnectionString()}");
            environmentVariables.Add($"AppSettings:SecurityService=https://{this.SecurityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"SecurityConfiguration:Authority=https://{this.SecurityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"urls=http://*:{DockerHelper.MessagingServiceDockerPort}");
            environmentVariables.Add("AppSettings:EmailProxy=Integration");
            environmentVariables.Add("AppSettings:SMSProxy=Integration");

            if (additionalEnvironmentVariables != null)
            {
                environmentVariables.AddRange(additionalEnvironmentVariables);
            }

            ContainerBuilder messagingServiceContainer = new Builder().UseContainer().WithName(this.MessagingServiceContainerName)
                                                                      .WithEnvironment(environmentVariables.ToArray()).UseImage(imageName, forceLatestImage)
                                                                      .ExposePort(DockerHelper.MessagingServiceDockerPort).UseNetwork(networkServices.ToArray());

            messagingServiceContainer = this.MountHostFolder(messagingServiceContainer);
            messagingServiceContainer = this.SetDockerCredentials(messagingServiceContainer);

            // Now build and return the container                
            IContainerService builtContainer = messagingServiceContainer.Build().Start().WaitForPort($"{DockerHelper.MessagingServiceDockerPort}/tcp", 30000);

            this.Trace("Messaging Service Container Started");

            return builtContainer;
        }

        /// <summary>
        /// Setups the security service container.
        /// </summary>
        /// <param name="imageName">Name of the image.</param>
        /// <param name="networkService">The network service.</param>
        /// <param name="forceLatestImage">if set to <c>true</c> [force latest image].</param>
        /// <param name="additionalEnvironmentVariables">The additional environment variables.</param>
        /// <returns></returns>
        public virtual IContainerService SetupSecurityServiceContainer(String imageName,
                                                                       INetworkService networkService,
                                                                       Boolean forceLatestImage = false,
                                                                       List<String> additionalEnvironmentVariables = null)
        {
            this.Trace("About to Start Security Container");

            List<String> environmentVariables = new List<String>();
            environmentVariables.Add($"ServiceOptions:PublicOrigin=https://{this.SecurityServiceContainerName}:{DockerHelper.SecurityServiceDockerPort}");
            environmentVariables.Add($"ServiceOptions:IssuerUrl=https://{this.SecurityServiceContainerName}:{DockerHelper.SecurityServiceDockerPort}");
            environmentVariables.Add("ASPNETCORE_ENVIRONMENT=IntegrationTest");
            environmentVariables.Add("urls=https://*:5001");

            if (additionalEnvironmentVariables != null)
            {
                environmentVariables.AddRange(additionalEnvironmentVariables);
            }

            ContainerBuilder securityServiceContainer = new Builder().UseContainer().WithName(this.SecurityServiceContainerName)
                                                                     .WithEnvironment(environmentVariables.ToArray()).UseImage(imageName, forceLatestImage)
                                                                     .ExposePort(DockerHelper.SecurityServiceDockerPort).UseNetwork(new List<INetworkService>
                                                                         {
                                                                             networkService
                                                                         }.ToArray());

            securityServiceContainer = this.MountHostFolder(securityServiceContainer);
            securityServiceContainer = this.SetDockerCredentials(securityServiceContainer);

            // Now build and return the container                
            IContainerService builtContainer = securityServiceContainer.Build().Start().WaitForPort("5001/tcp", 30000);
            Thread.Sleep(20000); // This hack is in till health checks implemented :|

            this.Trace("Security Service Container Started");

            return builtContainer;
        }

        /// <summary>
        /// Setups the test host container.
        /// </summary>
        /// <param name="imageName">Name of the image.</param>
        /// <param name="networkServices">The network services.</param>
        /// <param name="forceLatestImage">if set to <c>true</c> [force latest image].</param>
        /// <param name="additionalEnvironmentVariables">The additional environment variables.</param>
        /// <returns></returns>
        public virtual IContainerService SetupTestHostContainer(String imageName,
                                                                List<INetworkService> networkServices,
                                                                Boolean forceLatestImage = false,
                                                                List<String> additionalEnvironmentVariables = null)
        {
            this.Trace("About to Start Test Hosts Container");

            List<String> environmentVariables = new List<String>();
            environmentVariables
                .Add($"ConnectionStrings:TestBankReadModel=\"server={this.SqlServerDetails.sqlServerContainerName};user id={this.SqlServerDetails.sqlServerUserName};password={this.SqlServerDetails.sqlServerPassword};database=TestBankReadModel\"");
            environmentVariables.Add("ASPNETCORE_ENVIRONMENT=IntegrationTest");

            if (additionalEnvironmentVariables != null)
            {
                environmentVariables.AddRange(additionalEnvironmentVariables);
            }

            ContainerBuilder testHostContainer = new Builder().UseContainer().WithName(this.TestHostContainerName).WithEnvironment(environmentVariables.ToArray())
                                                              .UseImage(imageName, forceLatestImage).ExposePort(DockerHelper.TestHostPort)
                                                              .UseNetwork(networkServices.ToArray());

            testHostContainer = this.MountHostFolder(testHostContainer);
            testHostContainer = this.SetDockerCredentials(testHostContainer);

            // Now build and return the container                
            IContainerService builtContainer = testHostContainer.Build().Start().WaitForPort($"{DockerHelper.TestHostPort}/tcp", 30000);

            this.Trace("Test Hosts Container Started");

            return builtContainer;
        }

        /// <summary>
        /// Setups the test network.
        /// </summary>
        /// <param name="networkName">Name of the network.</param>
        /// <param name="reuseIfExists">if set to <c>true</c> [reuse if exists].</param>
        /// <returns></returns>
        public static INetworkService SetupTestNetwork(String networkName = null,
                                                       Boolean reuseIfExists = false)
        {
            networkName = string.IsNullOrEmpty(networkName) ? $"testnetwork{Guid.NewGuid()}" : networkName;

            // Build a network
            NetworkBuilder networkService = new Builder().UseNetwork(networkName);

            if (reuseIfExists)
            {
                networkService.ReuseIfExist();
            }

            return networkService.Build();
        }

        /// <summary>
        /// Setups the transaction processor acl container.
        /// </summary>
        /// <param name="imageName">Name of the image.</param>
        /// <param name="networkService">The network service.</param>
        /// <param name="forceLatestImage">if set to <c>true</c> [force latest image].</param>
        /// <param name="securityServicePort">The security service port.</param>
        /// <param name="additionalEnvironmentVariables">The additional environment variables.</param>
        /// <returns></returns>
        public virtual IContainerService SetupTransactionProcessorACLContainer(String imageName,
                                                                               INetworkService networkService,
                                                                               Boolean forceLatestImage = false,
                                                                               Int32 securityServicePort = DockerHelper.SecurityServiceDockerPort,
                                                                               List<String> additionalEnvironmentVariables = null)
        {
            this.Trace("About to Start Transaction Processor ACL Container");

            List<String> environmentVariables = new List<String>();
            environmentVariables.Add($"AppSettings:SecurityService=https://{this.SecurityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"SecurityConfiguration:Authority=https://{this.SecurityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"urls=http://*:{DockerHelper.TransactionProcessorACLDockerPort}");
            environmentVariables
                .Add($"AppSettings:TransactionProcessorApi=http://{this.TransactionProcessorContainerName}:{DockerHelper.TransactionProcessorDockerPort}");
            environmentVariables.Add($"AppSettings:ClientId={this.ClientDetails.clientId}");
            environmentVariables.Add($"AppSettings:ClientSecret={this.ClientDetails.clientSecret}");

            if (additionalEnvironmentVariables != null)
            {
                environmentVariables.AddRange(additionalEnvironmentVariables);
            }

            ContainerBuilder transactionProcessorACLContainer = new Builder().UseContainer().WithName(this.TransactionProcessorACLContainerName)
                                                                             .WithEnvironment(environmentVariables.ToArray()).UseImage(imageName, forceLatestImage)
                                                                             .ExposePort(DockerHelper.TransactionProcessorACLDockerPort)
                                                                             .UseNetwork(new List<INetworkService>
                                                                                         {
                                                                                             networkService
                                                                                         }.ToArray());

            transactionProcessorACLContainer = this.MountHostFolder(transactionProcessorACLContainer);
            transactionProcessorACLContainer = this.SetDockerCredentials(transactionProcessorACLContainer);

            // Now build and return the container                
            IContainerService builtContainer =
                transactionProcessorACLContainer.Build().Start().WaitForPort($"{DockerHelper.TransactionProcessorACLDockerPort}/tcp", 30000);

            this.Trace("Transaction Processor Container ACL Started");

            return builtContainer;
        }

        /// <summary>
        /// Setups the transaction processor container.
        /// </summary>
        /// <param name="imageName">Name of the image.</param>
        /// <param name="networkServices">The network services.</param>
        /// <param name="forceLatestImage">if set to <c>true</c> [force latest image].</param>
        /// <param name="securityServicePort">The security service port.</param>
        /// <param name="additionalEnvironmentVariables">The additional environment variables.</param>
        /// <returns></returns>
        public virtual IContainerService SetupTransactionProcessorContainer(String imageName,
                                                                            List<INetworkService> networkServices,
                                                                            Boolean forceLatestImage = false,
                                                                            Int32 securityServicePort = DockerHelper.SecurityServiceDockerPort,
                                                                            List<String> additionalEnvironmentVariables = null)
        {
            this.Trace("About to Start Transaction Processor Container");

            List<String> environmentVariables = new List<String>();
            environmentVariables.Add($"EventStoreSettings:ConnectionString={this.GenerateEventStoreConnectionString()}");
            environmentVariables.Add($"AppSettings:SecurityService=https://{this.SecurityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"AppSettings:EstateManagementApi=http://{this.EstateManagementContainerName}:{DockerHelper.EstateManagementDockerPort}");
            environmentVariables.Add($"AppSettings:VoucherManagementApi=http://{this.VoucherManagementContainerName}:{DockerHelper.VoucherManagementDockerPort}");
            environmentVariables.Add($"SecurityConfiguration:Authority=https://{this.SecurityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"urls=http://*:{DockerHelper.TransactionProcessorDockerPort}");
            environmentVariables.Add($"AppSettings:ClientId={this.ClientDetails.clientId}");
            environmentVariables.Add($"AppSettings:ClientSecret={this.ClientDetails.clientSecret}");
            environmentVariables.Add("AppSettings:SubscriptionFilter=TransactionProcessor");

            environmentVariables.Add($"OperatorConfiguration:Safaricom:Url=http://{this.TestHostContainerName}:9000/api/safaricom");

            if (additionalEnvironmentVariables != null)
            {
                environmentVariables.AddRange(additionalEnvironmentVariables);
            }

            ContainerBuilder transactionProcessorContainer = new Builder().UseContainer().WithName(this.TransactionProcessorContainerName)
                                                                          .WithEnvironment(environmentVariables.ToArray()).UseImage(imageName, forceLatestImage)
                                                                          .ExposePort(DockerHelper.TransactionProcessorDockerPort).UseNetwork(networkServices.ToArray());

            transactionProcessorContainer = this.MountHostFolder(transactionProcessorContainer);
            transactionProcessorContainer = this.SetDockerCredentials(transactionProcessorContainer);

            // Now build and return the container                
            IContainerService builtContainer = transactionProcessorContainer.Build().Start().WaitForPort($"{DockerHelper.TransactionProcessorDockerPort}/tcp", 30000);

            this.Trace("Transaction Processor Container Started");

            return builtContainer;
        }

        /// <summary>
        /// Setups the voucher management acl container.
        /// </summary>
        /// <param name="imageName">Name of the image.</param>
        /// <param name="networkServices">The network services.</param>
        /// <param name="forceLatestImage">if set to <c>true</c> [force latest image].</param>
        /// <param name="securityServicePort">The security service port.</param>
        /// <param name="additionalEnvironmentVariables">The additional environment variables.</param>
        /// <returns></returns>
        public virtual IContainerService SetupVoucherManagementACLContainer(String imageName,
                                                                            List<INetworkService> networkServices,
                                                                            Boolean forceLatestImage = false,
                                                                            Int32 securityServicePort = DockerHelper.SecurityServiceDockerPort,
                                                                            List<String> additionalEnvironmentVariables = null)
        {
            this.Trace("About to Start Voucher Management ACL Container");

            List<String> environmentVariables = new List<String>();
            environmentVariables.Add($"AppSettings:SecurityService=https://{this.SecurityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"AppSettings:VoucherManagementApi=http://{this.VoucherManagementContainerName}:{DockerHelper.VoucherManagementDockerPort}");
            environmentVariables.Add($"SecurityConfiguration:Authority=https://{this.SecurityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"urls=http://*:{DockerHelper.VoucherManagementACLDockerPort}");
            environmentVariables.Add($"AppSettings:ClientId={this.ClientDetails.clientId}");
            environmentVariables.Add($"AppSettings:ClientSecret={this.ClientDetails.clientSecret}");

            if (additionalEnvironmentVariables != null)
            {
                environmentVariables.AddRange(additionalEnvironmentVariables);
            }

            ContainerBuilder voucherManagementAclContainer = new Builder().UseContainer().WithName(this.VoucherManagementACLContainerName)
                                                                          .WithEnvironment(environmentVariables.ToArray()).UseImage(imageName, forceLatestImage)
                                                                          .ExposePort(DockerHelper.VoucherManagementACLDockerPort).UseNetwork(networkServices.ToArray());
            voucherManagementAclContainer = this.MountHostFolder(voucherManagementAclContainer);
            voucherManagementAclContainer = this.SetDockerCredentials(voucherManagementAclContainer);

            // Now build and return the container                
            IContainerService builtContainer = voucherManagementAclContainer.Build().Start().WaitForPort($"{DockerHelper.VoucherManagementACLDockerPort}/tcp", 30000);

            this.Trace("Voucher Management ACL Container Started");

            return builtContainer;
        }

        /// <summary>
        /// Setups the voucher management container.
        /// </summary>
        /// <param name="imageName">Name of the image.</param>
        /// <param name="networkServices">The network services.</param>
        /// <param name="forceLatestImage">if set to <c>true</c> [force latest image].</param>
        /// <param name="securityServicePort">The security service port.</param>
        /// <param name="additionalEnvironmentVariables">The additional environment variables.</param>
        /// <returns></returns>
        public virtual IContainerService SetupVoucherManagementContainer(String imageName,
                                                                         List<INetworkService> networkServices,
                                                                         Boolean forceLatestImage = false,
                                                                         Int32 securityServicePort = DockerHelper.SecurityServiceDockerPort,
                                                                         List<String> additionalEnvironmentVariables = null)
        {
            this.Trace("About to Start Voucher Management Container");

            List<String> environmentVariables = new List<String>();
            environmentVariables.Add($"EventStoreSettings:ConnectionString={this.GenerateEventStoreConnectionString()}");
            environmentVariables.Add($"AppSettings:SecurityService=https://{this.SecurityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"AppSettings:EstateManagementApi=http://{this.EstateManagementContainerName}:{DockerHelper.EstateManagementDockerPort}");
            environmentVariables.Add($"SecurityConfiguration:Authority=https://{this.SecurityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"urls=http://*:{DockerHelper.VoucherManagementDockerPort}");
            environmentVariables.Add($"AppSettings:ClientId={this.ClientDetails.clientId}");
            environmentVariables.Add($"AppSettings:ClientSecret={this.ClientDetails.clientSecret}");
            environmentVariables
                .Add($"ConnectionStrings:EstateReportingReadModel=\"server={this.SqlServerDetails.sqlServerContainerName};user id={this.SqlServerDetails.sqlServerUserName};password={this.SqlServerDetails.sqlServerPassword};database=EstateReportingReadModel\"");

            if (additionalEnvironmentVariables != null)
            {
                environmentVariables.AddRange(additionalEnvironmentVariables);
            }

            ContainerBuilder voucherManagementContainer = new Builder().UseContainer().WithName(this.VoucherManagementContainerName)
                                                                       .WithEnvironment(environmentVariables.ToArray()).UseImage(imageName, forceLatestImage)
                                                                       .ExposePort(DockerHelper.VoucherManagementDockerPort).UseNetwork(networkServices.ToArray());

            voucherManagementContainer = this.MountHostFolder(voucherManagementContainer);
            voucherManagementContainer = this.SetDockerCredentials(voucherManagementContainer);

            // Now build and return the container                
            IContainerService builtContainer = voucherManagementContainer.Build().Start().WaitForPort($"{DockerHelper.VoucherManagementDockerPort}/tcp", 30000);

            this.Trace("Voucher Management  Container Started");

            return builtContainer;
        }

        /// <summary>
        /// Starts the containers for scenario run.
        /// </summary>
        /// <param name="scenarioName">Name of the scenario.</param>
        /// <returns></returns>
        public abstract Task StartContainersForScenarioRun(String scenarioName);

        /// <summary>
        /// Starts the SQL container with open connection.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="imageName">Name of the image.</param>
        /// <param name="networkService">The network service.</param>
        /// <param name="hostFolder">The host folder.</param>
        /// <param name="dockerCredentials">The docker credentials.</param>
        /// <param name="sqlUserName">Name of the SQL user.</param>
        /// <param name="sqlPassword">The SQL password.</param>
        /// <returns></returns>
        public static IContainerService StartSqlContainerWithOpenConnection(String containerName,
                                                                            ILogger logger,
                                                                            String imageName,
                                                                            INetworkService networkService,
                                                                            String hostFolder,
                                                                            (String URL, String UserName, String Password)? dockerCredentials,
                                                                            String sqlUserName = "sa",
                                                                            String sqlPassword = "thisisalongpassword123!")
        {
            logger.LogInformation("About to start SQL Server Container");
            IContainerService databaseServerContainer = new Builder().UseContainer().WithName(containerName).UseImage(imageName)
                                                                     .WithEnvironment("ACCEPT_EULA=Y", $"SA_PASSWORD={sqlPassword}").ExposePort(1433)
                                                                     .UseNetwork(networkService).KeepContainer().KeepRunning().ReuseIfExists().Build().Start()
                                                                     .WaitForPort("1433/tcp", 30000);

            logger.LogInformation("SQL Server Container Started");

            logger.LogInformation("About to SQL Server Container is running");
            IPEndPoint sqlServerEndpoint = databaseServerContainer.ToHostExposedEndpoint("1433/tcp");

            // Try opening a connection
            Int32 maxRetries = 10;
            Int32 counter = 1;

            String server = "127.0.0.1";
            String database = "master";
            String user = sqlUserName;
            String password = sqlPassword;
            String port = sqlServerEndpoint.Port.ToString();

            String connectionString = $"server={server},{port};user id={user}; password={password}; database={database};";
            logger.LogInformation($"Connection String {connectionString}");
            SqlConnection connection = new SqlConnection(connectionString);

            while (counter <= maxRetries)
            {
                try
                {
                    logger.LogInformation($"Database Connection Attempt {counter}");

                    connection.Open();

                    SqlCommand command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM sys.databases";
                    command.ExecuteNonQuery();

                    logger.LogInformation("Connection Opened");

                    connection.Close();
                    logger.LogInformation("SQL Server Container Running");
                    break;
                }
                catch(SqlException ex)
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }

                    logger.LogError(ex);
                    Thread.Sleep(20000);
                }
                finally
                {
                    counter++;
                }
            }

            return databaseServerContainer;
        }

        /// <summary>
        /// Stops the containers for scenario run.
        /// </summary>
        /// <returns></returns>
        public abstract Task StopContainersForScenarioRun();

        /// <summary>
        /// Generates the event store connection string.
        /// </summary>
        /// <returns></returns>
        protected virtual String GenerateEventStoreConnectionString()
        {
            String eventStoreAddress = $"esdb://admin:changeit@{this.EventStoreContainerName}:{DockerHelper.EventStoreHttpDockerPort}?tls=false";

            return eventStoreAddress;
        }

        protected ContainerBuilder MountHostFolder(ContainerBuilder containerBuilder,
                                                   String containerPath = "/home/txnproc/trace")
        {
            if (string.IsNullOrEmpty(this.HostTraceFolder) == false)
            {
                containerBuilder = containerBuilder.Mount(this.HostTraceFolder, containerPath, MountType.ReadWrite);
            }

            return containerBuilder;
        }

        protected ContainerBuilder SetDockerCredentials(ContainerBuilder containerBuilder)
        {
            if (this.DockerCredentials.HasValue)
            {
                containerBuilder = containerBuilder.WithCredential(this.DockerCredentials.Value.URL,
                                                                   this.DockerCredentials.Value.UserName,
                                                                   this.DockerCredentials.Value.Password);
            }

            return containerBuilder;
        }

        protected void Trace(String traceMessage)
        {
            if (this.Logger.IsInitialised)
            {
                this.Logger.LogInformation(traceMessage);
            }
        }

        #endregion

        #region Others

        /// <summary>
        /// The callback handler docker port
        /// </summary>
        public const Int32 CallbackHandlerDockerPort = 5010;

        /// <summary>
        /// The estate management docker port
        /// </summary>
        public const Int32 EstateManagementDockerPort = 5000;

        /// <summary>
        /// The estate reporting docker port
        /// </summary>
        public const Int32 EstateReportingDockerPort = 5005;

        /// <summary>
        /// The event store HTTP docker port
        /// </summary>
        public const Int32 EventStoreHttpDockerPort = 2113;

        /// <summary>
        /// The event store TCP docker port
        /// </summary>
        public const Int32 EventStoreTcpDockerPort = 1113;

        /// <summary>
        /// The messaging service docker port
        /// </summary>
        public const Int32 MessagingServiceDockerPort = 5006;

        /// <summary>
        /// The security service docker port
        /// </summary>
        public const Int32 SecurityServiceDockerPort = 5001;

        /// <summary>
        /// The test host port
        /// </summary>
        public const Int32 TestHostPort = 9000;

        /// <summary>
        /// The transaction processor acl docker port
        /// </summary>
        public const Int32 TransactionProcessorACLDockerPort = 5003;

        /// <summary>
        /// The transaction processor docker port
        /// </summary>
        public const Int32 TransactionProcessorDockerPort = 5002;

        /// <summary>
        /// The voucher management acl docker port
        /// </summary>
        public const Int32 VoucherManagementACLDockerPort = 5008;

        /// <summary>
        /// The voucher management docker port
        /// </summary>
        public const Int32 VoucherManagementDockerPort = 5007;

        #endregion
    }
}