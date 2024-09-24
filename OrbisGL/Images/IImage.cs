using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrbisGL.Images
{
    public interface IDecodedImage : IDisposable
    {
        int Width { get; }
        int Height { get; }
        int BitsPerPixel { get; }
        int PixelDataLength { get; }

        void CopyPixelDataTo(Span<byte> Buffer);

        /// <summary>
        /// Get the converted RGBA pixel data or convert it if needed.
        /// <para>For background conversion use the <see cref="ConvertToRGBA"/></para>
        /// </summary>
        /// <param name="Buffer"></param>
        void CopyAsRGBA(Span<byte> Buffer);

        /// <summary>
        /// Converts the pixel data to RGBA in the current thread.
        /// If the pixel data has no alpha channel, the data will be just copied as is
        /// <para>To get the converted data use the <see cref="CopyAsRGBA(Span{byte})"/></para>
        /// </summary>
        void ConvertToRGBA();
    }
}
