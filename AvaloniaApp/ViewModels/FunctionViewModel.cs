using System;
using System.Collections.Generic;
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
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using SkiaSharp;

namespace AvaloniaApp.ViewModels;

public class FunctionViewModel : ReactiveObject
{
    private static readonly Random Random = new();
    private string _name;
    private LineSeries<ObservablePoint> _series;
    private LineSeries<ObservablePoint>? _inverseSeries;
    private PointModel? _selectedPoint;
    private bool _isInverseVisible;

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
    public ISeries[] SeriesArray
    {
        get
        {
            if (_inverseSeries != null && _isInverseVisible)
                return [_series, _inverseSeries];
            return [_series];
        }
    }
    public PointModel? SelectedPoint
    {
        get => _selectedPoint;
        set => this.RaiseAndSetIfChanged(ref _selectedPoint, value);
    }
    public bool IsInverseVisible
    {
        get => _isInverseVisible;
        set
        {
            this.RaiseAndSetIfChanged(ref _isInverseVisible, value);
            ToggleInverseVisibility(value);
            this.RaisePropertyChanged(nameof(SeriesArray));
        }
    }
    public string ColorHex { get; }
    public ICommand AddPointCommand { get; }
    public ICommand RemovePointCommand { get; }

    public FunctionViewModel(string name)
    {
        _name = name;
        ColorHex = GenerateRandomColorHex();
        _series = CreateSeries(isInverse: false, []);
        _inverseSeries = CreateSeries(isInverse: true, []);
        GenerateRandomPoints();
        Points
            .ToObservableChangeSet()
            .AutoRefresh(p => p.X)
            .AutoRefresh(p => p.Y)
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                UpdateSeries();
                if (_isInverseVisible)
                    UpdateInverseSeries();
            });

        AddPointCommand = ReactiveCommand.Create(AddPoint);
        RemovePointCommand = ReactiveCommand.Create(RemovePoint);
    }
    
    private void AddPoint()
    {
        var x = Random.NextDouble() * 10 - 5;
        var y = Random.NextDouble() * 10 - 5;

        Points.Add(new PointModel
        {
            X = x.ToString("F2"),
            Y = y.ToString("F2")
        });
    }

    private void RemovePoint()
    {
        if (SelectedPoint != null)
            Points.Remove(SelectedPoint);
    }
    
    private LineSeries<ObservablePoint> CreateSeries(bool isInverse, List<ObservablePoint?> points)
    {
        var strokeColor = SKColor.Parse(ColorHex);

        return new LineSeries<ObservablePoint>
        {
            Name = isInverse ? $"{Name}⁻¹(x)" : Name,
            LineSmoothness = 0,
            Fill = null,
            GeometrySize = isInverse ? 6 : 8,
            GeometryStroke = new SolidColorPaint(strokeColor) { StrokeThickness = isInverse ? 2 : 3 },
            GeometryFill = new SolidColorPaint(strokeColor),
            Stroke = new SolidColorPaint(strokeColor)
            {
                StrokeThickness = isInverse ? 3 : 4,
                PathEffect = isInverse ? new DashEffect(new float[] { 6, 4 }) : null
            },
            DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
            AnimationsSpeed = TimeSpan.FromMilliseconds(80),
            EasingFunction = EasingFunctions.Lineal,
            Values = new ObservableCollection<ObservablePoint>(points!)
        };
    }

    private void UpdateSeries()
    {
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
        
        Series = CreateSeries(false, points);
    }
    
    private void UpdateInverseSeries()
    {
        if (_inverseSeries == null)
            return;

        var inversePoints = Points
            .Select(p =>
            {
                if (double.TryParse(p.X, out var x) && double.TryParse(p.Y, out var y))
                    return new ObservablePoint(y, x); // Change X ↔ Y
                return null;
            })
            .Where(p => p != null)!
            .OrderBy(p => p!.X)
            .ToList();
        
        _inverseSeries = CreateSeries(true, inversePoints);
    }
    
    private bool CanBuildInverseSeries()
    {
        var parsedPoints = Points
            .Select(p => new
            {
                X = double.TryParse(p.X, out var x) ? x : double.NaN,
                Y = double.TryParse(p.Y, out var y) ? y : double.NaN
            })
            .Where(p => !double.IsNaN(p.X) && !double.IsNaN(p.Y))
            .ToList();

        // Если меньше двух точек — бессмысленно строить обратную
        if (parsedPoints.Count < 2)
            return false;

        // Проверяем, что каждому X соответствует только один Y
        var hasDuplicateX = parsedPoints
            .GroupBy(p => p.X)
            .Any(g => g.Count() > 1);
        if (hasDuplicateX)
            return false;

        // Проверяем, что каждому Y соответствует только один X
        var hasDuplicateY = parsedPoints
            .GroupBy(p => p.Y)
            .Any(g => g.Count() > 1);
        if (hasDuplicateY)
            return false;

        // Функция взаимно однозначна => обратная существует
        return true;
    }
    
    private void ToggleInverseVisibility(bool show)
    {
        if (show)
        {
            if (!CanBuildInverseSeries())
            {
                ShowInverseUnavailableMessage();
                IsInverseVisible = false;
                return;
            }

            UpdateInverseSeries();
        }
        else
        {
            // _inverseSeries = null;
        }
        this.RaisePropertyChanged(nameof(SeriesArray));
    }
    
    private void GenerateRandomPoints()
    {
        Points.Clear();
        for (var i = 0; i < 3; i++)
            AddPoint();
    }
    
    private static string GenerateRandomColorHex()
    {
        var r = Random.Next(0, 256);
        var g = Random.Next(0, 256);
        var b = Random.Next(0, 256);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private async void ShowInverseUnavailableMessage()
    {
        var messageBox = MessageBoxManager
            .GetMessageBoxStandard("Ошибка", $"Для {Name} невозможно построить обратную функцию (неоднозначные Y).", ButtonEnum.Ok);
        var result = await messageBox.ShowAsync();
    }
}