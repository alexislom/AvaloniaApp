using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using AvaloniaApp.Models;
using DynamicData;
using DynamicData.Binding;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using ReactiveUI;

namespace AvaloniaApp.ViewModels;

public class FunctionViewModel : ReactiveObject
{
    private string _name;
    private LineSeries<ObservablePoint> _series;
    private PointModel? _selectedPoint;

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public ObservableCollection<PointModel> Points { get; } = new();

    public LineSeries<ObservablePoint> Series
    {
        get => _series;
        set => this.RaiseAndSetIfChanged(ref _series, value);
    }

    public PointModel? SelectedPoint
    {
        get => _selectedPoint;
        set => this.RaiseAndSetIfChanged(ref _selectedPoint, value);
    }
    
    public ICommand AddPointCommand { get; }
    public ICommand RemovePointCommand { get; }

    public FunctionViewModel(string name)
    {
        _name = name;
        // Создаём начальную серию
        _series = new LineSeries<ObservablePoint>
        {
            Name = name,
            LineSmoothness = 0,
            Values = new ObservableCollection<ObservablePoint>()
        };

        // Подписка на изменения свойств каждой точки
        Points
            .ToObservableChangeSet()
            .AutoRefresh(p => p.X)
            .AutoRefresh(p => p.Y)
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => UpdateSeries());

        Points.CollectionChanged += (_, _) => UpdateSeries();
        
        AddPointCommand = ReactiveCommand.Create(AddPoint);
        RemovePointCommand = ReactiveCommand.Create(RemovePoint);
    }
    
    private void AddPoint()
    {
        Points.Add(new PointModel { X = "0", Y = "0" });
    }

    private void RemovePoint()
    {
        if (SelectedPoint != null)
            Points.Remove(SelectedPoint);
    }

    private void UpdateSeries()
    {
        if (Series is not LineSeries<ObservablePoint> line)
            return;

        var points = Points
            .Select(p =>
            {
                if (double.TryParse(p.X, out var x) && double.TryParse(p.Y, out var y))
                    return new ObservablePoint(x, y);
                return null;
            })
            .Where(p => p != null)!
            .OrderBy(p => p!.X)
            .ToList();

        line.Values = new ObservableCollection<ObservablePoint>(points!);
    }
}