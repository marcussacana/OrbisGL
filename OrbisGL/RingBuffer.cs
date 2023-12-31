﻿using System;
using System.IO;
using System.Threading;

namespace OrbisGL
{
    public sealed class RingBuffer : Stream
    {
        
        static RingBuffer()
        {
            //Early JIT the class for lower delay in the first usage
            new Thread(() => {
                using (var tmp = new RingBuffer(100))
                    tmp.Write(new byte[10], 0, 10);
            }).Start();
        }
        
        private int Size, ReadOffset, WriteOffset, BufferedAmount;
        private long ReadLoop, WriteLoop;

        byte[] DataBuffer;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        /// <summary>
        /// Get the total amount of data currently buffered in the ring buffer
        /// </summary>
        public override long Length => BufferedAmount;

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public RingBuffer(int Size) { 
            this.Size = Size;
            DataBuffer = new byte[Size];
        }

        ~RingBuffer()
        {
            DataBuffer = null;
        }

        public SemaphoreSlim RSemaphore = new SemaphoreSlim(1);
        public SemaphoreSlim WSemaphore = new SemaphoreSlim(1);

        /// <summary>
        /// Clear all ring buffer data
        /// </summary>
        public override void Flush()
        {
            RSemaphore.Wait();
            WSemaphore.Wait();
            BufferedAmount = 0;
            ReadOffset = 0;
            WriteOffset = 0;
            RSemaphore.Release();
            WSemaphore.Release();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            RSemaphore.Wait();

            try
            {
                return DoRead(buffer, offset, count);
            }
            finally { RSemaphore.Release(); }
        }

        private int DoRead(byte[] buffer, int offset, int count)
        {
            if (DataBuffer == null)
                return 0;

            if (ReadOffset >= Size)
            {
                ReadOffset = 0;
                ReadLoop++;
            }

            if (count > BufferedAmount)
                count = BufferedAmount;

            if (offset + count > buffer.Length)
                count = buffer.Length - offset;

            if (ReadOffset == WriteOffset && ReadLoop >= WriteLoop)
                return 0;

            int MaxBulkRead = Size - ReadOffset;
            int ReadAmount = Math.Min(count, MaxBulkRead);

            if (ReadOffset < WriteOffset)
            {
                MaxBulkRead = WriteOffset - ReadOffset;
                ReadAmount = Math.Min(ReadAmount, MaxBulkRead);
            }

            Array.Copy(DataBuffer, ReadOffset, buffer, offset, ReadAmount);

            count -= ReadAmount;
            ReadOffset += ReadAmount;
            BufferedAmount -= ReadAmount;

            if (count > 0)
                return DoRead(buffer, offset + ReadAmount, count) + ReadAmount;

            return ReadAmount;
        }

        public override void Write(byte[] buffer, int InOffset, int count)
        {
            Write(new Span<byte>(buffer), InOffset, count);
        }
        
        public void Write(Span<byte> buffer, int inOffset, int count)
        {
            if (count > Size)
                throw new ArgumentOutOfRangeException("count");

            if (WriteOffset >= Size)
            {
                WriteOffset = 0;
                WriteLoop++;
            }

            while (CantWrite(count))
            {
                Thread.Sleep(100);
            }

            WSemaphore.Wait();

            try
            {
                DoWrite(buffer, inOffset, count);
            } finally { WSemaphore.Release(); }
        }

        public bool CantWrite(int count)
        {
            return BufferedAmount + count >= Size;
        }

        private void DoWrite(Span<byte> buffer, int inOffset, int count)
        { 
           int bytesToWrite = Math.Min(count, Size - WriteOffset);

            buffer.Slice(inOffset, bytesToWrite).CopyTo(DataBuffer.AsSpan(WriteOffset));

            WriteOffset = (WriteOffset + bytesToWrite) % DataBuffer.Length;

            BufferedAmount += bytesToWrite;

            if (bytesToWrite < count)
            {
                DoWrite(buffer, inOffset + bytesToWrite, count - bytesToWrite);
            }
        }

        protected override void Dispose(bool disposing)
        {
            DataBuffer = null;
            base.Dispose(disposing);
        }
    }
}
