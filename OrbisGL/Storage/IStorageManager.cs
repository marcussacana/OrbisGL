namespace OrbisGL.Storage
{
    public interface IStorageManager<T> where T : IStorageData
    {
        T Create(string Indentifier);
        void Delete(string Indentifier);
        T Update(string Indentifier);
    }
}
