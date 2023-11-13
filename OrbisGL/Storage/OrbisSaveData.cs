using Orbis.Internals;
using System;
using System.Runtime.InteropServices;
using System.Text;
using static OrbisGL.Storage.OrbisSaveDataIntrop;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace OrbisGL.Storage
{
    public class OrbisSaveData : IStorageData
    {
        private bool Mounted = true;
        private OrbisSaveDataMountResult Mount;
        internal OrbisSaveData(string name, OrbisSaveDataMountResult mount, bool readOnly)
        {
            Name = name;
            Mount = mount;
            ReadOnly = readOnly;

            if (string.IsNullOrEmpty(mount.mountPoint.data))
                throw new Exception("Failed to mount the save");

            MountedPath = mount.mountPoint.data;
        }

        ~OrbisSaveData()
        {
            Unmount();
        }

        public string Name { get; private set; }
        public string MountedPath { get; private set; }
        
        public bool ReadOnly { get; private set; }

        public string AbsoluteMountedPath => Path.Combine(Path.GetDirectoryName(IO.GetAppBaseDirectory()), MountedPath.TrimStart('\\', '/'));

        /// <summary>
        /// Unmount the save data
        /// </summary>
        public void Unmount()
        {
            if (!Mounted)
                return;

            var Pointers = Mount.mountPoint.CopyTo(out IntPtr Addr);
            
            int rst = sceSaveDataUmount(Addr);

            foreach (var Pointer in Pointers)
                Marshal.FreeHGlobal(Pointer);

            if (rst < 0)
            {
                switch (rst)
                {
                    case unchecked((int)0x809f0003):
                        throw new Exception("Can't unmount an save in use.");
                    default:
                        throw new Exception($"Failed to unmount the save data, Error: 0x{rst:X8}");
                }
            }

            Mounted = false;
        }

        public unsafe void SetTitle(string Title)
        {
            if (string.IsNullOrEmpty(Title))
                throw new ArgumentNullException("title");

#if DEBUG
            Title += " [DEBUG]";
#endif
            
            var ptr = Kernel.AllocString(Title, out int Size);

            SetParamData(ptr, Size, OrbisSaveDataParamType.TITLE);
        }

        public unsafe void SetSubTitle(string Subtitle)
        {
            if (Subtitle == null)
                Subtitle = string.Empty;

            var ptr = Kernel.AllocString(Subtitle, out int Size);

            SetParamData(ptr, Size, OrbisSaveDataParamType.SUB_TITLE);
        }

        public unsafe void SetDetail(string Detail)
        {
            if (Detail == null)
                Detail = string.Empty;

            var ptr = Kernel.AllocString(Detail, out int Size);

            SetParamData(ptr, Size, OrbisSaveDataParamType.DETAIL);
        }

        public unsafe void SetUserData(byte[] Data)
        {
            if (Data == null)
                throw new ArgumentNullException("Data");

            var ptr = Kernel.malloc(Data.Length);
            var Size = Data.Length;

            Marshal.Copy(Data, 0, new IntPtr(ptr), Size);

            SetParamData(ptr, Size, OrbisSaveDataParamType.USER_PARAM);
        }

        public string GetTitle()
        {
            var Data = GetParamData(OrbisSaveDataParamType.TITLE);

            return Encoding.UTF8.GetString(Data);
        }

        public string GetSubtitle()
        {
            var Data = GetParamData(OrbisSaveDataParamType.SUB_TITLE);

            return Encoding.UTF8.GetString(Data);
        }

        public string GetDetail()
        {
            var Data = GetParamData(OrbisSaveDataParamType.DETAIL);

            return Encoding.UTF8.GetString(Data);
        }

        public byte[] GetUserData(int MaxSize)
        {
            return GetParamData(OrbisSaveDataParamType.USER_PARAM, MaxSize);
        }

        private unsafe byte[] GetParamData(OrbisSaveDataParamType Type)
        {
            return GetParamData(Type, GetMaxSize(Type));
        }

        private unsafe byte[] GetParamData(OrbisSaveDataParamType Type , int MaxSize)
        {
            if (MaxSize == -1)
                throw new Exception("Max Size must be specified in this param type");

            var ptr = Kernel.malloc(MaxSize);

            var Pointers = Mount.mountPoint.CopyTo(out IntPtr Addr);

            int rst = sceSaveDataGetParam(Addr, Type, ptr, MaxSize, out int RstSize);

            foreach (var Pointer in Pointers)
                Marshal.FreeHGlobal(Pointer);
            
            if (rst < 0)
            {
                Kernel.free(ptr);
                throw new Exception($"Failed to get the save data param, Error: 0x{rst:X8}");
            }

            byte[] Output = new byte[RstSize];
            Marshal.Copy(new IntPtr(ptr), Output, 0, Output.Length);

            return Output;
        }

        private unsafe void SetParamData(void* ptr, int Size, OrbisSaveDataParamType Type)
        {
            int MaxSize = GetMaxSize(Type);

            if (MaxSize != -1 && Size > MaxSize)
            {
                Kernel.free(ptr);
                throw new Exception($"Content length out of bounds, Length > {MaxSize}");
            }

            var Pointers = Mount.mountPoint.CopyTo(out IntPtr Addr);

            int rst = sceSaveDataSetParam(Addr, Type, ptr, Size);

            foreach (var Pointer in Pointers)
                Marshal.FreeHGlobal(Pointer);

            Kernel.free(ptr);

            if (rst < 0)
                throw new Exception($"Set Save Data Param Error: 0x{rst:X8}");
        }

        private unsafe int GetMaxSize(OrbisSaveDataParamType Type)
        {
            int MaxSize = -1;
            switch (Type)
            {
                case OrbisSaveDataParamType.TITLE:
                    MaxSize = Constants.ORBIS_SAVE_DATA_TITLE_MAXSIZE;
                    break;
                case OrbisSaveDataParamType.SUB_TITLE:
                    MaxSize = Constants.ORBIS_SAVE_DATA_SUBTITLE_MAXSIZE;
                    break;
                case OrbisSaveDataParamType.DETAIL:
                    MaxSize = Constants.ORBIS_SAVE_DATA_DETAIL_MAXSIZE;
                    break;
                case OrbisSaveDataParamType.MTIME:
                    MaxSize = 16;
                    break;
            }

            return MaxSize;
        }

        public void SetIcon(byte[] Data)
        {
            OrbisSaveDataIcon Icon = new OrbisSaveDataIcon
            {
                buf = Data,
                dataSize = Data.Length
            };

            List<IntPtr> Pointers;

            IntPtr Addr = IntPtr.Zero;

            using (MemoryStream Stream = new MemoryStream())
            {
                Pointers = Icon.CopyTo(Stream).ToList();

                Addr = Marshal.AllocHGlobal((int)Stream.Length);
                Marshal.Copy(Stream.ToArray(), 0, Addr, (int)Stream.Length);
            }

             Pointers.AddRange(Mount.mountPoint.CopyTo(out IntPtr MountAddr));

            int rst = sceSaveDataSaveIcon(MountAddr, Addr);

            foreach (var Pointer in Pointers)
                Marshal.FreeHGlobal(Pointer);

            Marshal.FreeHGlobal(Addr);

            if (rst < 0)
                throw new Exception($"Failed to Set the Save Icon, Error: 0x{rst:X8}");
        }

        public unsafe byte[] GetIcon(int BufferSize = 1024 * 1024 * 10)
        {
            UnsafeOrbisSaveDataIcon Icon = new UnsafeOrbisSaveDataIcon();
            Icon.buf = (byte*)Kernel.malloc(BufferSize);

            var Pointers = Mount.mountPoint.CopyTo(out IntPtr Addr);
            
            int rst = sceSaveDataLoadIcon(Addr, ref Icon);

            foreach (var Pointer in Pointers)
                Marshal.FreeHGlobal(Pointer);
            
            
            if (rst < 0)
            {
                Kernel.free(Icon.buf);
                throw new Exception($"Failed to Get the Save Icon, Error: 0x{rst:X8}");
            }

            var Data = new byte[Icon.dataSize];

            Marshal.Copy(new IntPtr(Icon.buf), Data, 0, Data.Length);
            
            Kernel.free(Icon.buf);

            return Data;
        }



        [DllImport("libSceSaveData.sprx")]
        static unsafe extern int sceSaveDataSetParam(IntPtr mountPoint, OrbisSaveDataParamType paramType, void* paramBuf, long paramBufSize);

        [DllImport("libSceSaveData.sprx")]
        static unsafe extern int sceSaveDataGetParam(IntPtr mountPoint, OrbisSaveDataParamType Type, void* paramBuf, long paramBufSize, out int rstSize);

        [DllImport("libSceSaveData.sprx")]
        static unsafe extern int sceSaveDataSaveIcon(IntPtr mountPoint, IntPtr param);

        [DllImport("libSceSaveData.sprx")]
        static unsafe extern int sceSaveDataLoadIcon(IntPtr mountPoint, ref UnsafeOrbisSaveDataIcon param);

        [DllImport("libSceSaveData.sprx")]
        static unsafe extern int sceSaveDataUmount(IntPtr mountPoint);
    }
}
