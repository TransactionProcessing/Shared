using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SimpleResults;

namespace Shared.Monitoring
{
    public interface IUptimeKumaClient {
        Task<Result<Int32>> RegisterMonitor(UptimeKumaMonitor monitorToAdd, CancellationToken cancellationToken);
    }

    public class UptimeKumaClient : IUptimeKumaClient, IAsyncDisposable {
        private readonly ISocketIO? Socket;
        private readonly UptimeKumaConfiguration Configuration;


        public UptimeKumaClient(ISocketIO? socket, UptimeKumaConfiguration configuration) {
            this.Socket = socket;
            this.Configuration = configuration;
        }
        
        public async Task<Result<Int32>> RegisterMonitor(UptimeKumaMonitor monitorToAdd,
                                          CancellationToken cancellationToken) {
            if (!this.Configuration.Enabled)
            {
                return Result.Success(0);
            }

            var monitorListReceived = new TaskCompletionSource<JsonElement>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            await LoginAsync(this.Configuration.Username, this.Configuration.Password, cancellationToken);

            Socket.On("monitorList", response =>
            {
                try
                {
                    monitorListReceived.TrySetResult(response.GetValue<JsonElement>(0));
                }
                catch (Exception exception)
                {
                    monitorListReceived.TrySetException(exception);
                }

                return Task.CompletedTask;
            });

            var listResult = await EmitAsync<MonitorListResponse>(
                "getMonitorList",
                null,
                cancellationToken);
            if (!listResult.ok)
            {
                throw new InvalidOperationException($"Monitor list retrieval failed: {listResult.msg}");
            }

            var monitorList = await monitorListReceived.Task.WaitAsync(
                TimeSpan.FromSeconds(30),
                cancellationToken);
            var existingMonitor = FindExistingMonitor(monitorList, monitorToAdd);
            var monitor = BuildMonitorPayload(monitorToAdd);

            if (existingMonitor is null)
            {
                var addResult = await EmitAsync<MonitorResponse>("add", monitor, cancellationToken);
                if (!addResult.ok || addResult.monitorID is null)
                {
                    throw new InvalidOperationException($"Monitor creation failed: {addResult.msg}");
                }

                return addResult.monitorID.Value;
            }

            monitor["id"] = existingMonitor.Id;
            var editResult = await EmitAsync<MonitorResponse>(
                "editMonitor",
                monitor,
                cancellationToken);
            if (!editResult.ok)
            {
                throw new InvalidOperationException($"Monitor update failed: {editResult.msg}");
            }

            return existingMonitor.Id;
        }

        internal static ExistingMonitor? FindExistingMonitor(
            JsonElement monitorList,
            UptimeKumaMonitor monitorToFind)
        {
            if (monitorList.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var monitorProperty in monitorList.EnumerateObject())
            {
                var monitor = monitorProperty.Value;
                if (monitor.ValueKind != JsonValueKind.Object
                    || !monitor.TryGetProperty("name", out var name)
                    || !monitor.TryGetProperty("url", out var url)
                    || !string.Equals(name.GetString(), monitorToFind.Name, StringComparison.Ordinal)
                    || !string.Equals(url.GetString(), monitorToFind.Url, StringComparison.Ordinal))
                {
                    continue;
                }

                if (monitor.TryGetProperty("id", out var idProperty)
                    && idProperty.TryGetInt32(out var monitorId))
                {
                    return new ExistingMonitor(monitorId);
                }

                if (int.TryParse(monitorProperty.Name, out monitorId))
                {
                    return new ExistingMonitor(monitorId);
                }
            }

            return null;
        }

        private static Dictionary<string, object> BuildMonitorPayload(UptimeKumaMonitor monitorToAdd)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "http",
                ["name"] = monitorToAdd.Name,
                ["url"] = monitorToAdd.Url,
                ["method"] = "GET",
                ["interval"] = monitorToAdd.IntervalInSeconds,
                ["retryInterval"] = monitorToAdd.IntervalInSeconds,
                ["maxretries"] = 0,
                ["resendInterval"] = 0,
                ["accepted_statuscodes"] = new[] { "200-299" },
                ["conditions"] = Array.Empty<object>(),
                ["notificationIDList"] = new Dictionary<string, bool>(),
                ["active"] = true,
                ["ignoreTls"] = monitorToAdd.IgnoreTls,
                ["expiryNotification"] = true,
                ["upsideDown"] = false,
                ["maxredirects"] = 10
            };
        }

        private async Task LoginAsync(string username,
                                      string password,
                                      CancellationToken cancellationToken) {
            if (Socket.Connected)
            {
                return;
            }

            var infoReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // Register before ConnectAsync.
            Socket.On("info", _ => {
                infoReceived.TrySetResult();
                return Task.CompletedTask;
            });

            await Socket.ConnectAsync(cancellationToken);

            await infoReceived.Task.WaitAsync(TimeSpan.FromSeconds(60), cancellationToken);

            var login = await EmitAsync<LoginResponse>("login", new { username, password, token = "" }, cancellationToken);

            if (!login.ok) {
                throw new InvalidOperationException($"Uptime Kuma login failed: {login.msg ?? "No error message returned"}");
            }
        }


        private async Task<T> EmitAsync<T>(
            string eventName,
            object? payload,
            CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource<T>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            await Socket.EmitAsync(
                eventName,
                payload is null ? Array.Empty<object>() : new[] { payload },
                acknowledgement =>
                {
                    try
                    {
                        var value = acknowledgement.GetValue<T>(0);
                        completion.TrySetResult(value);
                    }
                    catch (Exception exception)
                    {
                        completion.TrySetException(exception);
                    }

                    return Task.CompletedTask;
                }, cancellationToken);

            return await completion.Task.WaitAsync(
                TimeSpan.FromSeconds(30),
                cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            if (Socket is null)
            {
                return;
            }

            await Socket.DisconnectAsync();
            Socket.Dispose();
        }

        private sealed class LoginResponse
        {
            public bool ok { get; set; }
            public string? msg { get; set; }
        }

        private sealed class MonitorResponse
        {
            public bool ok { get; set; }
            public string? msg { get; set; }
            public int? monitorID { get; set; }
        }

        private sealed class MonitorListResponse
        {
            public bool ok { get; set; }
            public string? msg { get; set; }
        }

        internal sealed record ExistingMonitor(int Id);
    }


    public record UptimeKumaConfiguration(Boolean Enabled, String ServerAddress, String Username, String Password);

    public record UptimeKumaMonitor(
        String Name,
        String Url,
        Int32 IntervalInSeconds = 60,
        Boolean IgnoreTls = false);
}
