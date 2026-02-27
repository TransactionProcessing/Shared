using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Shared.Middleware;

[ExcludeFromCodeCoverage]
public class ResponseLoggingMemoryStream : MemoryStream {
    public override void Close() {
        // Dont close by default
    }

    public void ForceClose() {
        base.Close();
    }

    public bool IsDisposed() {
        return this.CanRead && this.CanSeek && this.CanWrite;
    }
}