using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AvaloniaApp.Behaviours;

public class NumericInputBehavior : AvaloniaObject
{
    // Soft check (allow: "-", "1.", "1,")
    private static readonly Regex SoftPattern =
        new(@"^-?\d*([.,]?\d*)?$", RegexOptions.Compiled);

    // Strict check: without -0, 001, except 0
    private static readonly Regex StrictPattern =
        new(@"^(?:-?(?:0|[1-9]\d*))([.,]\d+)?$", RegexOptions.Compiled);

    public static readonly AttachedProperty<bool> EnableNumericValidationProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>(
            "EnableNumericValidation",
            typeof(NumericInputBehavior));

    public static void SetEnableNumericValidation(AvaloniaObject element, bool value) =>
        element.SetValue(EnableNumericValidationProperty, value);

    public static bool GetEnableNumericValidation(AvaloniaObject element) =>
        element.GetValue(EnableNumericValidationProperty);

    static NumericInputBehavior()
    {
        EnableNumericValidationProperty.Changed.AddClassHandler<TextBox>((tb, e) =>
        {
            if (e.NewValue is bool enable)
            {
                if (enable)
                {
                    tb.AddHandler(InputElement.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
                    tb.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
                    tb.AddHandler(InputElement.LostFocusEvent, OnLostFocus, RoutingStrategies.Tunnel);
                    
                    tb.AttachedToVisualTree += (_, _) =>
                    {
                        var top = tb.GetVisualRoot() as InputElement;
                        top?.AddHandler(InputElement.PointerPressedEvent,
                            (_, _) =>
                            {
                                if (tb.IsKeyboardFocusWithin)
                                    Normalize(tb);
                            },
                            RoutingStrategies.Tunnel);
                    };
                }
                else
                {
                    tb.RemoveHandler(InputElement.TextInputEvent, OnTextInput);
                    tb.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
                    tb.RemoveHandler(InputElement.LostFocusEvent, OnLostFocus);
                }
            }
        });
    }

    private static void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (sender is not TextBox tb)
            return;

        var current = tb.Text ?? string.Empty;
        var newText = current.Insert(tb.CaretIndex, e.Text);

        if (!SoftPattern.IsMatch(newText))
        {
            e.Handled = true;
        }
    }
    
    private static void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox tb && e.Key == Key.Enter)
        {
            Normalize(tb);
            e.Handled = true;
        }
    }
    
    private static void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
            Normalize(tb);
    }
    
    private static void Normalize(TextBox tb)
    {
        // var text = tb.Text?.Trim() ?? string.Empty;
        //
        // if (StrictPattern.IsMatch(text) &&
        //     double.TryParse(text.Replace(',', '.'),
        //         NumberStyles.Float,
        //         CultureInfo.InvariantCulture,
        //         out var parsed))
        // {
        //     if (parsed == 0 && text.StartsWith("-"))
        //     {
        //         tb.Text = "0";
        //         return;
        //     }
        //
        //     tb.Text = parsed.ToString(CultureInfo.InvariantCulture);
        // }
        // else
        // {
        //     tb.Text = "0";
        // }
        //
        var oldText = tb.Text?.Trim() ?? string.Empty;
        var normalized = oldText;

        if (StrictPattern.IsMatch(oldText) &&
            double.TryParse(oldText.Replace(',', '.'),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsed))
        {
            normalized = parsed.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            normalized = "0";
        }

        // Избегаем лишних PropertyChanged если текст не изменился
        if (tb.Text != normalized)
            tb.Text = normalized;
    }
}