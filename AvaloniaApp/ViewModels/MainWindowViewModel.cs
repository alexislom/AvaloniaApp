using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using ReactiveUI;
using SkiaSharp;

namespace AvaloniaApp.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private readonly SourceList<FunctionViewModel> _functionsSource = new();
    private ReadOnlyObservableCollection<FunctionViewModel> _functions;
    private FunctionViewModel? _selectedFunction;
    private ISeries[] _series = [];
    private bool _hasUnsavedChanges;
    
    public ISeries[] Series
    {
        get => _series;
        private set => this.RaiseAndSetIfChanged(ref _series, value);
    }
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
    public ReadOnlyObservableCollection<FunctionViewModel> Functions => _functions;
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
    }
    
    public FunctionViewModel? SelectedFunction
    {
        get => _selectedFunction;
        set => this.RaiseAndSetIfChanged(ref _selectedFunction, value);
    }
    public ICommand AddFunctionCommand { get; }
    public ICommand RemoveFunctionCommand { get; }

    public MainWindowViewModel()
    {
        // Реактивное обновление графиков при любых изменениях функций
        _functionsSource
            .Connect()
            // При изменении IsInverseVisible или SeriesArray обновляем графики
            .AutoRefresh(f => f.IsInverseVisible)
            .AutoRefresh(f => f.SeriesArray)
            // Следим за изменениями внутри Points каждой функции
            .AutoRefreshOnObservable(f => f.Points
                .ToObservableChangeSet()
                .AutoRefresh(p => p.X)
                .AutoRefresh(p => p.Y)
                .Throttle(TimeSpan.FromMilliseconds(150))
            )
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _functions)
            .Subscribe(_ =>
            {
                UpdateSeries();
                
                if (!_functions.Any()) 
                {
                    ClearSeries(); // вызываем метод, чтобы RaisePropertyChanged точно сработал
                }
            });

        AddFunctionCommand = ReactiveCommand.Create(AddFunction);
        RemoveFunctionCommand = ReactiveCommand.Create(RemoveFunction);
    }

    private void AddFunction()
    {
        var newFunction = new FunctionViewModel($"F{Functions.Count + 1}(x)");
        _functionsSource.Add(newFunction);
        // SelectedFunction = newFunction; //???
    }

    private void RemoveFunction()
    {
        if (SelectedFunction == null)
            return;

        _functionsSource.Remove(SelectedFunction);
        SelectedFunction = Functions.LastOrDefault();
    }
    
    private void UpdateSeries()
    {
        Series = Functions.SelectMany(f => f.SeriesArray).ToArray();
    }
    
    private void ClearSeries()
    {
        Series = Array.Empty<ISeries>();
    }
}