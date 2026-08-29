using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.IntegrationTesting.Tests;

using NLog;
using Reqnroll;
using Shared.Logger;
using Shouldly;

[Binding]
public class Setup
{
    [BeforeTestRun]
    protected static Task GlobalSetup(){
        ShouldlyConfiguration.DefaultTaskTimeout = TimeSpan.FromMinutes(1);
        return Task.CompletedTask;
    }
}
