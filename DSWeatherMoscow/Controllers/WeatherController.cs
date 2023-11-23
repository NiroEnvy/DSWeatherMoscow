using DSWeatherMoscow.DbContexts;
using DSWeatherMoscow.Extensions;
using DSWeatherMoscow.Interfaces;
using DSWeatherMoscow.Models;

namespace DSWeatherMoscow.Controllers;

public class WeatherController : Controller
{
    private const string CacheKeyMin = "minYear";
    private const string CacheKeyMax = "maxYear";
    private readonly WeatherDbContext _context;
    private readonly IDBWrapper _idbWrapper;
    private readonly ILogger<WeatherController> _logger;
    private readonly IMemoryCache _memoryCache;


    public WeatherController(WeatherDbContext context, ILogger<WeatherController> logger, IMemoryCache memoryCache, IDBWrapper idbWrapper)
    {
        _context = context;
        _logger = logger;
        _memoryCache = memoryCache;
        _idbWrapper = idbWrapper;
    }

    public IActionResult ViewWeatherArchives(int? year, int? month, int page = 1, int pageSize = 50)
    {
        IQueryable<WeatherData> query = _context.WeatherData;
        if (year.HasValue)
        {
            query = query.Where(w => w.Date.Year == year);
        }

        if (month.HasValue)
        {
            query = query.Where(w => w.Date.Month == month);
        }


        // Try to get the value from the cache
        if (!_memoryCache.TryGetValue(CacheKeyMin, out int minYear))
        {
            if (_context.WeatherData.Any())
            {
                minYear = _context.WeatherData.Min(w => w.Date.Year);
                // Set cache options
                var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromDays(1));
                // Set the value in cache
                _memoryCache.Set(CacheKeyMin, minYear, cacheEntryOptions);
            }
        }

        if (!_memoryCache.TryGetValue(CacheKeyMax, out int maxYear))
        {
            if (_context.WeatherData.Any())
            {
                maxYear = _context.WeatherData.Max(w => w.Date.Year);
                _memoryCache.Set(CacheKeyMax, maxYear, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromDays(1)));
            }
        }

        ViewBag.MinYear = minYear;
        ViewBag.MaxYear = maxYear;
        ViewBag.Year = year;
        ViewBag.Month = month;

        var pagedWeatherArchives = query.OrderByDescending(w => w.Date).ToPagedList(page, pageSize);

        return View(pagedWeatherArchives);
    }


    public IActionResult UploadWeatherArchives()
    {
        return View();
    }

    [HttpPost]
    public IActionResult UploadWeatherArchives(List<IFormFile> files)
    {
        _idbWrapper.BeginTransaction();
        try
        {
            foreach (var file in files.Where(file => file.Length > 0))
            {
                using var stream = file.OpenReadStream();
                var workbook = new XSSFWorkbook(stream);
                foreach (var sheet in workbook)
                {
                    ProcessRow(sheet);
                }
            }
            _context.SaveChanges(); // Commit changes for all rows in the transaction
            _idbWrapper.Commit(); // Commit the transaction
            _memoryCache.Remove(CacheKeyMin);
            _memoryCache.Remove(CacheKeyMax);
        }
        catch (Exception e)
        {
            _idbWrapper.Rollback(); // Rollback the transaction in case of an error
            _logger.LogError(e, "Error with new file: {Ex}", e);
        }
        return RedirectToAction(nameof(ViewWeatherArchives));
    }

    private void ProcessRow(ISheet sheet)
    {
        for (var rowIdx = 5; rowIdx <= sheet.LastRowNum; rowIdx++)
        {
            var row = sheet.GetRow(rowIdx);
            if (row == null) continue;

            var weatherData = ParseRowToWeatherData(row);

            var existingRecord = _context.WeatherData?.FirstOrDefault(w => w.Date == weatherData.Date);

            if (existingRecord != null)
            {
                UpdateExistingRecord(existingRecord, weatherData);
                _context.WeatherData?.Update(existingRecord);
            }
            else
            {
                _context.WeatherData?.Add(weatherData);
            }
        }
    }

    private static WeatherData ParseRowToWeatherData(IRow row)
    {
        var date = Convert.ToDateTime(row.GetCell(0, MissingCellPolicy.RETURN_NULL_AND_BLANK).ParseCellAsString());
        var time = Convert.ToDateTime(row.GetCell(1, MissingCellPolicy.RETURN_NULL_AND_BLANK).ParseCellAsString());
        var weatherData = new WeatherData {
            Date = DateTime.SpecifyKind(
                new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second),
                DateTimeKind.Utc),
            Temperature = row.GetCell(2, MissingCellPolicy.RETURN_NULL_AND_BLANK).ParseCellAsDouble(),
            AirHumidity = row.GetCell(3, MissingCellPolicy.RETURN_NULL_AND_BLANK).ParseCellAsDouble(),
            Td = row.GetCell(4, MissingCellPolicy.RETURN_NULL_AND_BLANK).ParseCellAsDouble(),
            AtmPressure = row.GetCell(5, MissingCellPolicy.RETURN_NULL_AND_BLANK).ParseCellAsDouble(),
            AirDirection = row.GetCell(6, MissingCellPolicy.RETURN_NULL_AND_BLANK).ParseCellAsString(),
            AirSpeed = row.GetCell(7, MissingCellPolicy.RETURN_NULL_AND_BLANK).ParseCellAsDouble(),
            Cloudiness = row.GetCell(8, MissingCellPolicy.RETURN_NULL_AND_BLANK).ParseCellAsDouble(),
            H = row.GetCell(9, MissingCellPolicy.RETURN_NULL_AND_BLANK).ParseCellAsDouble(),
            VV = row.GetCell(10, MissingCellPolicy.RETURN_NULL_AND_BLANK).ParseCellAsDouble(),
            WeatherEvents = row.GetCell(11, MissingCellPolicy.RETURN_NULL_AND_BLANK).ParseCellAsString()
        };
        return weatherData;
    }

    private static void UpdateExistingRecord(WeatherData existingRecord, WeatherData newData)
    {
        var properties = typeof(WeatherData).GetProperties()
            .Where(p => p is {CanRead: true, CanWrite: true} && p.Name != nameof(WeatherData.Id));

        foreach (var property in properties)
        {
            var newValue = property.GetValue(newData);
            property.SetValue(existingRecord, newValue);
        }
    }
}