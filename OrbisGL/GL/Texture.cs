using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Runtime.CompilerServices;
using Microsoft.SqlServer.Server;
using SharpGLES;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OrbisGL.GL
{
    public class Texture : IDisposable
    {
        private int CurrentTextureSize;
        
        private static List<int> SlotQueue = new List<int>(32);

        private static List<int> SlotTexture = new List<int>(32);

        public int Width { get; private set; }
        public int Height { get; private set; }

        static Texture()
        {
            for (int i = 0; i < 32; i++)
                SlotQueue.Add(i);

            for (int i = 0; i < 32; i++)
                SlotTexture.Add(0);
        }

        private int _TextureID;
        internal int TextureID {
            get
            {
                if (_TextureID == -1)
                    throw new ObjectDisposedException(nameof(Texture));
                
                return _TextureID;
            }
        }
        
        private int TextureType;

        public bool Is2D => TextureType == GLES20.GL_TEXTURE_2D;

        public Texture(bool Is2DTexture)
        {
            TextureType = Is2DTexture ? GLES20.GL_TEXTURE_2D : GLES20.GL_TEXTURE;

            int[] Textures = new int[1];
            GLES20.GenTextures(1, Textures);
            _TextureID = Textures.First();
        }

        ~Texture()
        {
            Dispose();
        }

        private void Bind(int Slot)
        {
            GLES20.GetLastError();
            
            GLES20.ActiveTexture(GLES20.GL_TEXTURE0 + Slot);
            GLES20.BindTexture(TextureType, TextureID);

            int Error = GLES20.GetLastError();

            if (Error != GLES20.GL_NO_ERROR)
            {
                throw new Exception("GL TEXBIND ERROR: 0x" + Error.ToString("X8"));
            }
        }


        /// <summary>
        /// Set the Texture from raw pixel data
        /// </summary>
        /// <param name="Width">Texture Width</param>
        /// <param name="Height">Texture Height</param>
        /// <param name="Data">Pixel Data</param>
        /// <param name="Format">Pixel Data Format</param>
        public unsafe void SetData(int Width, int Height, byte[] Data, PixelFormat Format, bool EnableFiltering)
        {
            if (Width * Height > Constants.ORBIS_MAX_TEXTURE_SIZE * Constants.ORBIS_MAX_TEXTURE_SIZE)
                throw new NotSupportedException($"Texture Resolution can't be higher than {Constants.ORBIS_MAX_TEXTURE_SIZE}x{Constants.ORBIS_MAX_TEXTURE_SIZE}");
            
            Bind(Active());

            this.Width = Width;
            this.Height = Height;

            if (CurrentTextureSize > 0)
                GC.RemoveMemoryPressure(CurrentTextureSize);

            GLES20.GetError(); //Clear any old error

            fixed (byte* pData = Data)
            {
                GLES20.TexImage2D(TextureType, 0, (int)Format, Width, Height, 0, (int)Format, GLES20.GL_UNSIGNED_BYTE, new IntPtr(pData));
                
                int Error = GLES20.GetError();

                if (Error != GLES20.GL_NO_ERROR)
                    throw new Exception($"GL ERROR 0x{Error:X8}");
                
                CurrentTextureSize = Data.Length;
                GC.AddMemoryPressure(CurrentTextureSize);
                
                if (EnableFiltering)
                {
                    GLES20.TexParameteri(TextureType, GLES20.GL_TEXTURE_MAG_FILTER, GLES20.GL_LINEAR);
                    GLES20.TexParameteri(TextureType, GLES20.GL_TEXTURE_MIN_FILTER, GLES20.GL_LINEAR_MIPMAP_LINEAR);
                    GLES20.GenerateMipmap(TextureType);
                } 
                else
                {
                    GLES20.TexParameteri(TextureType, GLES20.GL_TEXTURE_MAG_FILTER, GLES20.GL_NEAREST);
                    GLES20.TexParameteri(TextureType, GLES20.GL_TEXTURE_MIN_FILTER, GLES20.GL_NEAREST);
                }
            }
        }
        public unsafe void SetDataCompressed(int Width, int Height, byte[] Data, TextureCompressionFormats Format, bool EnableFiltering)
        {
            if (Width * Height > Constants.ORBIS_MAX_TEXTURE_SIZE * Constants.ORBIS_MAX_TEXTURE_SIZE)
                throw new NotSupportedException($"Texture Resolution can't be higher than {Constants.ORBIS_MAX_TEXTURE_SIZE}x{Constants.ORBIS_MAX_TEXTURE_SIZE}");

            Bind(Active());
            
            this.Width = Width;
            this.Height = Height;

            fixed (byte* pData = Data)
            {
                int blockSize;
                switch (Format)
                {
                    case TextureCompressionFormats.RGB_S3TC_DXT1_EXT:
                    case TextureCompressionFormats.RGBA_S3TC_DXT1_EXT:
                        blockSize = 8;
                        break;
                    default:
                        blockSize = 16;
                        break;
                }
                
                int TexSize = ((Width + 3) / 4) * ((Height + 3) / 4) * blockSize;

                GLES20.CompressedTexImage2D(TextureType, 0, (int)Format, Width, Height, 0, TexSize, new IntPtr(pData));

                int Error = GLES20.GetLastError();

                if (Error != GLES20.GL_NO_ERROR)
                    throw new Exception($"GL ERROR 0x{Error:X8}");
                
                CurrentTextureSize = TexSize;
                GC.AddMemoryPressure(CurrentTextureSize);

                if (EnableFiltering)
                {
                    GLES20.TexParameteri(TextureType, GLES20.GL_TEXTURE_MAG_FILTER, GLES20.GL_LINEAR);
                    GLES20.TexParameteri(TextureType, GLES20.GL_TEXTURE_MIN_FILTER, GLES20.GL_LINEAR);
                }
                else
                {
                    GLES20.TexParameteri(TextureType, GLES20.GL_TEXTURE_MAG_FILTER, GLES20.GL_NEAREST);
                    GLES20.TexParameteri(TextureType, GLES20.GL_TEXTURE_MIN_FILTER, GLES20.GL_NEAREST);
                }
            }
        }

        [Obsolete("Slow, use SetDDS instead", false)]
        public void SetImage(byte[] Data, PixelFormat TextureFormat, bool EnableFiltering)
        {
            int Width, Height;
            byte[] Buffer;
            switch (TextureFormat)
            {
                case PixelFormat.RGB:
                    var Img24 = Image.Load<Rgb24>(Data);
                    Width = Img24.Width;
                    Height = Img24.Height;
                    Buffer = new byte[Width * Height * 3];
                    Img24.CopyPixelDataTo(Buffer);
                    break;
                case PixelFormat.RGBA:
                    var Img32 = Image.Load<Rgba32>(Data);
                    Width = Img32.Width;
                    Height = Img32.Height;
                    Buffer = new byte[Width * Height * 4];
                    Img32.CopyPixelDataTo(Buffer);
                    break;

                default:
                    throw new Exception("Unexpected Pixel Format");
            }
            SetData(Width, Height, Buffer, TextureFormat, EnableFiltering);
        }

        /// <summary>
        /// Set the texture data from a DDS file
        /// </summary>
        /// <param name="File">DDS File Stream</param>
        public void SetDDS(Stream File, bool EnableFiltering)
        {
            File.Position = 0;
            using (var Reader = new BinaryReader(File))
            {
                var Magic = Reader.ReadInt32();
                if (Magic != 0x20534444)//DDS
                    throw new NotSupportedException("The given file isn't in DDS format");

                long DataOffset = Reader.ReadInt32() + 4;

                DDS_FLAGS Flags = (DDS_FLAGS)Reader.ReadUInt32();

                if (!Flags.HasFlag(DDS_FLAGS.WIDTH) || !Flags.HasFlag(DDS_FLAGS.HEIGHT) || !Flags.HasFlag(DDS_FLAGS.PIXELFORMAT))
                    throw new NotSupportedException("The given DDS file must contain Width, Height and Pixel Format Information");

                int Height = Reader.ReadInt32();
                int Width = Reader.ReadInt32();
                uint PitchOrLinearSize = Reader.ReadUInt32();
                uint Depth = Reader.ReadUInt32();
                uint MipMapCount = Reader.ReadUInt32();

                if (MipMapCount > 1)
                    throw new NotImplementedException("DDS mipmap Support Not Implemented");

                for (uint i = 0; i < 11; i++)
                    _ = Reader.ReadUInt32();//Reserved


                uint PixelFormatSize = Reader.ReadUInt32();

                if (PixelFormatSize != 32)
                    throw new NotSupportedException("Unexpected DDS Pixel Format Structure Size");

                DDS_FORMAT_FLAGS PFFLAGS = (DDS_FORMAT_FLAGS)Reader.ReadUInt32();

                bool Compressed = PFFLAGS.HasFlag(DDS_FORMAT_FLAGS.FOURCC);

                uint FOURCC = Reader.ReadUInt32();

                uint RGBBitCount = Reader.ReadUInt32();

                uint RBitMask = Reader.ReadUInt32();
                uint GBitMask = Reader.ReadUInt32();
                uint BBitMask = Reader.ReadUInt32();
                uint ABitMask = Reader.ReadUInt32();

                Reader.BaseStream.Position = DataOffset;

                byte[] PixelData = new byte[Reader.BaseStream.Length - DataOffset];
                Reader.Read(PixelData, 0, PixelData.Length);

                if (Compressed) 
                {
                    TextureCompressionFormats Format;

                    switch (FOURCC)
                    {
                        case 0x31545844://DXT1
                            Format = TextureCompressionFormats.RGB_S3TC_DXT1_EXT;
                            break;
                        case 0x35545844://DXT5
                            Format = TextureCompressionFormats.RGBA_S3TC_DXT5_EXT;
                            break;
                        default:
                            throw new NotSupportedException("DDS Compression Format Not Supported");
                    }

                    SetDataCompressed(Width, Height, PixelData, Format, EnableFiltering);
                }
                else 
                {
                    if (!PFFLAGS.HasFlag(DDS_FORMAT_FLAGS.RGB))
                        throw new NotSupportedException("The given DDS file pixel format isn't supported");

                    bool HasAlpha = PFFLAGS.HasFlag(DDS_FORMAT_FLAGS.ALPHA);

                    if (RGBBitCount == 24 && !HasAlpha)
                        ConvertPixelData(PixelData, RBitMask, GBitMask, BBitMask, 0, RGBBitCount);
                    else if (RGBBitCount == 32 && HasAlpha)
                        ConvertPixelData(PixelData, RBitMask, GBitMask, BBitMask, ABitMask, RGBBitCount);
                    else
                        throw new NotSupportedException("The given DDS file RGB pixel format isn't supported");

                    SetData(Width, Height, PixelData, HasAlpha ? PixelFormat.RGBA : PixelFormat.RGB, EnableFiltering);
                }
            }
        }
        private unsafe void ConvertPixelData(byte[] Data, uint RMask, uint GMask, uint BMask, uint AMask, uint BitPerPixel)
        {
            //TODO: TEST THIS SHIT

            uint PixelSize = BitPerPixel / 8;

            int RMove = 0, GMove = 0, BMove = 0, AMove = 0;
            bool RLeft = false, GLeft = false;

            switch (RMask)
            {
                case 0x00FF0000:
                    break;
                case 0x0000FF00:
                    RMove = 8;
                    RLeft = true;
                    break;
                case 0x000000FF:
                    RMove = 16;
                    RLeft = true;
                    break;
                case 0xFF000000:
                    RMove = 8;
                    break;
            }

            switch (GMask)
            {
                case 0x0000FF00:
                    break;
                case 0x00FF0000:
                    GMove = 8;
                    break;
                case 0x000000FF:
                    GMove = 8;
                    GLeft = true;
                    break;
                case 0xFF000000:
                    GMove = 16;
                    break;
            }

            switch (BMask)
            {
                case 0x000000FF:
                    break;
                case 0x00FF0000:
                    GMove = 16;
                    break;
                case 0x0000FF00:
                    GMove = 8;
                    break;
                case 0xFF000000:
                    GMove = 24;
                    break;
            }

            switch (AMask)
            {
                case 0xFF000000:
                    break;
                case 0x00FF0000:
                    AMove = 8;
                    break;
                case 0x0000FF00:
                    AMove = 16;
                    break;
                case 0x000000FF:
                    AMove = 24;
                    break;
            }

            if (RMove != 0 || GMove != 0 || BMove != 0)
            {
                fixed (byte* pPixel = Data)
                {
                    for (uint i = 0; i < Data.Length; i += PixelSize)
                    {
                        uint Pixel = *(uint*)(pPixel + i);

                        uint NewR = (RLeft ? ((Pixel & RMask) << RMove) : ((Pixel & RMask) >> RMove));
                        uint NewG = (GLeft ? ((Pixel & GMask) << GMove) : ((Pixel & GMask) >> GMove));
                        uint NewB = (Pixel & BMask) >> GMove;
                        uint NewA = (Pixel & AMask) << AMove;

                        *(uint*)(pPixel + i) = NewR | NewG | NewB | NewA;
                    }
                }
            }
        }

        /// <summary>
        /// Select the oldest texture slot, activate this texture and bind it
        /// </summary>
        /// <returns>The used texture slot</returns>
        public int Active()
        {
            if (SlotTexture.Contains(TextureID))
            {
                var Slot = SlotTexture.IndexOf(TextureID);
                
                if (SlotQueue.Contains(Slot))
                    SlotQueue.Remove(Slot);

                SlotQueue.Add(Slot);
                return Slot;
            }
            
            var ActiveSlot = SlotQueue.First();
            SlotQueue.RemoveAt(0);
            SlotQueue.Add(ActiveSlot);

            SlotTexture[ActiveSlot] = TextureID;

            Bind(ActiveSlot);

            return ActiveSlot;
        }
        
        public void Dispose()
        {
            if (CurrentTextureSize > 0)
            {
                GC.RemoveMemoryPressure(CurrentTextureSize);
                CurrentTextureSize = 0;
            }

            if (_TextureID == -1)
                return;
            
            int[] Textures = new int[] { TextureID };
            GLES20.DeleteTextures(Textures.Length, Textures);

            _TextureID = -1;
        }
    }
}