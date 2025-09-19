using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Tests;

using Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Shouldly;
using Xunit;

public partial class SharedTests
{
    [Theory]
    [InlineData("Development", false)]
    [InlineData("Staging", false)]
    [InlineData("PreProduction", true)]
    [InlineData("Production", false)]
    public void HostEnvironmentExtensions_IsPreProduction_CorrectValueReturned(String environment, Boolean expectedValue){
        IHostEnvironment hostEnvironment = new HostingEnvironment();
        hostEnvironment.EnvironmentName = environment;

        Boolean result = hostEnvironment.IsPreProduction();

        result.ShouldBe(expectedValue);
    }

    [Fact]
    public void HostEnvironmentExtensions_IsPreProduction_IHostEnvironment_ArgumentNullExceptionThrown()
    {
        IHostEnvironment hostEnvironment = null;
        Should.Throw<ArgumentNullException>(() => hostEnvironment.IsPreProduction());

    }
}