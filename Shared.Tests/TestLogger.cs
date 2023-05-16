namespace Shared.Tests;

using System;
using System.Collections.Generic;
using Logger;

public class TestLogger : ILogger
{
    public TestLogger(){
        this.LogEntries = new List<String>();
    }
    public Boolean IsInitialised { get; set; }

    public String[] GetLogEntries()
    {
        return this.LogEntries.ToArray();
    }

    public void LogCritical(Exception exception)
    {
        this.LogEntries.Add(exception.ToString());
    }

    public void LogCritical(String message, Exception exception)
    {
        this.LogEntries.Add(message);
        this.LogEntries.Add(exception.ToString());
    }

    public void LogDebug(String message)
    {
        this.LogEntries.Add(message);
    }

    public void LogError(Exception exception)
    {
        this.LogEntries.Add(exception.ToString());
    }

    public void LogError(String message, Exception exception)
    {
        this.LogEntries.Add(message);
        this.LogEntries.Add(exception.ToString());
    }

    public void LogInformation(String message)
    {
        this.LogEntries.Add(message);
    }

    public void LogTrace(String message)
    {
        this.LogEntries.Add(message);
    }

    public void LogWarning(String message)
    {
        this.LogEntries.Add(message);
    }

    private readonly List<String> LogEntries;
}