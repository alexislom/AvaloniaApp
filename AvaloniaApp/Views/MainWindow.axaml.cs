using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaApp.ViewModels;
using OxyPlot.Avalonia;

namespace AvaloniaApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void PlotView_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var vm = this.DataContext as MainWindowViewModel;
        if (vm == null) return;
        var pv = sender as PlotView;
        if (pv == null) return;

        var p = e.GetCurrentPoint(pv).Position; // Avalonia Point (in device-independent pixels)
        vm.OnPlotPointerPressed(p, pv);
        e.Handled = true;
    }

    private void PlotView_PointerMoved(object? sender, PointerEventArgs e)
    {
        var vm = this.DataContext as MainWindowViewModel;
        if (vm == null) return;
        var pv = sender as PlotView;
        if (pv == null) return;

        var p = e.GetCurrentPoint(pv).Position;
        vm.OnPlotPointerMoved(p, pv);
        e.Handled = true;
    }

    private void PlotView_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var vm = this.DataContext as MainWindowViewModel;
        if (vm == null) return;
        var pv = sender as PlotView;
        if (pv == null) return;

        var p = e.GetCurrentPoint(pv).Position;
        // vm.OnPlotPointerReleased(p, pv);
        vm.OnPlotPointerReleased();
        e.Handled = true;
    }
}