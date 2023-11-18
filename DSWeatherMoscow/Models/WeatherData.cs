namespace DSWeatherMoscow.Models;

public class WeatherData
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public double? Temperature { get; set; }
    public double? AirHumidity { get; set; }
    public double? Td { get; set; }
    public double? AtmPressure { get; set; }
    public string? AirDirection { get; set; }
    public double? AirSpeed { get; set; }
    public double? Cloudiness { get; set; }
    public double? H { get; set; }
    public double? VV { get; set; }
    public string? WeatherEvents { get; set; }
}