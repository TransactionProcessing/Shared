using System.Net.Http;

namespace TransactionProcessing.ClientProxyBase;

public static class GetSocketsHttpHandler {
    public static HttpMessageHandler GetHandler() {
        // Create a new SocketsHttpHandler with custom SSL options
        return new SocketsHttpHandler
        {
            SslOptions =
            {
                RemoteCertificateValidationCallback = (sender,
                                                       certificate,
                                                       chain,
                                                       errors) => true,
            }
        };
    }
}