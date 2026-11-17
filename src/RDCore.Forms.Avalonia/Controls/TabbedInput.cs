using Avalonia.Controls;
using Avalonia.Input;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Avalonia.Controls;

// The mouse and the keys of a MultiPage or a TabStrip, whose events name the tab they are about first (-1 for none).
//  - A press raises MouseDown with the tab under the pointer; a press of the left button on a tab that can be selected selects it, which
//    raises Change where the selection changed, and its release over the same tab raises Click. A release raises MouseUp, before Click.
//  - A double click's second press raises DblClick, with Cancel, in place of MouseDown, and its release raises no Click.
//  - On the control itself, the arrow keys along its tabs select the previous or next tab that can be selected, and raise Click; from
//    anywhere inside it, Ctrl+Tab and Ctrl+Shift+Tab do the same, wrapping round.
// What else the pointer is over decides whether the press is the control's: a control on a page is that control's own, and the page's
// empty area is the MultiPage's.
internal sealed class TabbedInput(Control control, RDFormsTabHeaders headers, Func<object?, bool> owns)
{
    private int pressed = -1;
    private bool second;

    private ComponentInstance? Instance => RDFormsControl.GetInstance(control);

    public void Pressed(PointerPressedEventArgs e)
    {
        if (!owns(e.Source) || Instance is not { } instance)
            return;
        var index = headers.IndexAt(e.GetPosition(headers));
        var point = e.GetCurrentPoint(control);
        second = e.ClickCount % 2 == 0 && point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed;
        if (second)
        {
            pressed = -1;
            if (index >= 0)
                RDFormsControl.Raise(control, Events.TabDblClick, FormValue.FromLong(index), FormValue.FromBoolean(false));
            return;
        }

        Raise(Events.TabMouseDown, index, e, moved: false);
        pressed = -1;
        if (index < 0 || point.Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed || !headers.Tabs[index].Enabled)
            return;
        pressed = index;
        instance.Set(P.SelectedIndex, index);
        control.Focus(NavigationMethod.Pointer);
    }

    public void Released(PointerReleasedEventArgs e)
    {
        if (!owns(e.Source))
            return;
        var index = headers.IndexAt(e.GetPosition(headers));
        Raise(Events.TabMouseUp, index, e, moved: false);
        if (!second && pressed >= 0 && index == pressed)
            RDFormsControl.Raise(control, Events.TabClick, FormValue.FromLong(index));
        pressed = -1;
        second = false;
    }

    public void Moved(PointerEventArgs e)
    {
        if (owns(e.Source))
            Raise(Events.TabMouseMove, headers.IndexAt(e.GetPosition(headers)), e, moved: true);
    }

    public void KeyDown(KeyEventArgs e, TabOrientation orientation)
    {
        if (e.Handled || Instance is not { } instance)
            return;
        var vertical = orientation is TabOrientation.Left or TabOrientation.Right;
        int? step = (e.Key, e.KeyModifiers) switch
        {
            (Key.Tab, KeyModifiers.Control) => 1,
            (Key.Tab, KeyModifiers.Control | KeyModifiers.Shift) => -1,
            (Key.Right, KeyModifiers.None) when !vertical && ReferenceEquals(e.Source, control) => 1,
            (Key.Left, KeyModifiers.None) when !vertical && ReferenceEquals(e.Source, control) => -1,
            (Key.Down, KeyModifiers.None) when vertical && ReferenceEquals(e.Source, control) => 1,
            (Key.Up, KeyModifiers.None) when vertical && ReferenceEquals(e.Source, control) => -1,
            _ => null,
        };
        if (step is not { } by)
            return;
        e.Handled = true;
        var wraps = e.Key == Key.Tab;
        var tabs = headers.Tabs;
        var index = instance.Get(P.SelectedIndex);
        for (var tried = 0; tried < tabs.Count; tried++)
        {
            index += by;
            if (index < 0 || index >= tabs.Count)
            {
                if (!wraps)
                    return;
                index = (index + tabs.Count) % tabs.Count;
            }

            if (tabs[index] is { Enabled: true, Visible: true })
            {
                instance.Set(P.SelectedIndex, index);
                RDFormsControl.Raise(control, Events.TabClick, FormValue.FromLong(index));
                return;
            }
        }
    }

    private void Raise(EventDefinition definition, int index, PointerEventArgs e, bool moved)
    {
        if (ControlEvents.MouseArguments(control, e, moved) is { } arguments)
            RDFormsControl.Raise(control, definition, [FormValue.FromLong(index), .. arguments]);
    }
}
