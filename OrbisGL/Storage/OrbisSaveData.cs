using System;

namespace OrbisGL.Storage
{
    public class OrbisSaveData : IStorageData
    {
        internal OrbisSaveData(string name, string mountPath)
        {
            Name = name;
            MountedPath = mountPath;
        }

        public string Name { get; private set; }
        public string MountedPath { get; private set; }

        public void Unmount()
        {
            throw new NotImplementedException();
        }
    }
}
