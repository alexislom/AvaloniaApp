using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using OxyPlot;
using OxyPlot.Series;
using ReactiveUI;

namespace AvaloniaApp.Models;

public class FunctionModel : ReactiveObject
{
    public string Name { get; set; } = "Функция";

    public ObservableCollection<Node> Nodes { get; set; } = new();

    private PlotModel _plotModel;

    public PlotModel PlotModel
    {
        get => _plotModel;
        private set => this.RaiseAndSetIfChanged(ref _plotModel, value);
    }

    public FunctionModel()
    {
        Nodes.CollectionChanged += (_, e) => OnNodesChanged(e);
        RebuildPlot();
    }

    private void OnNodesChanged(NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (Node node in e.NewItems)
                node.PropertyChanged += NodeChanged;
        }

        if (e.OldItems != null)
        {
            foreach (Node node in e.OldItems)
                node.PropertyChanged -= NodeChanged;
        }

        RebuildPlot();
    }

    private void NodeChanged(object? sender, PropertyChangedEventArgs e)
    {
        RebuildPlot();
    }

    public void RebuildPlot(Node? selected = null)
    {
        var plot = new PlotModel { Title = Name };
        var series = new LineSeries { MarkerType = MarkerType.Circle };

        foreach (var n in Nodes.OrderBy(n => n.X))
            series.Points.Add(new DataPoint(n.X, n.Y));

        plot.Series.Add(series);

        // выделим активную точку
        if (selected != null)
        {
            var highlight = new ScatterSeries { MarkerType = MarkerType.Diamond, MarkerSize = 6 };
            highlight.Points.Add(new ScatterPoint(selected.X, selected.Y));
            plot.Series.Add(highlight);
        }

        PlotModel = plot;
    }

    public void LoadFromFile(string path)
    {
        if (!File.Exists(path))
            return;
        var data = JsonSerializer.Deserialize<ObservableCollection<Node>>(File.ReadAllText(path));
        Nodes.Clear();
        
        foreach (var n in data!)
            Nodes.Add(n);
    }

    public void SaveToFile(string path)
    {
        var json = JsonSerializer.Serialize(Nodes);
        File.WriteAllText(path, json);
    }

    public System.Collections.Generic.List<Point>? TryInverse()
    {
        if (Nodes.Count < 2) return null;

        var sorted = Nodes.OrderBy(n => n.Y).ToList();
        if (sorted.Select(n => n.Y).Distinct().Count() != sorted.Count)
            return null; // обратная не существует (не монотонна)

        return sorted.Select(n => new Point(n.Y, n.X)).ToList();
    }
}