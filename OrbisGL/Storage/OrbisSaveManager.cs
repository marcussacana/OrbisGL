using Orbis.Internals;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using static OrbisGL.Storage.OrbisSaveDataDialogInterop;

namespace OrbisGL.Storage
{
    public class OrbisSaveManager : IStorageManager<OrbisSaveData>
    {
        static int UserID;
        static bool Initialized;
        public unsafe OrbisSaveData Create(string Indentifier = null)
        {
            if (!Initialized)
                Initialize();

            if (Indentifier == null)
            {
                IntPtr[] Pointers = new IntPtr[0];
                
                var Icon = File.ReadAllBytes(Path.Combine(IO.GetAppBaseDirectory(), "assets", "images", "dvd-logo.png"));
                
                try
                {
                    var Param = new OrbisSaveDataDialogParam()
                    {
                        mode = OrbisSaveDataDialogMode.ORBIS_SAVE_DATA_DIALOG_MODE_SYSTEM_MSG,
                        dispType = OrbisSaveDataDialogType.ORBIS_SAVE_DATA_DIALOG_TYPE_SAVE,
                        sysMsgParam = new OrbisSaveDataDialogSystemMessageParam()
                        {
                            sysMsgType = OrbisSaveDataDialogSystemMessageType.ORBIS_SAVE_DATA_DIALOG_SYSMSG_TYPE_CONFIRM
                        },
                        items = new OrbisSaveDataDialogItems()
                        {
                            userId = UserID,
                            dirName = new OrbisSaveDataDirName()
                            {
                                data = "SYSTEM"
                            },
                            dirNameNum = 1,
                            newItem = new OrbisSaveDataDialogNewItem()
                            {
                                iconBuf = Icon,
                                iconSize = (ulong)Icon.LongLength,
                                title = "Hello World"
                            },
                        }
                    };

                    Pointers = Param.CopyTo(out IntPtr Addr).ToArray();
                    
                    int Rst = sceSaveDataDialogOpen(Addr);

                    if (Rst != 0)
                        throw new Exception($"Failed to Create the Save Dialog, ERROR: 0x{Rst:X8}");

                    if (sceSaveDataDialogUpdateStatus() != 0)
                    {
                        int dbg = 0;
                        while ((dbg = sceSaveDataDialogUpdateStatus()) != 3)
                        {
                            Thread.Sleep(100);
                        }
                    }

                }
                finally
                {
                    foreach (var Pointer in Pointers)
                    {
                        Marshal.FreeHGlobal(Pointer);
                    }
                }
            }

            throw new NotImplementedException();
        }

        public void Delete(string Indentifier = null)
        {
            if (!Initialized)
                Initialize();

            throw new NotImplementedException();
        }

        public OrbisSaveData Update(string Indentifier = null)
        {
            if (!Initialized)
                Initialize();

            throw new NotImplementedException();
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

            int a = sceSaveDataInitialize3();
            int b = sceCommonDialogInitialize();
            int c = sceSaveDataDialogInitialize();
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

        [DllImport("libSceSaveData.sprx")]
        private static extern int sceSaveDataInitialize3(int initParam = 0);
    }
}
