using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using ReactiveUI;
using SkiaSharp;

namespace AvaloniaApp.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    public ObservableCollection<ObservablePoint> Points { get; set; }
    public ISeries[] Series { get; set; }

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

    public ObservablePoint? SelectedPoint { get; set; }
    public ICommand AddPointCommand { get; }
    public ICommand RemovePointCommand { get; }

    public MainWindowViewModel()
    {
        Points = new ObservableCollection<ObservablePoint>
        {
            new(0, 0),
            new(1, 2),
            new(2, 1)
        };
        Series =
        [
            new LineSeries<ObservablePoint>
            {
                Values =
                [
                    new(0, 0),
                    new(1, 2),
                    new(2, 1),
                    new(-2, -10)
                ],
                Name = "Function 1"
            },
            new LineSeries<ObservablePoint>
            {
                Values =
                [
                    new(0, 1),
                    new(1, 3),
                    new(2, 2)
                ],
                Name = "Function 2"
            }
        ];
        AddPointCommand = ReactiveCommand.Create(AddPoint);
        RemovePointCommand = ReactiveCommand.Create(RemovePoint);
    }

    private void AddPoint()
    {
        var nextX = Points.Any() ? Points.Max(p => p.X) + 1 : 0;
        Points.Add(new ObservablePoint(nextX, 0));
    }

    private void RemovePoint()
    {
        if (SelectedPoint != null)
            Points.Remove(SelectedPoint);
    }
}