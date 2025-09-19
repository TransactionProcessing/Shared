using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Tests
{
    using System.Reflection;
    using General;
    using Microsoft.Extensions.Configuration;
    using Shouldly;
    using Xunit;
    using Xunit.Abstractions;

    public partial class SharedTests
    {
        private readonly ITestOutputHelper TestOutputHelper;

        public SharedTests(ITestOutputHelper testOutputHelper){
            this.TestOutputHelper = testOutputHelper;
        }

        [Fact]
        public void ConfigurationReader_Initialise_ReaderIsInitialised(){
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(TestHelpers.DefaultAppSettings).AddEnvironmentVariables();
            ConfigurationReader.Initialise(configurationBuilder.Build());

            ConfigurationReader.IsInitialised.ShouldBeTrue();
        }

        [Fact]
        public void ConfigurationReader_Initialise_NullConfigurationRoot_ErrorThrown(){
            Should.Throw<ArgumentNullException>(() => ConfigurationReader.Initialise(null));
        }

        [Fact]
        public void ConfigurationReader_GetBaseServerUri_ValueReturned()
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(TestHelpers.DefaultAppSettings).AddEnvironmentVariables();
            ConfigurationReader.Initialise(configurationBuilder.Build());
            Uri uri = ConfigurationReader.GetBaseServerUri("TransactionProcessorApi");
            uri.AbsoluteUri.ShouldBe("http://127.0.0.1:5002/");
        }

        [Fact]
        public void ConfigurationReader_GetValue_NotInitialised_ErrorThrown()
        {
            var field = typeof(ConfigurationReader).GetProperty("IsInitialised", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty);

            // Normally the first argument to "SetValue" is the instance
            // of the type but since we are mutating a static field we pass "null"
            field.SetValue(null, false);

            TestOutputHelper.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff"));

            Should.Throw<InvalidOperationException>(() => {
                                                        ConfigurationReader.GetValue("AppSettings", "TestArray");
                                                    });
        }

        [Fact]
        public void ConfigurationReader_GetValue_SectionNotFound_ErrorThrown()
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(TestHelpers.DefaultAppSettings).AddEnvironmentVariables();
            ConfigurationReader.Initialise(configurationBuilder.Build());

            Should.Throw<KeyNotFoundException>(() => {
                                                        ConfigurationReader.GetValue("AppSettings1", "TestArray");
                                                    });
        }

        [Fact]
        public void ConfigurationReader_GetValue_KeyNotFound_ErrorThrown()
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(TestHelpers.DefaultAppSettings).AddEnvironmentVariables();
            ConfigurationReader.Initialise(configurationBuilder.Build());

            Should.Throw<KeyNotFoundException>(() => {
                                                   ConfigurationReader.GetValue("AppSettings", "MissingKey");
                                               });
        }

        [Fact]
        public void ConfigurationReader_GetValue_ValueIsReturned()
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(TestHelpers.DefaultAppSettings).AddEnvironmentVariables();
            ConfigurationReader.Initialise(configurationBuilder.Build());

            var value = ConfigurationReader.GetValue("TransactionProcessorApi");
            value.ShouldBe("http://127.0.0.1:5002");
        }



        [Fact]
        public void ConfigurationReader_GetValueFromSection_ValueIsReturned()
        {
            TestOutputHelper.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff"));
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(TestHelpers.DefaultAppSettings).AddEnvironmentVariables();
            ConfigurationReader.Initialise(configurationBuilder.Build());

            var value = ConfigurationReader.GetValueFromSection<List<String>>("AppSettings" , "TestArray");
            value[0].ShouldBe("A");
            value[1].ShouldBe("B");
            value[2].ShouldBe("C");
        }

        [Fact]
        public void ConfigurationReader_GetValueFromSection_NotInitialised_ErrorThrown(){
            // This is a bit of a hack to make this test pass
            var field = typeof(ConfigurationReader).GetProperty("IsInitialised", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty);

            // Normally the first argument to "SetValue" is the instance
            // of the type but since we are mutating a static field we pass "null"
            field.SetValue(null, false);

            TestOutputHelper.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff"));
            
            Should.Throw<InvalidOperationException>(() => {
                                                        ConfigurationReader.GetValueFromSection<List<String>>("AppSettings", "TestArray");
                                                    });
        }

        [Fact]
        public void ConfigurationReader_GetValueFromSection_SectionNotFound_ErrorThrown()
        {
            TestOutputHelper.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff"));
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(TestHelpers.DefaultAppSettings).AddEnvironmentVariables();
            ConfigurationReader.Initialise(configurationBuilder.Build());

            Should.Throw<KeyNotFoundException>(() => {
                                                        ConfigurationReader.GetValueFromSection<List<String>>("AppSettings1", "TestArray");
                                                    });
        }

        [Fact]
        public void ConfigurationReader_GetValueFromSection_KeyNotFound_ErrorThrown()
        {
            TestOutputHelper.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff"));
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(TestHelpers.DefaultAppSettings).AddEnvironmentVariables();
            ConfigurationReader.Initialise(configurationBuilder.Build());

            Should.Throw<KeyNotFoundException>(() => {
                                                   ConfigurationReader.GetValueFromSection<List<String>>("AppSettings", "TestArray1");
                                               });
        }

        [Fact]
        public void ConfigurationReader_GetConnectionString_ValueIsReturned()
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(TestHelpers.DefaultAppSettings).AddEnvironmentVariables();
            ConfigurationReader.Initialise(configurationBuilder.Build());

            var value = ConfigurationReader.GetConnectionString("HealthCheck");
            value.ShouldBe("server=192.168.1.133;database=master;user id=sa;password=Sc0tland");
        }

        [Fact]
        public void ConfigurationReader_GetValueOrDefault_ValueIsReturned()
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(TestHelpers.DefaultAppSettings).AddEnvironmentVariables();
            ConfigurationReader.Initialise(configurationBuilder.Build());

            var value = ConfigurationReader.GetValueOrDefault("AppSettings","TransactionProcessorApi", "http://127.0.0.1:5001");
            value.ShouldBe("http://127.0.0.1:5002");
        }

        [Fact]
        public void ConfigurationReader_GetValueOrDefault_DefaultValueIsReturned()
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(TestHelpers.DefaultAppSettings).AddEnvironmentVariables();
            ConfigurationReader.Initialise(configurationBuilder.Build());

            var value = ConfigurationReader.GetValueOrDefault("AppSettings", "TransactionProcessorApiX", "http://127.0.0.1:5001");
            value.ShouldBe("http://127.0.0.1:5001");
        }

        [Fact]
        public void ConfigurationReader_GetValueOrDefault_ConfigEmpty_DefaultValueIsReturned()
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(TestHelpers.DefaultAppSettings).AddEnvironmentVariables();
            ConfigurationReader.Initialise(configurationBuilder.Build());

            var value = ConfigurationReader.GetValueOrDefault("AppSettings", "Test", "http://127.0.0.1:5001");
            value.ShouldBe("http://127.0.0.1:5001");
        }

    }
}
