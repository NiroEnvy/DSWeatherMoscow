namespace DSWeatherMoscow.Interfaces;

public interface IDBWrapper
{
    void BeginTransaction();
    void Commit();
    void Rollback();
}