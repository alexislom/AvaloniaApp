using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AvaloniaApp.Behaviours;

public static class DataGridEditOnClickBehavior
{
    public static readonly AttachedProperty<bool> EnableEditOnClickProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>(
            "EnableEditOnClick",
            typeof(DataGridEditOnClickBehavior),
            false);

    public static void SetEnableEditOnClick(AvaloniaObject element, bool value) =>
        element.SetValue(EnableEditOnClickProperty, value);

    public static bool GetEnableEditOnClick(AvaloniaObject element) =>
        element.GetValue(EnableEditOnClickProperty);

    static DataGridEditOnClickBehavior()
    {
        EnableEditOnClickProperty.Changed.AddClassHandler<DataGrid>((dg, e) =>
        {
            if (e.NewValue is bool enable)
            {
                if (enable)
                    dg.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
                else
                    dg.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
            }
        });
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not DataGrid dg) return;

        var point = e.GetPosition(dg);
        var hit = dg.InputHitTest(point) as Control;
        var cell = hit?.FindAncestorOfType<DataGridCell>();
        if (cell == null) return;

        dg.BeginEdit();

        var tb = cell.FindDescendantOfType<TextBox>();
        tb?.Focus();
    }
}