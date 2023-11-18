using DSWeatherMoscow.DbContexts;
using DSWeatherMoscow.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace DSWeatherMoscow.Services;

public class EfdbWrapper : IDBWrapper
{
    private readonly WeatherDbContext _context;
    private IDbContextTransaction? _transaction;

    public EfdbWrapper(WeatherDbContext context)
    {
        _context = context;
    }

    public void BeginTransaction()
    {
        _transaction = _context.Database.BeginTransaction();
    }

    public void Commit()
    {
        if (_transaction == null) return;

        _transaction.Commit();
        _transaction.Dispose();
        _transaction = null;
    }

    public void Rollback()
    {
        if (_transaction == null) return;

        _transaction.Rollback();
        _transaction.Dispose();
        _transaction = null;
    }
}