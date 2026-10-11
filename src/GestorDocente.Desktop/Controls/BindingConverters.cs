using System.Globalization;
using Avalonia.Data.Converters;
namespace GestorDocente.Desktop.Controls;
public sealed class EqualsConverter : IValueConverter
{
    public object Convert(object? value,Type targetType,object? parameter,CultureInfo culture)=>value?.ToString()==parameter?.ToString();
    public object ConvertBack(object? value,Type targetType,object? parameter,CultureInfo culture)=>throw new NotSupportedException();
}
public sealed class HasTextConverter : IValueConverter
{
    public object Convert(object? value,Type targetType,object? parameter,CultureInfo culture)=>!string.IsNullOrWhiteSpace(value?.ToString());
    public object ConvertBack(object? value,Type targetType,object? parameter,CultureInfo culture)=>throw new NotSupportedException();
}
