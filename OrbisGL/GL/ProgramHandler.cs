using Orbis.Internals;
using SharpGLES;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace OrbisGL.GL
{
    public class ProgramHandler : IDisposable
    {

#if DEBUG
        static int TotalShadersReferences => ReferenceCount.Values.Sum();
        static int TotalShadersLoaded => ProgramCache.Count();
#endif
        static Dictionary<byte[], int> ProgramCache = new Dictionary<byte[], int>(new ByteArrayComparer());


        static Dictionary<int, int> ReferenceCount = new Dictionary<int, int>();

        int hProgram;

        byte[] ProgramHash;

        bool _Disposed = false;
        bool Disposed => _Disposed || !ProgramCache.ContainsKey(ProgramHash);

        public static implicit operator int(ProgramHandler handler)
        {
            if (handler.Disposed)
                throw new ObjectDisposedException(nameof(handler));

            return handler.hProgram;
        }

        public ProgramHandler(string Vertex, string Fragment)
        {
            ProgramHash = GetSHA256(Encoding.UTF8.GetBytes(Vertex + Fragment));

            if (ProgramCache.TryGetValue(ProgramHash, out hProgram))
            {
                ReferenceCount[hProgram]++;
                return;
            } 

            hProgram = Shader.GetProgram(Vertex, Fragment);

            if (ProgramCache.ContainsValue(hProgram))
            {
                foreach(var Pair in ProgramCache.ToArray())
                {
                    if (Pair.Value == hProgram)
                    {
#if DEBUG && ORBIS
                        Kernel.Log("Shader Program Handle collision, Manually disposed?");
#endif
                        ProgramCache.Remove(Pair.Key);
                        ReferenceCount.Remove(Pair.Value);
                    }
                }
            }

            ProgramCache[ProgramHash] = hProgram;

            if (!ReferenceCount.ContainsKey(hProgram))
                ReferenceCount[hProgram] = 0;

            ReferenceCount[hProgram]++;
        }

#if ORBIS
        public ProgramHandler(byte[] Vertex, byte[] Fragment)
        {
            ProgramHash = GetSHA256(Vertex.Concat(Fragment).ToArray());

            if (ProgramCache.TryGetValue(ProgramHash, out hProgram))
            {
                ReferenceCount[hProgram]++;
                return;
            }

            hProgram = Shader.GetProgram(Vertex, Fragment);

            if (ProgramCache.ContainsValue(hProgram))
            {
                foreach(var Pair in ProgramCache.ToArray())
                {
                    if (Pair.Value == hProgram)
                    {
                        ProgramCache.Remove(Pair.Key);
                        ReferenceCount.Remove(Pair.Value);
                    }
                }
            }

            ProgramCache[ProgramHash] = hProgram;

            if (!ReferenceCount.ContainsKey(hProgram))
                ReferenceCount[hProgram] = 0;

            ReferenceCount[hProgram]++;
        }
#endif
        public ProgramHandler(int hProgram)
        {
            if (!ReferenceCount.ContainsKey(hProgram))
                ReferenceCount[hProgram] = 0;

            ReferenceCount[hProgram]++;
            this.hProgram = hProgram;
        }

        private static byte[] GetSHA256(byte[] Data)
        {
            SHA256 Hasher = SHA256.Create();
            return Hasher.ComputeHash(Data);
        }

#if DEBUG && ORBIS
        public static void LogShaderCacheStatus()
        {
            Kernel.Log($"Shaders Loaded: {TotalShadersLoaded}, Shaders References: {TotalShadersReferences}\n");
        }
#elif DEBUG
        public static void LogShaderCacheStatus()
        {
            Debugger.Log(0, "INF", $"Shaders Loaded: {TotalShadersLoaded}, Shaders References: {TotalShadersReferences}\n");
        }
#endif
        public void Dispose()
        {
            if (Disposed)
                return;

            ReferenceCount[hProgram]--;

            _Disposed = true;
            
            if (ReferenceCount[hProgram] <= 0)
            {
                GLES20.DeleteProgram(hProgram);
                ReferenceCount.Remove(hProgram);

                if (ProgramCache.ContainsKey(ProgramHash))
                    ProgramCache.Remove(ProgramHash);
            }
        }
    }
}
