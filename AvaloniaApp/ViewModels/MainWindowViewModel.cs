using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using AvaloniaApp.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using ReactiveUI;
using SkiaSharp;

namespace AvaloniaApp.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private bool _hasUnsavedChanges;
    private FunctionViewModel? _selectedFunction;
    public ISeries[] Series { get; private set; }
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
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
    }
    public ObservableCollection<FunctionViewModel> Functions { get; } = new();
    public FunctionViewModel? SelectedFunction
    {
        get => _selectedFunction;
        set => this.RaiseAndSetIfChanged(ref _selectedFunction, value);
    }
    public ICommand AddFunctionCommand { get; }
    public ICommand RemoveFunctionCommand { get; }

    public MainWindowViewModel()
    {
        AddFunctionCommand = ReactiveCommand.Create(AddFunction);
        RemoveFunctionCommand = ReactiveCommand.Create(RemoveFunction);
        var f1 = new FunctionViewModel("f₁(x)");
        f1.Points.Add(new PointModel { X = "0", Y = "0" });
        f1.Points.Add(new PointModel { X = "1", Y = "2" });
        f1.Points.Add(new PointModel { X = "2", Y = "1" });

        var f2 = new FunctionViewModel("f₂(x)");
        f2.Points.Add(new PointModel { X = "0", Y = "1" });
        f2.Points.Add(new PointModel { X = "1", Y = "3" });
        f2.Points.Add(new PointModel { X = "2", Y = "2" });

        Functions.Add(f1);
        Functions.Add(f2);

        SelectedFunction = f1;
        
        Series = Functions.Select(f => f.Series).ToArray();
    }

    private void AddFunction()
    {
        var f = new FunctionViewModel($"F{Functions.Count + 1}(x)");
        f.Points.Add(new PointModel { X = "0", Y = "0" });
        f.Points.Add(new PointModel { X = "1", Y = "1" });

        Functions.Add(f);
        Series = Functions.Select(x => x.Series).ToArray();
        this.RaisePropertyChanged(nameof(Series));
    }

    private void RemoveFunction()
    {
        if (SelectedFunction == null)
            return;

        Functions.Remove(SelectedFunction);
        SelectedFunction = Functions.FirstOrDefault();
        Series = Functions.Select(x => x.Series).ToArray();
        this.RaisePropertyChanged(nameof(Series));
    }
}