namespace DSWeatherMoscow.Utils;

public abstract class Helpers
{
    public static double? ParseCellAsDouble(ICell? cell)
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

    public static string? GetStringCellValue(ICell? cell)
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

    public static void CopyProperties(object source, object destination)
    {
        // If any this null throw an exception
        if (source == null || destination == null)
        {
            throw new Exception("Source or/and Destination Objects are null");
        }
        // Getting the Types of the objects
        var typeDest = destination.GetType();
        var typeSrc = source.GetType();

        // Iterate the Properties of the source instance and  
        // populate them from their desination counterparts  
        var srcProps = typeSrc.GetProperties();
        foreach (var srcProp in srcProps)
        {
            if (!srcProp.CanRead)
            {
                continue;
            }
            var targetProperty = typeDest.GetProperty(srcProp.Name);
            if (targetProperty == null)
            {
                continue;
            }
            if (!targetProperty.CanWrite)
            {
                continue;
            }
            if (targetProperty.GetSetMethod(true) != null && targetProperty.GetSetMethod(true).IsPrivate)
            {
                continue;
            }
            if ((targetProperty.GetSetMethod().Attributes & MethodAttributes.Static) != 0)
            {
                continue;
            }
            if (!targetProperty.PropertyType.IsAssignableFrom(srcProp.PropertyType))
            {
                continue;
            }
            // Passed all tests, lets set the value
            targetProperty.SetValue(destination, srcProp.GetValue(source, null), null);
        }
    }
}