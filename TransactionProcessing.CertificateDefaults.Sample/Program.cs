using TransactionProcessing.CertificateDefaults;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureTransactionProcessingHttps();

builder.Services.AddHttpClient("SecurityService", client =>
{
    string? baseAddress = builder.Configuration["SecurityService:BaseAddress"];
    if (!string.IsNullOrWhiteSpace(baseAddress))
    {
        client.BaseAddress = new Uri(baseAddress, UriKind.Absolute);
    }
});

WebApplication app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.Run();
