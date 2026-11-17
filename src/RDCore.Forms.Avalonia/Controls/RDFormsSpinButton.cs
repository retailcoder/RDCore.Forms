using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using RDCore.Forms.Model;
using ArrowShape = Avalonia.Controls.Shapes.Path;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// An RDForms SpinButton: two <see cref="RepeatButton"/> arrows that show a <see cref="ComponentClasses.SpinButton"/> component.
/// </summary>
/// <remarks>
/// <para>
/// A click on the up or right arrow raises SpinUp, then moves the component's <c>Value</c> by <c>SmallChange</c> toward <c>Max</c>; a
/// click on the down or left arrow raises SpinDown, then moves it toward <c>Min</c>. A move stops at the end, and a <c>Min</c> greater
/// than <c>Max</c> moves it the other way. Change is raised each time the <c>Value</c> changes, by an arrow or by code, and only when
/// it changes.
/// </para>
/// <para>
/// An arrow held down repeats: the second move comes five times <c>Delay</c> milliseconds after the press, and the others every
/// <c>Delay</c> milliseconds.
/// </para>
/// <para>
/// The arrows are one above the other where its <c>Orientation</c> is Vertical, and side by side, the down arrow on the left, where it
/// is Horizontal. Auto follows its shape: side by side where it is wider than it is tall.
/// </para>
/// <para>
/// It takes the focus, and raises GotFocus and LostFocus, and KeyDown, KeyPress and KeyUp. With the focus, the Up and Right keys spin
/// it up and the Down and Left keys down, as a click on the arrow does. The arrows are drawn in its <c>ForeColor</c> on its
/// <c>BackColor</c>.
/// </para>
/// </remarks>
public sealed class RDFormsSpinButton : Grid, IRaisesKeyEvents
{
    // An arrow's triangle, pointing up; the others are this one turned.
    private static readonly Geometry ArrowUp = Geometry.Parse("M 0,4 L 4,0 L 8,4 Z");

    // The first repeat of a held arrow comes this many Delays after its press.
    private const int FirstRepeat = 5;

    private readonly ComponentInstance instance;
    private readonly RepeatButton up;
    private readonly RepeatButton down;
    private readonly ArrowShape upArrow;
    private readonly ArrowShape downArrow;

    static RDFormsSpinButton()
    {
        FocusableProperty.OverrideDefaultValue<RDFormsSpinButton>(true);
        ControlEvents.AttachFocus<RDFormsSpinButton>();
        ControlKeys.Attach<RDFormsSpinButton>();
    }

    /// <summary>
    /// Creates the arrows that show a SpinButton.
    /// </summary>
    /// <param name="instance">The SpinButton.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a SpinButton.</exception>
    public RDFormsSpinButton(ComponentInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        this.instance = instance;
        (up, upArrow) = Arrow(true);
        (down, downArrow) = Arrow(false);
        Children.Add(up);
        Children.Add(down);
        ComponentBinding.Bind(this, instance, ComponentClasses.SpinButton, Apply);
    }

    /// <summary>
    /// The up or right arrow.
    /// </summary>
    public RepeatButton UpArrow => up;

    /// <summary>
    /// The down or left arrow.
    /// </summary>
    public RepeatButton DownArrow => down;

    /// <summary>
    /// Whether the arrows are side by side, by the component's <c>Orientation</c> and, where it is Auto, by its shape.
    /// </summary>
    public bool IsHorizontal => instance.Get(P.Orientation) switch
    {
        ControlOrientation.Horizontal => true,
        ControlOrientation.Vertical => false,
        _ => instance.Get(P.Width) > instance.Get(P.Height),
    };

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || e.KeyModifiers != KeyModifiers.None)
            return;
        bool? spinsUp = e.Key switch
        {
            Key.Up or Key.Right => true,
            Key.Down or Key.Left => false,
            _ => null,
        };
        if (spinsUp is not { } isUp)
            return;
        e.Handled = true;
        Spin(isUp);
    }

    // The event of the arrow, then the move of the value toward its end.
    private void Spin(bool isUp)
    {
        RDFormsControl.Raise(this, isUp ? Events.SpinUp : Events.SpinDown);
        var min = instance.Get(P.SpinButtonMin);
        var max = instance.Get(P.SpinButtonMax);
        var value = instance.Get(P.SpinButtonValue);
        var toward = isUp ? max : min;
        var step = (long)instance.Get(P.SpinButtonSmallChange) * Math.Sign((long)toward - value);
        var next = Math.Clamp(value + step, Math.Min(min, max), Math.Max(min, max));
        instance.Set(P.SpinButtonValue, (int)next);
    }

    private (RepeatButton Button, ArrowShape Arrow) Arrow(bool isUp)
    {
        var arrow = new ArrowShape
        {
            Data = ArrowUp,
            Stretch = Stretch.None,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var button = new RepeatButton
        {
            Content = arrow,
            Focusable = false,
            Padding = default,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        button.Click += (_, e) =>
        {
            e.Handled = true;
            Spin(isUp);
        };
        return (button, arrow);
    }

    // The arrows one above the other, or side by side with the down arrow on the left, each pointing its way.
    private void Arrange()
    {
        var horizontal = IsHorizontal;
        RowDefinitions.Clear();
        ColumnDefinitions.Clear();
        if (horizontal)
        {
            ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            Place(down, 0, 0);
            Place(up, 0, 1);
        }
        else
        {
            RowDefinitions.Add(new RowDefinition(GridLength.Star));
            RowDefinitions.Add(new RowDefinition(GridLength.Star));
            Place(up, 0, 0);
            Place(down, 1, 0);
        }
        upArrow.RenderTransform = new RotateTransform(horizontal ? 90 : 0);
        downArrow.RenderTransform = new RotateTransform(horizontal ? -90 : 180);
    }

    private static void Place(Control control, int row, int column)
    {
        SetRow(control, row);
        SetColumn(control, column);
    }

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        if (ReferenceEquals(e.Property, P.SpinButtonValue))
        {
            if (!Equals(e.Before, e.After))
                RDFormsControl.Raise(this, Events.Change);
            return;
        }
        if (ReferenceEquals(e.Property, P.Delay))
        {
            var delay = Math.Max(1, (int)e.After!);
            foreach (var button in new[] { up, down })
            {
                button.Delay = delay * FirstRepeat;
                button.Interval = delay;
            }
            return;
        }
        if (ReferenceEquals(e.Property, P.BackColor))
        {
            up.Background = down.Background = FormPalette.ToBrush((FormColor)e.After!);
            return;
        }
        if (ReferenceEquals(e.Property, P.ForeColor))
        {
            upArrow.Fill = downArrow.Fill = FormPalette.ToBrush((FormColor)e.After!);
            return;
        }
        ComponentBinding.ApplyCommon(this, e.Property, e.After);
        if (ReferenceEquals(e.Property, P.Orientation) || ReferenceEquals(e.Property, P.Width) || ReferenceEquals(e.Property, P.Height))
            Arrange();
    }
}
