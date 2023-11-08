using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;

namespace OrbisGL.Storage
{
    internal static class OrbisSaveDataIntrop
    {
        public enum OrbisSaveDataBlocks : ulong
        {
            SIZE = 32768,
            MIN2 = 96,
            MAX = 32768
        }

        [Flags]
        public enum OrbisSaveDataMountMode : uint
        {
            RDONLY = 0x00000001,
            RDWR = 0x00000002,
            CREATE_OR_FAIL = 0x00000004,
            DESTRUCT_OFF = 0x00000008,
            COPY_ICON = 0x00000010,
            CREATE_OR_OPEN = 0x00000020,
        }

        public enum OrbisSaveDataParamType : uint
        {
            ALL = 0,
            TITLE = 1,
            SUB_TITLE = 2,
            DETAIL = 3,
            USER_PARAM = 4,
            MTIME = 5
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct OrbisSaveDataDelete
        {	
            public int userId;
            public int unused1;
            [MarshalAs(UnmanagedType.LPStruct)]
            public OrbisSaveDataTitleId? titleId;
            [MarshalAs(UnmanagedType.LPStruct)]
            public OrbisSaveDataDirName? dirName;
            public uint unused2;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] reserved;
            public int unused3;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct UnsafeOrbisSaveDataIcon
        {
            public byte* buf;

            /// <summary>
            /// Unused
            /// </summary>
            public long bufSize;

            public long dataSize;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            byte[] reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct OrbisSaveDataIcon
        {
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
            public byte[] buf;

            /// <summary>
            /// Unused
            /// </summary>
            public long bufSize;

            public long dataSize;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] reserved;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct OrbisSaveDataMountPoint
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string data;
        };
        
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct OrbisSaveDataTitleId
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
            public string data;
            
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] padding;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct OrbisSaveDataDirName {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string data;
        } 
        
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct OrbisSaveDataFingerprint
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 65)]
            public byte[] data;
            
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
            public byte[] padding;
        };
        
        public struct OrbisSaveDataMountResult {
            public OrbisSaveDataMountPoint mountPoint;
            public OrbisSaveDataBlocks requiredBlocks;
            public uint progress;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] reserved;
        };

        struct OrbisSaveDataMount
        {
            public int userID;

            [MarshalAs(UnmanagedType.LPStruct)] 
            public OrbisSaveDataTitleId? titleId;

            [MarshalAs(UnmanagedType.LPStruct)] 
            public OrbisSaveDataDirName? dirName;
            
            [MarshalAs(UnmanagedType.LPStruct)] 
            public OrbisSaveDataFingerprint? fingerprint;
            
            OrbisSaveDataBlocks blocks;
            OrbisSaveDataMountMode mountMode;
            
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            byte[] reserved;
        }
        public struct OrbisSaveDataMount2 {
            public int userId;
            public uint unused1;
            
            [MarshalAs(UnmanagedType.LPStruct)] 
            public OrbisSaveDataDirName? dirName;
            
            public OrbisSaveDataBlocks blocks;
            public OrbisSaveDataMountMode mountMode;
            
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] reserved;
            
            public uint unused2;
        }
        
        public struct OrbisSaveDataParam {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.ORBIS_SAVE_DATA_TITLE_MAXSIZE)]
            public string title;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.ORBIS_SAVE_DATA_SUBTITLE_MAXSIZE)]
            public string subTitle;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.ORBIS_SAVE_DATA_DETAIL_MAXSIZE)]
            public string detail;
            public uint userParam;
            public int unused;
            public long mtime;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] reserved;
        };

        public static IntPtr[] CopyTo(this OrbisSaveDataMountPoint Point, out IntPtr Addr)
        {
            using (var Stream = new MemoryStream())
            {
                var Ptrs = Point.CopyTo(Stream).ToList();
                Addr = Marshal.AllocHGlobal((int)Stream.Length);
                Marshal.Copy(Stream.ToArray(), 0, Addr, (int)Stream.Length);
                
                Ptrs.Add(Addr);

                return Ptrs.ToArray();
            }
        }
    }
}