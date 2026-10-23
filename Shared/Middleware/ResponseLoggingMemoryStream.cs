using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Middleware;

[ExcludeFromCodeCoverage]
public sealed class ResponseLoggingMemoryStream : Stream
{
    private readonly Stream destination;
    private readonly MemoryStream captured = new();
    private readonly Int32 maximumBytes;

    public ResponseLoggingMemoryStream(Stream destination, Int32 maximumBytes)
    {
        this.destination = destination;
        this.maximumBytes = Math.Max(0, maximumBytes);
    }

    public Byte[] GetCapturedBytes() => this.captured.ToArray();
    public Boolean IsDisposed() => this.captured.CanRead && this.captured.CanSeek && this.captured.CanWrite;
    public void ForceClose() => this.captured.Close();
    public override Boolean CanRead => false;
    public override Boolean CanSeek => false;
    public override Boolean CanWrite => true;
    public override Int64 Length => throw new NotSupportedException();
    public override Int64 Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Flush() => this.destination.Flush();
    public override void Close() { }
    public override Task FlushAsync(CancellationToken cancellationToken) => this.destination.FlushAsync(cancellationToken);
    public override void Write(Byte[] buffer, Int32 offset, Int32 count)
    {
        Int32 remaining = Math.Max(0, this.maximumBytes + 1 - (Int32)this.captured.Length);
        if (remaining > 0) this.captured.Write(buffer, offset, Math.Min(count, remaining));
        this.destination.Write(buffer, offset, count);
    }
    public override async ValueTask WriteAsync(ReadOnlyMemory<Byte> buffer, CancellationToken cancellationToken = default)
    {
        Int32 remaining = Math.Max(0, this.maximumBytes + 1 - (Int32)this.captured.Length);
        if (remaining > 0) await this.captured.WriteAsync(buffer[..Math.Min(buffer.Length, remaining)], cancellationToken);
        await this.destination.WriteAsync(buffer, cancellationToken);
    }
    public override Task WriteAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken) =>
        this.WriteAsync(new ReadOnlyMemory<Byte>(buffer, offset, count), cancellationToken).AsTask();
    public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count) => throw new NotSupportedException();
    public override Int64 Seek(Int64 offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(Int64 value) => throw new NotSupportedException();
}
