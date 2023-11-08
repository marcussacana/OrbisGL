using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using static OrbisGL.Storage.OrbisSaveDataIntrop;

namespace OrbisGL.Storage
{
    internal static class OrbisSaveDataDialogInterop
    {
        public const int ORBIS_SAVE_DATA_DIALOG_USER_MSG_MAXSIZE = 256;

        public enum OrbisSaveDataDialogMode : int
        {
            INVALID = 0,
            LIST = 1,
            USER_MSG = 2,
            SYSTEM_MSG = 3,
            ERROR_CODE = 4,
            PROGRESS_BAR = 5
        }

        public enum OrbisSaveDataDialogType : int
        {
            INVALID = 0,
            SAVE = 1,
            LOAD = 2,
            DELETE = 3
        }

        public enum OrbisSaveDataDialogFocusPos : int
        {
            LISTHEAD = 0,
            LISTTAIL = 1,
            DATAHEAD = 2,
            DATATAIL = 3,
            DATALATEST = 4,
            DATAOLDEST = 5,
            DIRNAME = 6
        }

        public enum OrbisSaveDataDialogSystemMessageType : int
        {
            INVALID = 0,
            NODATA = 1,
            CONFIRM = 2,
            OVERWRITE = 3,
            NOSPACE = 4,
            PROGRESS = 5,
            CORRUPTED = 6,
            FINISHED = 7,
            CONTINUABLE = 8,
            CORRUPTED_AND_DELETE = 10,
            CORRUPTED_AND_CREATE = 11,
            CORRUPTED_AND_RESTORE = 13,
            TOTAL_SIZE_EXCEEDED = 14
        }

        public enum OrbisSaveDataDialogButtonType : int
        {
            OK = 0,
            YESNO = 1,
            NONE = 2,
            OKCANCEL = 3
        }

        public enum OrbisSaveDataDialogButtonId : int
        {
            INVALID = 0, 
            OK = 1,
            YES = 1,
            NO = 2
        }

        public enum OrbisSaveDataDialogOptionBack : int
        {
            ENABLE = 0,
            DISABLE = 1
        }

        public enum OrbisSaveDataDialogProgressBarType : int
        {
            PERCENTAGE = 0
        }

        public enum OrbisSaveDataDialogProgressBarTarget : int
        {
            DEFAULT = 0
        }

        public enum OrbisSaveDataDialogItemStyle : int 
        {
            TITLE_DATESIZE_SUBTITLE = 0,
            TITLE_SUBTITLE_DATESIZE = 1,
            TITLE_DATESIZE = 2
        }

        public enum OrbisSaveDataDialogAnimation : int
        {
            ON = 0,
            OFF = 1
        }

        public enum OrbisSaveDataDialogUserMessageType : int
        {
            NORMAL = 0,
            ERROR = 1
        }

        public enum OrbisSaveDataDialogProgressSystemMessageType : int
        {
            INVALID = 0,
            PROGRESS = 1,
            RESTORE = 2
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct OrbisSaveDataDialogAnimationParam
        {
            public OrbisSaveDataDialogAnimation userOK;
            public OrbisSaveDataDialogAnimation userCancel;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct OrbisSaveDataDialogNewItem
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string title;
            
            [MarshalAs(UnmanagedType.LPArray)]
            public byte[] iconBuf;
            
            public ulong iconSize;
            
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] reserved;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct OrbisSaveDataDialogItems
        {
            public int userId;
            public int unusedValue1;

            [MarshalAs(UnmanagedType.LPStruct)]
            public OrbisSaveDataTitleId? titleId;

            [MarshalAs(UnmanagedType.LPStruct)]
            public OrbisSaveDataDirName? dirName;

            public uint dirNameNum;
            public int unusedValue2;

            [MarshalAs(UnmanagedType.LPStruct)]
            public OrbisSaveDataDialogNewItem? newItem;

            public OrbisSaveDataDialogFocusPos focusPos;
            public int unusedValue3;

            [MarshalAs(UnmanagedType.LPStruct)]
            public OrbisSaveDataDirName? focusPosDirName;

            public OrbisSaveDataDialogItemStyle itemStyle;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
            public byte[] reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct OrbisSaveDataDialogUserMessageParam
        {
            public OrbisSaveDataDialogButtonType buttonType;
            public OrbisSaveDataDialogUserMessageType msgType;
            
            [MarshalAs(UnmanagedType.LPStr)]
            public string msg;
            
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct OrbisSaveDataDialogSystemMessageParam
        {
            public OrbisSaveDataDialogSystemMessageType sysMsgType;
            public int unusedField;
            public ulong value;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct OrbisSaveDataDialogErrorCodeParam
        {
            public int errorCode;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct OrbisSaveDataDialogProgressBarParam
        {
            public OrbisSaveDataDialogProgressBarType barType;
            public IntPtr msg;
            public OrbisSaveDataDialogProgressSystemMessageType sysMsgType;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)]
            public byte[] reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct OrbisSaveDataDialogOptionParam
        {
            public OrbisSaveDataDialogOptionBack back;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct OrbisSaveDataDialogCloseParam
        {
            public OrbisSaveDataDialogAnimation anim;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct OrbisSaveDataDialogResult
        {
            public OrbisSaveDataDialogMode mode;
            
            /// <summary>
            /// 0 = OK, 1 = Cancel
            /// </summary>
            public int result;
            
            public OrbisSaveDataDialogButtonId buttonId;
            public int unused;
            public OrbisSaveDataDirName dirName;
            public OrbisSaveDataParam param;
            public IntPtr userData;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct OrbisCommonDialogBaseParam
        {
            public ulong size;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
            public byte[] reserved;
            public uint magic;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct OrbisSaveDataDialogParam
        {
            public OrbisCommonDialogBaseParam baseParam;
            public int size;
            public OrbisSaveDataDialogMode mode;
            public OrbisSaveDataDialogType dispType;
            public int unusedField;

            [MarshalAs(UnmanagedType.LPStruct)]
            public OrbisSaveDataDialogAnimationParam? animParam;

            [MarshalAs(UnmanagedType.LPStruct)]
            public OrbisSaveDataDialogItems? items;

            [MarshalAs(UnmanagedType.LPStruct)]
            public OrbisSaveDataDialogUserMessageParam? userMsgParam;

            [MarshalAs(UnmanagedType.LPStruct)]
            public OrbisSaveDataDialogSystemMessageParam? sysMsgParam;

            [MarshalAs(UnmanagedType.LPStruct)]
            public OrbisSaveDataDialogErrorCodeParam? errorCodeParam;

            [MarshalAs(UnmanagedType.LPStruct)]
            public OrbisSaveDataDialogProgressBarParam? progBarParam;

            public IntPtr userData;

            [MarshalAs(UnmanagedType.LPStruct)]
            public OrbisSaveDataDialogOptionParam? optionParam;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
            public byte[] reserved;
        }

        static void OrbisCommonDialogBaseParamInit(ref OrbisCommonDialogBaseParam param, IntPtr paramAddr)
        {
            param.size = 0x30;
            param.reserved = new byte[36];
            
            unchecked {
                param.magic = (uint)(paramAddr.ToInt64() + Constants.ORBIS_COMMON_DIALOG_MAGIC_NUMBER);
            }
        }

        internal static IEnumerable<IntPtr> CopyTo(this OrbisSaveDataDialogParam param, out IntPtr Addr)
        {
            param.size = 0x98;
            Addr = Marshal.AllocHGlobal(param.size);
            OrbisCommonDialogBaseParamInit(ref param.baseParam, Addr);

            List<IntPtr> Addrs = new List<IntPtr>();

            using (var Stream = new MemoryStream())
            {
                foreach (var subAddr in param.CopyTo(Stream))
                    Addrs.Add(subAddr);

                var Data = Stream.ToArray();
                Marshal.Copy(Data, 0, Addr, Data.Length);
            }

            Addrs.Add(Addr);

            return Addrs;
        }
    }

}
