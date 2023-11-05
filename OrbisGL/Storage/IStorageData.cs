namespace OrbisGL.Storage
{
    public interface IStorageData
    {
        string Name { get; }
        string MountedPath { get; }

        void Unmount();
    }
}
