using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
    private LineSeries<ObservablePoint> _series;
    private LineSeries<ObservablePoint>? _inverseSeries;
    private PointModel? _selectedPoint;
    private bool _isInverseVisible;

    public ObservableCollection<PointModel> Points { get; } = new();
    public LineSeries<ObservablePoint> Series
    {
        get => _series;
        set => this.RaiseAndSetIfChanged(ref _series, value);
    }
    public LineSeries<ObservablePoint>? InverseSeries
    {
        get => _inverseSeries;
        private set => this.RaiseAndSetIfChanged(ref _inverseSeries, value);
    }
    public ObservableCollection<ISeries> SeriesCollection { get; } = new();
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
            this.RaisePropertyChanged(nameof(SeriesCollection));
        }
    }
    public string ColorHex { get; }
    public ICommand AddPointCommand { get; }
    public ICommand RemovePointCommand { get; }

    public FunctionViewModel()
    {
        ColorHex = GenerateRandomColorHex();
        Series = CreateSeries(false, []);
        InverseSeries = CreateSeries(true, []);
        SeriesCollection.Add(Series);
        Points
            .ToObservableChangeSet()
            .AutoRefresh(p => p.X)
            .AutoRefresh(p => p.Y)
            .Throttle(TimeSpan.FromMilliseconds(150))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                UpdateSeries();
                if (IsInverseVisible && CanBuildInverseSeries())
                    UpdateInverseSeries();
            });

        GenerateRandomPoints();

        AddPointCommand = ReactiveCommand.Create(AddPoint);
        RemovePointCommand = ReactiveCommand.Create(RemovePoint);
    }
    
    private void AddPoint()
    {
        var x = Random.NextDouble() * 10 - 5;
        var y = Random.NextDouble() * 10 - 5;

        Points.Add(new PointModel
        {
            X = x.ToString("F2", CultureInfo.InvariantCulture),
            Y = y.ToString("F2", CultureInfo.InvariantCulture)
        });
    }

    private void RemovePoint()
    {
        if (SelectedPoint != null)
            Points.Remove(SelectedPoint);
    }
    
    private LineSeries<ObservablePoint> CreateSeries(bool isInverse, List<ObservablePoint> points)
    {
        var strokeColor = SKColor.Parse(ColorHex);

        return new LineSeries<ObservablePoint>
        {
            Name = isInverse ? $"F⁻¹(x)" : "F(x)",
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
            Values = new ObservableCollection<ObservablePoint>(points)
        };
    }

    private void UpdateSeries()
    {
        var points = Points
            .Select(p =>
            {
                if (double.TryParse(p.X, NumberStyles.Any, CultureInfo.InvariantCulture, out var x) && 
                    double.TryParse(p.Y, NumberStyles.Any, CultureInfo.InvariantCulture, out var y))
                    return new ObservablePoint(x, y);
                return null;
            })
            .Where(p => p != null)!
            .OrderBy(p => p!.X)
            .ToList();
        
        Series = CreateSeries(isInverse: false, points!);
        SeriesCollection.Clear();
        SeriesCollection.Add(Series);
    }
    
    private void UpdateInverseSeries()
    {
        var inversePoints = Points
            .Select(p =>
            {
                if (double.TryParse(p.X, NumberStyles.Any, CultureInfo.InvariantCulture, out var x) && 
                    double.TryParse(p.Y, NumberStyles.Any, CultureInfo.InvariantCulture, out var y))
                    return new ObservablePoint(y, x); // Change X ↔ Y
                return null;
            })
            .Where(p => p != null)!
            .OrderBy(p => p!.X)
            .ToList();

        InverseSeries = CreateSeries(isInverse: true, inversePoints!);
        SeriesCollection.Add(InverseSeries);
    }
    
    private bool CanBuildInverseSeries()
    {
        // Проверяем, что все точки парсятся корректно
        var parsedPoints = Points
            .Select(p => new
            {
                X = double.TryParse(p.X, NumberStyles.Any, CultureInfo.InvariantCulture, out var x) ? x : double.NaN,
                Y = double.TryParse(p.Y, NumberStyles.Any, CultureInfo.InvariantCulture, out var y) ? y : double.NaN
            })
            .Where(p => !double.IsNaN(p.X) && !double.IsNaN(p.Y))
            .ToList();

        // Если меньше двух точек — обратную функцию строить бессмысленно
        if (parsedPoints.Count < 2)
            return false;

        // Проверяем, что все X уникальны (иначе исходная функция не является функцией)
        var uniqueX = parsedPoints.Select(p => p.X).Distinct().Count();
        if (uniqueX != parsedPoints.Count)
            return false; // есть одинаковые X

        // Проверяем, что все Y уникальны (иначе функция не является инъективной)
        var uniqueY = parsedPoints.Select(p => p.Y).Distinct().Count();
        if (uniqueY != parsedPoints.Count)
            return false; // есть одинаковые Y, обратной функции не существует

        // Всё ок — функция взаимно однозначная
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
            SeriesCollection.Remove(InverseSeries!);
        }
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
            .GetMessageBoxStandard("Ошибка", $"Для F(x) невозможно построить обратную функцию (неоднозначные Y).", ButtonEnum.Ok);
        var result = await messageBox.ShowAsync();
    }
}