using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Orbis.Internals
{
    public unsafe class Kernel
    {
        public static int LoadStartModule(string Path)
        {
            var pPath = AllocString(Path);

            int LoadStatus = 0;
            var Result = LoadStartModule(pPath);

            free(pPath);

            return Result;
        }

        public static bool GetModuleBase(string Name, out long BaseAddress, out long ModuleSize)
        {
            var pName = AllocString(Name);

            long bAddr = 0;
            long mSize = 0;

            var Success = GetModuleBase(pName, &bAddr, &mSize);

            BaseAddress = bAddr;
            ModuleSize = mSize;

            free(pName);

            return Success;
        }

        public static void Log(string Message)
        {
#if ORBIS
            var pMsg = AllocString(Message);
            Log(pMsg);
            free(pMsg);
#endif
        }

        public static void* AllocString(string String, out int BufferSize)
        {
            var Data = Encoding.UTF8.GetBytes(String + "\x0");

            byte* Buffer = (byte*)malloc(Data.Length);

            BufferSize = Data.Length;

            for (int i = 0; i < Data.Length; i++)
                Buffer[i] = Data[i];

            return Buffer;
        }
        public static void* AllocString(string String)
        {
            var Data = Encoding.UTF8.GetBytes(String + "\x0");

            byte* Buffer = (byte*)malloc(Data.Length);

            for (int i = 0; i < Data.Length; i++)
                Buffer[i] = Data[i];

            return Buffer;
        }

        public static string ParseString(byte* pString)
        {
            List<byte> Buffer = new List<byte>();
            while (*pString != 0)
            {
                Buffer.Add(*pString++);
            }

            return Encoding.UTF8.GetString(Buffer.ToArray());
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        static extern void Log(void* Line);

        [MethodImpl(MethodImplOptions.InternalCall)]
        static extern int LoadStartModule(void* path);

        [MethodImpl(MethodImplOptions.InternalCall)]
        static extern bool GetModuleBase(void* Name, void* BaseAddress, void* ModuleSize);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern bool Jailbreak(long AuthID = 0);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern bool Unjailbreak();

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern bool IsJailbroken();

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void* malloc(int Size);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void free(void* Size);


        [DllImport("libkernel.sprx", EntryPoint = "sceKernelMprotect")]
        public static extern int MemProtect(void* Address, int size, int Flags);

        [DllImport("libkernel.sprx")]
        public static extern void sceKernelUsleep(uint MicroSecond);

        [DllImport("libSceRtc.sprx")]
        public static extern int sceRtcGetCurrentTick(out long CurrentTick);

        public const int PROT_READ = 1;
        public const int PROT_WRITE = 2;
        public const int PROT_EXEC = 4;
    }
}