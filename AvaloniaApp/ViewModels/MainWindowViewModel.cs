using System.Collections.ObjectModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using ReactiveUI;
using SkiaSharp;

namespace AvaloniaApp.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    public FunctionViewModel Function { get; set; }
    private bool _hasUnsavedChanges;

    public ObservableCollection<ISeries> Series { get; } = new();
    public Axis[] XAxes { get; set; } =
    [
        new()
        {
            Name = "X Axis",
            NamePaint = new SolidColorPaint(SKColors.Black),
            LabelsPaint = new SolidColorPaint(SKColors.Blue),
            TextSize = 10,
            SeparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray) { StrokeThickness = 2 }
        }
    ];
    public Axis[] YAxes { get; set; } =
    [
        new()
        {
            Name = "Y Axis",
            NamePaint = new SolidColorPaint(SKColors.Red),
            LabelsPaint = new SolidColorPaint(SKColors.Green),
            TextSize = 20,
            SeparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray)
            {
                StrokeThickness = 2,
                PathEffect = new DashEffect(new float[] { 3, 3 })
            }
        }
    ];
    public DrawMarginFrame DrawMarginFrame => new()
    {
        Stroke = new SolidColorPaint(SKColors.Black, 3)
    };
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
    }

    public MainWindowViewModel()
    {
        Function = new FunctionViewModel();
        Series = Function.SeriesCollection;
    }
}