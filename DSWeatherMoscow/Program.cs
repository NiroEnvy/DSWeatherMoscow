using DSWeatherMoscow.DbContexts;
using DSWeatherMoscow.Interfaces;
using DSWeatherMoscow.Services;

namespace DSWeatherMoscow;

internal static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        //load from appsettings.json
        builder.Configuration.AddJsonFile("appsettings.json");
        builder.Services.AddDbContext<WeatherDbContext>(options =>
        {
            var path = builder.Configuration.GetConnectionString("WeatherDbConnection");
            options.UseNpgsql(path);
        });
        // Create a logger factory and configure it
        builder.Services.AddLogging(logging =>
        {
            logging.AddConsole(); // Add console logger
        });
        builder.Services.AddMemoryCache();
        builder.Services.AddScoped<IDBWrapper, EfdbWrapper>();

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        //app.UseAuthorization();

        app.MapControllerRoute(
            "default",
            "{controller=Home}/{action=Index}/{id?}");
        app.Run();
    }
}