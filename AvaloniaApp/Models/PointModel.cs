using System.Globalization;
using ReactiveUI;

namespace AvaloniaApp.Models;

public class PointModel : ReactiveObject
{
    private string _x = "0";
    private string _y = "0";

    public string X
    {
        get => _x;
        set => this.RaiseAndSetIfChanged(ref _x, value);
    }

    public string Y
    {
        get => _y;
        set => this.RaiseAndSetIfChanged(ref _y, value);
    }

    public double XValue => double.TryParse(X, NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ? x : 0;
    public double YValue => double.TryParse(Y, NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ? y : 0;
}