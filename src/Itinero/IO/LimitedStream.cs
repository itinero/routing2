using System.IO;

namespace Itinero.IO
{
    // /// <summary>
    // /// Wraps a stream to prevent some fixed data from being overwritten.
    // /// </summary>
    // internal sealed class LimitedStream : Stream
    // {
    //     private readonly long _offset;
    //     private readonly Stream _stream;
    //
    //     /// <summary>
    //     /// Creates a new limited stream.
    //     /// </summary>
    //     public LimitedStream(Stream stream)
    //     {
    //         _stream = stream;
    //         _offset = _stream.Position;
    //     }
    //
    //     /// <summary>
    //     /// Creates a new limited stream.
    //     /// </summary>
    //     public LimitedStream(Stream stream, long offset)
    //     {
    //         _stream = stream;
    //         _offset = offset;
    //     }
    //
    //     /// <summary>
    //     /// Gets a value indicating whether the current stream supports reading.
    //     /// </summary>
    //     public override bool CanRead => _stream.CanRead;
    //
    //     /// <summary>
    //     /// Gets a value indicating whether the current stream supports seeking.
    //     /// </summary>
    //     public override bool CanSeek => _stream.CanSeek;
    //
    //     /// <summary>
    //     /// Gets a value indicating whether the current stream supports writing.
    //     /// </summary>
    //     public override bool CanWrite
    //     {
    //         get { return _stream.CanWrite; }
    //     }
    //
    //     /// <summary>
    //     /// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
    //     /// </summary>
    //     public override void Flush()
    //     {
    //         _stream.Flush();
    //     }
    //
    //     /// <summary>
    //     /// Returns the current length of this stream.
    //     /// </summary>
    //     public override long Length => _stream.Length - _offset;
    //
    //     /// <summary>
    //     /// Gets/sets the current position.
    //     /// </summary>
    //     public override long Position
    //     {
    //         get => _stream.Position - _offset;
    //         set => _stream.Position = value + _offset;
    //     }
    //
    //     /// <summary>
    //     /// Reads a sequence of bytes from the current
    //     ///     stream and advances the position within the stream by the number of bytes
    //     ///     read.
    //     /// </summary>
    //     /// <returns></returns>
    //     public override int Read(byte[] buffer, int offset, int count)
    //     {
    //         return _stream.Read(buffer, offset, count);
    //     }
    //
    //     /// <summary>
    //     /// Sets the position within the current
    //     ///     stream.
    //     /// </summary>
    //     /// <returns></returns>
    //     public override long Seek(long offset, SeekOrigin origin)
    //     {
    //         if (origin == SeekOrigin.Begin)
    //         {
    //             return _stream.Seek(offset + _offset, origin);
    //         }
    //         return _stream.Seek(offset, origin);
    //     }
    //
    //     /// <summary>
    //     /// Sets the length of the current stream.
    //     /// </summary>
    //     public override void SetLength(long value)
    //     {
    //         _stream.SetLength(value + _offset);
    //     }
    //
    //     /// <summary>
    //     ///  Writes a sequence of bytes to the current
    //     ///     stream and advances the current position within this stream by the number
    //     ///     of bytes written.
    //     /// </summary>
    //     public override void Write(byte[] buffer, int offset, int count)
    //     {
    //         _stream.Write(buffer, offset, count);
    //     }
    // }
}