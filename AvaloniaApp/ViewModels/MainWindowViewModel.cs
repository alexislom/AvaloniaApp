using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using OxyPlot;
using OxyPlot.Axes;
using ReactiveUI;
using AvaloniaApp.Models; // Namespace, где FunctionModel и Node

namespace AvaloniaApp.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        // --- Свойства -------------------------------------------------------
        private FunctionModel? _selectedFunction;
        private Node? _draggedNode;
        private bool _isDirty;

        public ObservableCollection<FunctionModel> Functions { get; } = new();
        public FunctionModel? SelectedFunction
        {
            get => _selectedFunction;
            set => this.RaiseAndSetIfChanged(ref _selectedFunction, value);
        }

        public bool IsDirty
        {
            get => _isDirty;
            set => this.RaiseAndSetIfChanged(ref _isDirty, value);
        }

        // --- Команды --------------------------------------------------------
        public ReactiveCommand<Unit, Unit> AddFunctionCommand { get; }
        public ReactiveCommand<Unit, Unit> RemoveFunctionCommand { get; }
        public ReactiveCommand<Unit, Unit> AddPointCommand { get; }
        public ReactiveCommand<Node?, Unit> RemovePointCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public ReactiveCommand<Unit, Unit> LoadCommand { get; }

        // --- Конструктор ----------------------------------------------------
        public MainWindowViewModel()
        {
            AddFunctionCommand = ReactiveCommand.Create(AddFunction);
            RemoveFunctionCommand = ReactiveCommand.Create(RemoveFunction);
            AddPointCommand = ReactiveCommand.Create(AddPoint);
            RemovePointCommand = ReactiveCommand.Create<Node?>(RemovePoint);
            SaveCommand = ReactiveCommand.CreateFromTask(async () => await SaveAsync());
            LoadCommand = ReactiveCommand.CreateFromTask(async () => await LoadAsync());

            // Создаем пример по умолчанию
            var f = new FunctionModel { Name = "f(x)" };
            f.Nodes.Add(new Node{X = 0, Y = 0});
            f.Nodes.Add(new Node{X = 1, Y = 1});
            f.Nodes.Add(new Node{X = 2, Y = 0.5});
            f.RebuildPlot();
            Functions.Add(f);
            SelectedFunction = f;

            foreach (var func in Functions)
                SubscribeToFunction(func);
        }

        // --- Методы управления функциями -----------------------------------
        private void AddFunction()
        {
            var f = new FunctionModel { Name = $"f{Functions.Count + 1}(x)" };
            f.Nodes.Add(new Node{X = 0, Y =0});
            f.Nodes.Add(new Node{X =1,Y = 1});
            f.RebuildPlot();
            Functions.Add(f);
            SelectedFunction = f;
            SubscribeToFunction(f);
            IsDirty = true;
        }

        private void RemoveFunction()
        {
            if (SelectedFunction == null) return;
            Functions.Remove(SelectedFunction);
            SelectedFunction = Functions.FirstOrDefault();
            IsDirty = true;
        }

        // --- Методы работы с узлами ----------------------------------------
        private void AddPoint()
        {
            if (SelectedFunction == null) return;
            SelectedFunction.Nodes.Add(new Node{X =0, Y =0});
            SelectedFunction.RebuildPlot();
            IsDirty = true;
        }

        private void RemovePoint(Node? node)
        {
            if (node == null || SelectedFunction == null) return;
            SelectedFunction.Nodes.Remove(node);
            SelectedFunction.RebuildPlot();
            IsDirty = true;
        }

        // --- Сохранение и загрузка -----------------------------------------
        private async System.Threading.Tasks.Task SaveAsync()
        {
            var topLevel = GetTopLevel();
            if (topLevel == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сохранить функции",
                SuggestedFileName = "functions.json",
                FileTypeChoices = new[] { new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } } }
            });

            if (file == null) return;

            var json = JsonSerializer.Serialize(Functions);
            await File.WriteAllTextAsync(file.Path.LocalPath, json);
            IsDirty = false;
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            var topLevel = GetTopLevel();
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Открыть функции",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } } }
            });

            if (files.Count == 0) return;

            var text = await File.ReadAllTextAsync(files[0].Path.LocalPath);
            var funcs = JsonSerializer.Deserialize<ObservableCollection<FunctionModel>>(text);
            if (funcs == null) return;

            Functions.Clear();
            foreach (var f in funcs)
            {
                f.RebuildPlot();
                Functions.Add(f);
                SubscribeToFunction(f);
            }
            SelectedFunction = Functions.FirstOrDefault();
            IsDirty = false;
        }

        private TopLevel? GetTopLevel() =>
            (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        // --- Подписки для автообновления графика ---------------------------
        private void SubscribeToFunction(FunctionModel f)
        {
            f.Nodes.CollectionChanged += (_, __) => { f.RebuildPlot(); IsDirty = true; };

            foreach (var n in f.Nodes)
                n.PropertyChanged += (_, __) => { f.RebuildPlot(); IsDirty = true; };

            f.Nodes.CollectionChanged += (_, e) =>
            {
                if (e.NewItems != null)
                    foreach (Node n in e.NewItems)
                        n.PropertyChanged += (_, __) => { f.RebuildPlot(); IsDirty = true; };
            };
        }

        // --- Drag & Drop на графике ----------------------------------------
        public void OnPlotPointerPressed(Point p, OxyPlot.Avalonia.PlotView pv)
        {
            if (SelectedFunction == null) return;
            var model = pv.ActualModel;
            if (model == null) return;

            var sp = new ScreenPoint(p.X, p.Y);
            var data = Axis.InverseTransform(sp, model.DefaultXAxis, model.DefaultYAxis);

            var nearest = SelectedFunction.Nodes
                .OrderBy(n => Math.Pow(n.X - data.X, 2) + Math.Pow(n.Y - data.Y, 2))
                .FirstOrDefault();

            if (nearest == null) return;

            var dx = nearest.X - data.X;
            var dy = nearest.Y - data.Y;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 0.2) // порог
                _draggedNode = nearest;
        }

        public void OnPlotPointerMoved(Point p, OxyPlot.Avalonia.PlotView pv)
        {
            if (_draggedNode == null || SelectedFunction == null) return;
            var model = pv.ActualModel;
            if (model == null) return;

            var sp = new ScreenPoint(p.X, p.Y);
            var data = Axis.InverseTransform(sp, model.DefaultXAxis, model.DefaultYAxis);
            _draggedNode.X = data.X;
            _draggedNode.Y = data.Y;
            SelectedFunction.RebuildPlot();
            IsDirty = true;
        }

        public void OnPlotPointerReleased()
        {
            _draggedNode = null;
        }
    }
}
