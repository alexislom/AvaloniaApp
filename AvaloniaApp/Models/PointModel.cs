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
}