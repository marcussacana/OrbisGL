using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace OrbisGL.Storage
{
    public static class OrbisSaveDataDialogInterop
    {
        public const int ORBIS_SAVE_DATA_DIALOG_USER_MSG_MAXSIZE = 256;

        public enum OrbisSaveDataDialogMode : int
        {
            ORBIS_SAVE_DATA_DIALOG_MODE_INVALID = 0,
            ORBIS_SAVE_DATA_DIALOG_MODE_LIST = 1,
            ORBIS_SAVE_DATA_DIALOG_MODE_USER_MSG = 2,
            ORBIS_SAVE_DATA_DIALOG_MODE_SYSTEM_MSG = 3,
            ORBIS_SAVE_DATA_DIALOG_MODE_ERROR_CODE = 4,
            ORBIS_SAVE_DATA_DIALOG_MODE_PROGRESS_BAR = 5
        }

        public enum OrbisSaveDataDialogType : int
        {
            ORBIS_SAVE_DATA_DIALOG_TYPE_INVALID = 0,
            ORBIS_SAVE_DATA_DIALOG_TYPE_SAVE = 1,
            ORBIS_SAVE_DATA_DIALOG_TYPE_LOAD = 2,
            ORBIS_SAVE_DATA_DIALOG_TYPE_DELETE = 3
        }

        public enum OrbisSaveDataDialogFocusPos : int
        {
            ORBIS_SAVE_DATA_DIALOG_FOCUS_POS_LISTHEAD = 0,
            ORBIS_SAVE_DATA_DIALOG_FOCUS_POS_LISTTAIL = 1,
            ORBIS_SAVE_DATA_DIALOG_FOCUS_POS_DATAHEAD = 2,
            ORBIS_SAVE_DATA_DIALOG_FOCUS_POS_DATATAIL = 3,
            ORBIS_SAVE_DATA_DIALOG_FOCUS_POS_DATALATEST = 4,
            ORBIS_SAVE_DATA_DIALOG_FOCUS_POS_DATAOLDEST = 5,
            ORBIS_SAVE_DATA_DIALOG_FOCUS_POS_DIRNAME = 6
        }

        public enum OrbisSaveDataDialogSystemMessageType : int
        {
            ORBIS_SAVE_DATA_DIALOG_SYSMSG_TYPE_INVALID = 0,
            ORBIS_SAVE_DATA_DIALOG_SYSMSG_TYPE_NODATA = 1,
            ORBIS_SAVE_DATA_DIALOG_SYSMSG_TYPE_CONFIRM = 2,
            ORBIS_SAVE_DATA_DIALOG_SYSMSG_TYPE_OVERWRITE = 3,
            ORBIS_SAVE_DATA_DIALOG_SYSMSG_TYPE_NOSPACE = 4,
            ORBIS_SAVE_DATA_DIALOG_SYSMSG_TYPE_PROGRESS = 5,
            ORBIS_SAVE_DATA_DIALOG_SYSMSG_TYPE_FILE_CORRUPTED = 6,
            ORBIS_SAVE_DATA_DIALOG_SYSMSG_TYPE_FINISHED = 7,
            ORBIS_SAVE_DATA_DIALOG_SYSMSG_TYPE_NOSPACE_CONTINUABLE = 8,
            ORBIS_SAVE_DATA_DIALOG_SYSMSG_TYPE_CORRUPTED_AND_DELETE = 10,
            ORBIS_SAVE_DATA_DIALOG_SYSMSG_TYPE_CORRUPTED_AND_CREATE = 11,
            ORBIS_SAVE_DATA_DIALOG_SYSMSG_TYPE_CORRUPTED_AND_RESTORE = 13,
            ORBIS_SAVE_DATA_DIALOG_SYSMSG_TYPE_TOTAL_SIZE_EXCEEDED = 14
        }

        public enum OrbisSaveDataDialogButtonType : int
        {
            ORBIS_SAVE_DATA_DIALOG_BUTTON_TYPE_OK = 0,
            ORBIS_SAVE_DATA_DIALOG_BUTTON_TYPE_YESNO = 1,
            ORBIS_SAVE_DATA_DIALOG_BUTTON_TYPE_NONE = 2,
            ORBIS_SAVE_DATA_DIALOG_BUTTON_TYPE_OKCANCEL = 3
        }

        public enum OrbisSaveDataDialogButtonId : int
        {
            ORBIS_SAVE_DATA_DIALOG_BUTTON_ID_INVALID = 0,
            ORBIS_SAVE_DATA_DIALOG_BUTTON_ID_OK = 1,
            ORBIS_SAVE_DATA_DIALOG_BUTTON_ID_YES = 1,
            ORBIS_SAVE_DATA_DIALOG_BUTTON_ID_NO = 2
        }

        public enum OrbisSaveDataDialogOptionBack : int
        {
            ORBIS_SAVE_DATA_DIALOG_OPTION_BACK_ENABLE = 0,
            ORBIS_SAVE_DATA_DIALOG_OPTION_BACK_DISABLE = 1
        }

        public enum OrbisSaveDataDialogProgressBarType : int
        {
            ORBIS_SAVE_DATA_DIALOG_PROGRESSBAR_TYPE_PERCENTAGE = 0
        }

        public enum OrbisSaveDataDialogProgressBarTarget : int
        {
            ORBIS_SAVE_DATA_DIALOG_PROGRESSBAR_TARGET_BAR_DEFAULT = 0
        }

        public enum OrbisSaveDataDialogItemStyle : int 
        {
            ORBIS_SAVE_DATA_DIALOG_ITEM_STYLE_TITLE_DATESIZE_SUBTITLE = 0,
            ORBIS_SAVE_DATA_DIALOG_ITEM_STYLE_TITLE_SUBTITLE_DATESIZE = 1,
            ORBIS_SAVE_DATA_DIALOG_ITEM_STYLE_TITLE_DATESIZE = 2
        }

        public enum OrbisSaveDataDialogAnimation : int
        {
            ORBIS_SAVE_DATA_DIALOG_ANIMATION_ON = 0,
            ORBIS_SAVE_DATA_DIALOG_ANIMATION_OFF = 1
        }

        public enum OrbisSaveDataDialogUserMessageType : int
        {
            ORBIS_SAVE_DATA_DIALOG_USERMSG_TYPE_NORMAL = 0,
            ORBIS_SAVE_DATA_DIALOG_USERMSG_TYPE_ERROR = 1
        }

        public enum OrbisSaveDataDialogProgressSystemMessageType : int
        {
            ORBIS_SAVE_DATA_DIALOG_PRGRESS_SYSMSG_TYPE_INVALID = 0,
            ORBIS_SAVE_DATA_DIALOG_PRGRESS_SYSMSG_TYPE_PROGRESS = 1,
            ORBIS_SAVE_DATA_DIALOG_PRGRESS_SYSMSG_TYPE_RESTORE = 2
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
            public IntPtr msg;
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
            public int result;
            public OrbisSaveDataDialogButtonId buttonId;
            public IntPtr dirName;
            public IntPtr param;
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
