using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OrbisGL.Images
{
    internal class DecodedImage<T> : IDecodedImage where T : unmanaged, IPixel<T>
    {
        byte[] RGBAData = null;
        Image<T> Obj;
        public DecodedImage(Image<T> Image) 
        {
            Obj = Image;
        }

        public int Width => Obj.Width;

        public int Height => Obj.Height;

        public int BitsPerPixel => Obj.PixelType.BitsPerPixel;

        public int PixelDataLength => Width * Height * (BitsPerPixel / 8);

        public unsafe void ConvertToRGBA()
        {
            RGBAData = new byte[PixelDataLength];
            CopyPixelDataTo(RGBAData);

            if (BitsPerPixel == 32)
                PixelData.Convert(RGBAData, 0xFF000000, 0x00FF0000, 0x0000FF00, 0x000000FF, (uint)BitsPerPixel);
        }

        public void CopyPixelDataTo(Span<byte> Buffer)
        {
            Obj.CopyPixelDataTo(Buffer);
        }

        public void CopyAsRGBA(Span<byte> Buffer)
        {
            if (RGBAData == null)
                ConvertToRGBA();

            Span<byte> Source = RGBAData;
            Source.CopyTo(Buffer);
        }

        public void Dispose()
        {
            RGBAData = null;
            Obj.Dispose();
        }
    }
}
