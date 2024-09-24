using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OrbisGL.Images
{
    public class PixelData
    {
        /// <summary>
        /// Covert pixel data to RGBA or RGB by using masks of the source format
        /// </summary>
        /// <param name="Data">Raw Pixel Data to be Converted</param>
        /// <param name="RMask">Source R Channel Mask</param>
        /// <param name="GMask">Source G Channel Mask</param>
        /// <param name="BMask">Source B Channel Mask</param>
        /// <param name="AMask">Source Alpha Channel Mask</param>
        /// <param name="BitPerPixel">Bits per pixel</param>
        public unsafe static void Convert(Span<byte> Data, uint RMask, uint GMask, uint BMask, uint AMask, uint BitPerPixel)
        {
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
                    BMove = 16;
                    break;
                case 0x0000FF00:
                    BMove = 8;
                    break;
                case 0xFF000000:
                    BMove = 24;
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
                    if (RMove == GMove && GMove == BMove && RLeft == GLeft)
                    {
                        var RGBMask = RMask | GMask | BMask;
                        ApplyRGBMask(Data.Length, pPixel, PixelSize, RMove, AMove, RLeft, RGBMask, AMask);
                    }
                    else
                    {
                        ApplyMasks(Data.Length, pPixel, RMask, GMask, BMask, AMask, PixelSize, RMove, GMove, BMove, AMove, RLeft, GLeft);
                    }
                }
            }
        }

        #region ChannelMask
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void ApplyMasks(int DataLen, byte* pPixel, uint RMask, uint GMask, uint BMask, uint AMask, uint PixelSize, int RMove, int GMove, int BMove, int AMove, bool RLeft, bool GLeft)
        {
            ulong lRMask = RMask | ((ulong)RMask << 32);
            ulong lGMask = GMask | ((ulong)GMask << 32);
            ulong lBMask = BMask | ((ulong)BMask << 32);
            ulong lAMask = AMask | ((ulong)AMask << 32);

            uint i;

            if (RLeft && GLeft)
            {
                i = ApplyRGLeft(DataLen, pPixel, PixelSize, RMove, GMove, BMove, AMove, lRMask, lGMask, lBMask, lAMask);
            }
            else if (RLeft && !GLeft)
            {
                i = ApplyRLeft(DataLen, pPixel, PixelSize, RMove, GMove, BMove, AMove, lRMask, lGMask, lBMask, lAMask);
            }
            else if (!RLeft && GLeft)
            {
                i = ApplyGLeft(DataLen, pPixel, PixelSize, RMove, GMove, BMove, AMove, lRMask, lGMask, lBMask, lAMask);
            }
            else
            {
                i = Apply(DataLen, pPixel, PixelSize, RMove, GMove, BMove, AMove, lRMask, lGMask, lBMask, lAMask);
            }

            for (; i < DataLen; i += PixelSize)
            {
                uint Pixel = *(uint*)(pPixel + i);

                uint NewR = (RLeft ? ((Pixel & RMask) << RMove) : ((Pixel & RMask) >> RMove));
                uint NewG = (GLeft ? ((Pixel & GMask) << GMove) : ((Pixel & GMask) >> GMove));
                uint NewB = (Pixel & BMask) >> BMove;
                uint NewA = (Pixel & AMask) << AMove;

                *(uint*)(pPixel + i) = NewR | NewG | NewB | NewA;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint Apply(int DataLen, byte* pPixel, uint PixelSize, int RMove, int GMove, int BMove, int AMove, ulong lRMask, ulong lGMask, ulong lBMask, ulong lAMask)
        {
            uint i;
            for (i = 0; i < DataLen - 1; i += PixelSize * 2)
            {
                ulong Pixel = *(ulong*)(pPixel + i);

                ulong NewR = (Pixel & lRMask) >> RMove;
                ulong NewG = (Pixel & lGMask) >> GMove;
                ulong NewB = (Pixel & lBMask) >> BMove;
                ulong NewA = (Pixel & lAMask) << AMove;

                *(ulong*)(pPixel + i) = NewR | NewG | NewB | NewA;
            }

            return i;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint ApplyGLeft(int DataLen, byte* pPixel, uint PixelSize, int RMove, int GMove, int BMove, int AMove, ulong lRMask, ulong lGMask, ulong lBMask, ulong lAMask)
        {
            uint i;
            for (i = 0; i < DataLen - 1; i += PixelSize * 2)
            {
                ulong Pixel = *(ulong*)(pPixel + i);

                ulong NewR = (Pixel & lRMask) >> RMove;
                ulong NewG = (Pixel & lGMask) << GMove;
                ulong NewB = (Pixel & lBMask) >> BMove;
                ulong NewA = (Pixel & lAMask) << AMove;

                *(ulong*)(pPixel + i) = NewR | NewG | NewB | NewA;
            }

            return i;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint ApplyRLeft(int DataLen, byte* pPixel, uint PixelSize, int RMove, int GMove, int BMove, int AMove, ulong lRMask, ulong lGMask, ulong lBMask, ulong lAMask)
        {
            uint i;
            for (i = 0; i < DataLen - 1; i += PixelSize * 2)
            {
                ulong Pixel = *(ulong*)(pPixel + i);

                ulong NewR = (Pixel & lRMask) << RMove;
                ulong NewG = (Pixel & lGMask) >> GMove;
                ulong NewB = (Pixel & lBMask) >> BMove;
                ulong NewA = (Pixel & lAMask) << AMove;

                *(ulong*)(pPixel + i) = NewR | NewG | NewB | NewA;
            }

            return i;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint ApplyRGLeft(int DataLen, byte* pPixel, uint PixelSize, int RMove, int GMove, int BMove, int AMove, ulong lRMask, ulong lGMask, ulong lBMask, ulong lAMask)
        {
            uint i;
            for (i = 0; i < DataLen - 1; i += PixelSize * 2)
            {
                ulong Pixel = *(ulong*)(pPixel + i);

                ulong NewR = (Pixel & lRMask) << RMove;
                ulong NewG = (Pixel & lGMask) << GMove;
                ulong NewB = (Pixel & lBMask) >> BMove;
                ulong NewA = (Pixel & lAMask) << AMove;

                *(ulong*)(pPixel + i) = NewR | NewG | NewB | NewA;
            }

            return i;
        }
        #endregion

        #region RBGMask

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void ApplyRGBMask(int DataLen, byte* pPixel, uint PixelSize, int RGBMove, int AMove, bool RGBLeft, uint RGBMask, uint AMask)
        {
            ulong lRGBMask = RGBMask | ((ulong)RGBMask << 32);
            ulong lAMask = AMask | ((ulong)AMask << 32);

            var i = 0u;

            if (RGBLeft)
            {
                i = ApplyRGBLeft(DataLen, pPixel, PixelSize, RGBMove, AMove, lRGBMask, lAMask);
            }
            else
            {
                i = ApplyRGBRight(DataLen, pPixel, PixelSize, RGBMove, AMove, lRGBMask, lAMask);
            }

            for (; i < DataLen; i += PixelSize)
            {
                uint Pixel = *(uint*)(pPixel + i);

                uint NewRGB = (RGBLeft ? ((Pixel & RGBMask) << RGBMove) : ((Pixel & RGBMask) >> RGBMove));
                uint NewA = (Pixel & AMask) << AMove;

                *(uint*)(pPixel + i) = NewRGB | NewA;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint ApplyRGBRight(int DataLen, byte* pPixel, uint PixelSize, int RGBMove, int AMove, ulong lRGBMask, ulong lAMask)
        {
            uint i;
            for (i = 0; i < DataLen - 1; i += PixelSize * 2)
            {
                ulong Pixel = *(ulong*)(pPixel + i);

                ulong NewRGB = (Pixel & lRGBMask) >> RGBMove;
                ulong NewA = (Pixel & lAMask) << AMove;

                *(ulong*)(pPixel + i) = NewRGB | NewA;
            }

            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint ApplyRGBLeft(int DataLen, byte* pPixel, uint PixelSize, int RGBMove, int AMove, ulong lRGBMask, ulong lAMask)
        {
            uint i;
            for (i = 0; i < DataLen - 1; i += PixelSize * 2)
            {
                ulong Pixel = *(ulong*)(pPixel + i);

                ulong NewRGB = (Pixel & lRGBMask) << RGBMove;
                ulong NewA = (Pixel & lAMask) << AMove;

                *(ulong*)(pPixel + i) = NewRGB | NewA;
            }

            return i;
        }
#endregion
    }
}
