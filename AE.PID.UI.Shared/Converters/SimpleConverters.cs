using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using Color = Avalonia.Media.Color;

namespace AE.PID.UI.Shared.Converters;

public abstract class ThemeConverters
{
    public static FuncValueConverter<ThemeVariant?, Color?> ThemeToTintColorConverter { get; } =
        new(theme => theme == null || (string)theme.Key == "Light" ? Colors.White : Colors.Black);
}