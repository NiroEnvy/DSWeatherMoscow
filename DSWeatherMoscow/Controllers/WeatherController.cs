using DSWeatherMoscow.DbContexts;
using DSWeatherMoscow.Interfaces;
using DSWeatherMoscow.Models;
using DSWeatherMoscow.Utils;

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
                try
                {
                    using var stream = file.OpenReadStream();
                    var workbook = new XSSFWorkbook(stream);

                    foreach (var sheet in workbook)
                    {
                        for (var rowIdx = 5; rowIdx <= sheet.LastRowNum; rowIdx++)
                        {
                            var row = sheet.GetRow(rowIdx);
                            if (row == null) continue;

                            try
                            {
                                var date = Convert.ToDateTime(
                                    Helpers.GetStringCellValue(row.GetCell(0, MissingCellPolicy.RETURN_NULL_AND_BLANK)));
                                var time = Convert.ToDateTime(
                                    Helpers.GetStringCellValue(row.GetCell(1, MissingCellPolicy.RETURN_NULL_AND_BLANK)));
                                var weatherData = new WeatherData {
                                    Date = DateTime.SpecifyKind(
                                        new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second),
                                        DateTimeKind.Utc),
                                    Temperature = Helpers.ParseCellAsDouble(row.GetCell(2, MissingCellPolicy.RETURN_NULL_AND_BLANK)),
                                    AirHumidity = Helpers.ParseCellAsDouble(row.GetCell(3, MissingCellPolicy.RETURN_NULL_AND_BLANK)),
                                    Td = Helpers.ParseCellAsDouble(row.GetCell(4, MissingCellPolicy.RETURN_NULL_AND_BLANK)),
                                    AtmPressure = Helpers.ParseCellAsDouble(row.GetCell(5, MissingCellPolicy.RETURN_NULL_AND_BLANK)),
                                    AirDirection = Helpers.GetStringCellValue(row.GetCell(6, MissingCellPolicy.RETURN_NULL_AND_BLANK)),
                                    AirSpeed = Helpers.ParseCellAsDouble(row.GetCell(7, MissingCellPolicy.RETURN_NULL_AND_BLANK)),
                                    Cloudiness = Helpers.ParseCellAsDouble(row.GetCell(8, MissingCellPolicy.RETURN_NULL_AND_BLANK)),
                                    H = Helpers.ParseCellAsDouble(row.GetCell(9, MissingCellPolicy.RETURN_NULL_AND_BLANK)),
                                    VV = Helpers.ParseCellAsDouble(row.GetCell(10, MissingCellPolicy.RETURN_NULL_AND_BLANK)),
                                    WeatherEvents = Helpers.GetStringCellValue(row.GetCell(11, MissingCellPolicy.RETURN_NULL_AND_BLANK))
                                };

                                var existingRecord = _context.WeatherData?.FirstOrDefault(w => w.Date == weatherData.Date);

                                if (existingRecord != null)
                                {
                                    Helpers.CopyProperties(existingRecord, existingRecord);
                                    _context.WeatherData?.Update(existingRecord);
                                }
                                else
                                {
                                    _context.WeatherData?.Add(weatherData);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error while parsing Excel file: {FileName} with row number {Id}: {Ex}", file.FileName,
                                    rowIdx, ex);
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while parsing Excel file {FileName}: {Ex}", file.FileName, ex);
                }
            }
            _context.SaveChanges(); // Commit changes for all rows in the transaction
            _idbWrapper.Commit(); // Commit the transaction
        }
        catch (Exception e)
        {
            _idbWrapper.Rollback(); // Rollback the transaction in case of an error
            _logger.LogError(e, "Error with transaction: {Ex}", e);
        }

        return RedirectToAction(nameof(ViewWeatherArchives));
    }
}