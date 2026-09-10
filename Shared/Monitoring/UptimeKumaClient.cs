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
        private readonly ISocketIO Socket;
        private readonly UptimeKumaConfiguration Configuration;


        public UptimeKumaClient(ISocketIO socket, UptimeKumaConfiguration configuration) {
            this.Socket = socket;
            this.Configuration = configuration;
        }
        
        public async Task<Result<Int32>> RegisterMonitor(UptimeKumaMonitor monitorToAdd,
                                          CancellationToken cancellationToken) {
            if (!this.Configuration.Enabled)
            {
                return Result.Success(0);
            }

            await LoginAsync(this.Configuration.Username, this.Configuration.Password, cancellationToken);

            var monitor = new
            {
                type = "http",
                name = monitorToAdd.Name,
                url = monitorToAdd.Url,
                method = "GET",

                interval = monitorToAdd.IntervalInSeconds,
                retryInterval = monitorToAdd.IntervalInSeconds,
                maxretries = 0,
                resendInterval = 0,

                accepted_statuscodes = new[] { "200-299" },
                conditions = Array.Empty<object>(),
                notificationIDList = new Dictionary<string, bool>(),

                active = true,
                ignoreTls = true,
                expiryNotification = true,
                upsideDown = false,
                maxredirects = 10
            };

            var result = await EmitAsync<MonitorResponse>("add", monitor, cancellationToken);
            if (!result.ok || result.monitorID is null)
            {
                throw new InvalidOperationException($"Monitor creation failed: {result.msg}");
            }

            return result.monitorID.Value;
        }

        private async Task LoginAsync(string username,
                                      string password,
                                      CancellationToken cancellationToken) {
            var infoReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // Register before ConnectAsync.
            Socket.On("info", _ => {
                infoReceived.TrySetResult();
                return Task.CompletedTask;
            });

            await Socket.ConnectAsync(cancellationToken);

            await infoReceived.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);

            var login = await EmitAsync<LoginResponse>("login", new { username, password, token = "" }, cancellationToken);

            if (!login.ok) {
                throw new InvalidOperationException($"Uptime Kuma login failed: {login.msg ?? "No error message returned"}");
            }
        }


        private async Task<T> EmitAsync<T>(string eventName, object payload, CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource<T>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            await Socket.EmitAsync(
                eventName,
                new[] { payload },
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

            return await completion.Task;
        }

        public async ValueTask DisposeAsync()
        {
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
    }


    public record UptimeKumaConfiguration(Boolean Enabled, String ServerAddress, String Username, String Password);

    public record UptimeKumaMonitor(String Name, String Url, Int32 IntervalInSeconds=60);
}
