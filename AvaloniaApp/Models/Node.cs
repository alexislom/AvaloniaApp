using ReactiveUI;

namespace AvaloniaApp.Models;

public class Node : ReactiveObject
{
    private double _x;
    private double _y;

    public double X
    {
        get => _x;
        set => this.RaiseAndSetIfChanged(ref _x, value);
    }

    public double Y
    {
        get => _y;
        set => this.RaiseAndSetIfChanged(ref _y, value);
    }
}