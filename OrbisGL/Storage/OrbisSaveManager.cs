using Orbis.Internals;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using static OrbisGL.Storage.OrbisSaveDataDialogInterop;
using static OrbisGL.Storage.OrbisSaveDataIntrop;

namespace OrbisGL.Storage
{
    public class OrbisSaveManager : IStorageManager<OrbisSaveData>
    {
        public Action DoEvents = null;

        static int UserID;
        static bool Initialized;

        private int MaxSaveSize;

        /// <summary>
        /// Initialize an Save Manager Instance
        /// </summary>
        /// <param name="MaxSaveSize">The max save size in bytes</param>
        public OrbisSaveManager(int MaxSaveSize)
        {
            this.MaxSaveSize = MaxSaveSize;
        }

        /// <summary>
        /// Creates an new save data
        /// </summary>
        /// <param name="Confirm">When true, a confirmation dialog is displayed</param>
        /// <param name="Indentifier">The save folder name</param>
        public OrbisSaveData Create(bool Confirm,  string Indentifier = null)
        {
            if (!Initialized)
                Initialize();

            if (Indentifier == null)
            {
                Indentifier = "SYSTEM";
            }

            bool Overwrite = false;
            if (Exists(Indentifier, true))
                Overwrite = true;

            if (Confirm)
            {
                var MsgType = Overwrite ? OrbisSaveDataDialogSystemMessageType.OVERWRITE : OrbisSaveDataDialogSystemMessageType.CONFIRM;
                
                if (!ConfirmDialog(Indentifier, OrbisSaveDataDialogType.SAVE, MsgType))
                    return null;
            }

            if (Overwrite)
                Delete(false, Indentifier);

            var Mount = new OrbisSaveDataMount2()
            {
                userId = UserID,
                dirName = new OrbisSaveDataDirName()
                {
                    data = Indentifier
                },
                mountMode = OrbisSaveDataMountMode.CREATE_OR_FAIL | OrbisSaveDataMountMode.RDWR | OrbisSaveDataMountMode.COPY_ICON,
                blocks = ComputeBlockSize(MaxSaveSize)
            };

            var Save = MountSave(Indentifier, Mount, out uint Error);

            if (Save == null)
                ParseError(Indentifier, Error);

            return Save;
        }

        private bool ConfirmDialog(string Indentifier, OrbisSaveDataDialogType DialogType = OrbisSaveDataDialogType.SAVE, OrbisSaveDataDialogSystemMessageType MessageType = OrbisSaveDataDialogSystemMessageType.CONFIRM)
        {

            IntPtr[] Pointers = new IntPtr[0];
            try
            {
                var Param = new OrbisSaveDataDialogParam()
                {
                    mode = OrbisSaveDataDialogMode.SYSTEM_MSG,
                    dispType = DialogType,
                    sysMsgParam = new OrbisSaveDataDialogSystemMessageParam()
                    {
                        sysMsgType = MessageType
                    },
                    items = new OrbisSaveDataDialogItems()
                    {
                        userId = UserID,
                        dirName = new OrbisSaveDataDirName()
                        {
                            data = Indentifier
                        },
                        dirNameNum = 1
                    }
                };

                Pointers = Param.CopyTo(out IntPtr Addr).ToArray();

                var diagResult = ShowSaveDialog(Addr);
                if (diagResult.result == 1 || diagResult.buttonId == OrbisSaveDataDialogButtonId.NO)
                    return false;
            }
            finally
            {
                foreach (var Pointer in Pointers)
                {
                    Marshal.FreeHGlobal(Pointer);
                }
            }

            return true;
        }

        private static OrbisSaveData MountSave(string Indentifier, OrbisSaveDataMount2 Mount, out uint Error)
        {
            Error = 0;
            
            IntPtr Addr;
            IntPtr[] Pointers;
            var Result = new OrbisSaveDataMountResult();

            Addr = IntPtr.Zero;
            using (var Stream = new MemoryStream())
            {
                Pointers = Mount.CopyTo(Stream).ToArray();
                Addr = Marshal.AllocHGlobal((int)Stream.Length);
                Marshal.Copy(Stream.ToArray(), 0, Addr, (int)Stream.Length);
            }

            try
            {
                int rst = sceSaveDataMount2(Addr, ref Result);

                Error = unchecked((uint)rst);

                if (rst < 0)
                    return null;
                
                return new OrbisSaveData(Indentifier, Result, Mount.mountMode.HasFlag(OrbisSaveDataMountMode.RDONLY));
            }
            finally
            {
                foreach (var Pointer in Pointers)
                {
                    Marshal.FreeHGlobal(Pointer);
                }

                Marshal.FreeHGlobal(Addr);
            }
        }

        private static OrbisSaveDataBlocks ComputeBlockSize(int MaxSize)
        {
            var Blocks = (ulong)MaxSize / (ulong)OrbisSaveDataBlocks.SIZE;
            if ((ulong)MaxSize % (ulong)OrbisSaveDataBlocks.SIZE != 0)
                Blocks++;

            Blocks = Math.Min(Blocks, (ulong)OrbisSaveDataBlocks.MAX);
            return (OrbisSaveDataBlocks)Math.Max(Blocks, (ulong)OrbisSaveDataBlocks.MIN2);
        }

        private OrbisSaveDataDialogResult ShowSaveDialog(IntPtr Param)
        {
            int Rst = sceSaveDataDialogOpen(Param);

            if (Rst != 0)
                throw new Exception($"Failed to Create the Save Dialog, ERROR: 0x{Rst:X8}");

            if (sceSaveDataDialogUpdateStatus() != 0)
            {
                while (sceSaveDataDialogUpdateStatus() != 3)
                {
                    DoEvents?.Invoke();
                    Thread.Sleep(30);
                }
            }

            OrbisSaveDataDialogResult Result = new OrbisSaveDataDialogResult();
            sceSaveDataDialogGetResult(ref Result);
            return Result;
        }

        public bool Delete(bool Confirm, string Indentifier = null)
        {
            if (!Initialized)
                Initialize();

            if (Indentifier == null)
            {
                Indentifier = "SYSTEM";
            }

            if (!Exists(Indentifier, true))
                return true;

            if (Confirm && !ConfirmDialog(Indentifier, OrbisSaveDataDialogType.DELETE))
                return false;

            var delParam = new OrbisSaveDataDelete()
            {
                userId = UserID,
                dirName = new OrbisSaveDataDirName()
                {
                    data = Indentifier
                }
            };

            using (var Stream = new MemoryStream())
            {
                var Pointers = delParam.CopyTo(Stream).ToArray();

                var Param = Marshal.AllocHGlobal((int)Stream.Length);
                
                Marshal.Copy(Stream.ToArray(), 0, Param, (int)Stream.Length);
                
                int rst = sceSaveDataDelete(Param);

                foreach (var Pointer in Pointers)
                    Marshal.FreeHGlobal(Pointer);
                
                Marshal.FreeHGlobal(Param);

                switch (unchecked((uint)rst))
                {
                    case 0x809f0003:
                        throw new Exception("Can't delete: save data is Mounted");
                    case 0x809f0011:
                        throw new Exception("Can't delete: Invalid UserID or Inactive User");
                    case 0x809f0013:
                        throw new Exception("Can't delete: Save busy for backup");
                }

                return true;
            }
        }

        public bool Exists(string Indentifier = null, bool AllowCorrupted = false)
        {
            if (!Initialized)
                Initialize();
            
            if (Indentifier == null)
            {
                Indentifier = "SYSTEM";
            }
            
            var Mount = new OrbisSaveDataMount2()
            {
                userId = UserID,
                dirName = new OrbisSaveDataDirName()
                {
                    data = Indentifier
                },
                mountMode = OrbisSaveDataMountMode.RDONLY,
                blocks = ComputeBlockSize(MaxSaveSize)
            };

            var Save = MountSave(Indentifier, Mount, out uint Error);

            if (Save != null)
            {
                Save.Unmount();
                return true;
            }

            switch (Error)
            {
                case 0x809f0003:
                    return true;
                case 0x809f0008:
                    return false;
                case 0x809f0013:
                    return true;
                case 0x809f000f:
                    return AllowCorrupted;
            }

            return false;
        }

        /// <summary>
        /// Mount the save if exists, if not returns null
        /// </summary>
        public OrbisSaveData Update(bool Confirm, string Indentifier = null)
        {
            if (!Initialized)
                Initialize();

            if (Indentifier == null)
            {
                Indentifier = "SYSTEM";
            }

            if (Confirm && !ConfirmDialog(Indentifier))
            {
                return null;
            }
            

            var Mount = new OrbisSaveDataMount2()
            {
                userId = UserID,
                dirName = new OrbisSaveDataDirName()
                {
                    data = Indentifier
                },
                mountMode = OrbisSaveDataMountMode.RDWR,
                blocks = ComputeBlockSize(MaxSaveSize)
            };

            var Save = MountSave(Indentifier, Mount, out uint Error);
            
            if (Save == null)
                ParseError(Indentifier, Error);

            return Save;
        }

        public OrbisSaveData Open(string Indentifier = null)
        {
            if (!Initialized)
                Initialize();

            if (Indentifier == null)
            {
                Indentifier = "SYSTEM";
            }
            
            var Mount = new OrbisSaveDataMount2()
            {
                userId = UserID,
                dirName = new OrbisSaveDataDirName()
                {
                    data = Indentifier
                },
                mountMode = OrbisSaveDataMountMode.RDONLY,
                blocks = ComputeBlockSize(MaxSaveSize)
            };

            var Save = MountSave(Indentifier, Mount, out uint Error);
            
            if (Save == null)
                ParseError(Indentifier, Error);

            return Save;
        }

        private static void ParseError(string Indentifier, uint Error)
        {
            switch (Error)
            {
                case 0x809f0003:
                    throw new Exception($"{Indentifier} Save Data Already Mounted");
                case 0x809f000a:
                    throw new Exception("No available space");
                case 0x809f000c:
                    throw new Exception("Too many saves mounted");
                case 0x809f0011:
                    throw new Exception("Invalid UserID or Inactive User");
                case 0x809f0013:
                    throw new Exception("Save busy for backup");
                case 0x809f000f:
                    throw new Exception("Corrupted Save Data");
            }
        }

        private static void PreInitialize()
        {
            //This must be executed in a separated function for the JIT engine does not load the CommonDialog early
            Kernel.LoadStartModule("libSceRegMgr.sprx");
        }

        private static void PostInitialize()
        {
            UserService.Initialize();
            UserService.GetInitialUser(out UserID);

            sceSaveDataInitialize3();
            sceCommonDialogInitialize();
            sceSaveDataDialogInitialize();
        }

        public static void Initialize()
        {
            if (Initialized)
                return;

            Initialized = true;
            
            PreInitialize();
            PostInitialize();
        }
        
        //Stolen from: https://www.psdevwiki.com/ps4/Keystone
        public static byte[] GenerateKeystoneFile(string passcode)
        {
            // 1. The first 32 bytes are constant
            byte[] keystone = {
                0x6B, 0x65, 0x79, 0x73, 0x74, 0x6F, 0x6E, 0x65, 0x02, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };

            // 2. Convert the 32 characters of the passcode to a byte array
            byte[] passcodeInHEX = Encoding.ASCII.GetBytes(passcode);

            // 3. Calculate the fingerprint of the passcode
            HMACSHA256 hmac = new HMACSHA256();
            hmac.Key = new byte[32];
            
            
            byte[] fingerprint = hmac.ComputeHash(passcodeInHEX);

            // 4. Concat the 32 bytes from point 1 and the 32 bytes from point 3
            keystone = keystone.Concat(fingerprint).ToArray();

            // 5. Calculate the SHA256Hmac of the 64 bytes from point 4
            hmac.Key = new byte[32];
            byte[] sha256hmac = hmac.ComputeHash(keystone);

            // 6. Concat the constant bytes from point 1, the fingerprint from point 3 and the hmac from point 5
            keystone = keystone.Concat(sha256hmac).ToArray();


            return keystone;
        }


        [DllImport("libSceCommonDialog.sprx")]
        private static extern int sceCommonDialogInitialize();

        [DllImport("libSceSaveDataDialog.sprx")]
        private static extern int sceSaveDataDialogInitialize();

        [DllImport("libSceSaveDataDialog.sprx")]
        private static unsafe extern int sceSaveDataDialogOpen(IntPtr param);

        [DllImport("libSceSaveDataDialog.sprx")]
        private static extern int sceSaveDataDialogUpdateStatus();


        [DllImport("libSceSaveDataDialog.sprx")]
        private static extern int sceSaveDataDialogGetResult(ref OrbisSaveDataDialogResult result);

        [DllImport("libSceSaveData.sprx")]
        private static extern int sceSaveDataInitialize3(int initParam = 0);

        [DllImport("libSceSaveData.sprx")]
        private static extern int sceSaveDataMount2(IntPtr mount, ref OrbisSaveDataMountResult mountResult);

        [DllImport("libSceSaveData.sprx")]
        private static extern int sceSaveDataDelete(IntPtr del);
    }
}
