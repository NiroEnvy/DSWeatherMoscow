namespace DSWeatherMoscow.Extensions;

public static class CellExtensions
{
    public static double? ParseCellAsDouble(this ICell? cell)
    {
        if (cell == null) return null;

        switch (cell.CellType)
        {
            case CellType.Numeric:
                return cell.NumericCellValue;

            case CellType.String:
                if (double.TryParse(cell.StringCellValue, out var result))
                {
                    return result;
                }
                return null;

            case CellType.Blank:
            default:
                return null;
        }
    }

    public static string? ParseCellAsString(this ICell? cell)
    {
        if (cell == null) return null;

        switch (cell.CellType)
        {
            case CellType.String:
                return cell.StringCellValue;

            case CellType.Numeric:
                return cell.NumericCellValue.ToString(CultureInfo.InvariantCulture); // Convert numeric value to string

            default:
                return null;
        }
    }
}