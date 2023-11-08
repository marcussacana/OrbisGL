namespace OrbisGL.Storage
{
    public interface IStorageManager<T> where T : IStorageData
    {
        T Create(bool Confirm, int MaxSize, string Indentifier);
        bool Delete(bool Confirm, string Indentifier);
        T Update(bool Confirm, string Indentifier);
        T Open(string Indentifier);
        
        bool Exists(string Indentifier, bool AllowCorrupted);
    }
}
