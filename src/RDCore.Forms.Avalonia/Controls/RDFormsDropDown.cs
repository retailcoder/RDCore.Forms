using System.Globalization;
using System.Resources;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;

namespace RDCore.Forms.Avalonia.Controls;

// The drop-down of a ComboBox or a DriveListBox, as UI Automation sees it and opens it. Its button is named as Windows names a combo
// box's, "Open" in the user's language. Expanding it, as a program sends a Win32 combo box CB_SHOWDROPDOWN, leaves the focus where it is:
// no item of the list takes it while the list is open, and collapsing it gives the focus to no control that was without it.
internal sealed class RDFormsDropDown
{
    private static readonly ResourceManager Texts = new("RDCore.Forms.Avalonia.Controls.ControlTexts", typeof(RDFormsDropDown).Assembly);
    private readonly ComboBox combo;
    // The items kept from the focus while a list UI Automation opened is open; none while the list is closed or the user opened it.
    private List<IDisposable>? held;

    public RDFormsDropDown(ComboBox combo)
    {
        this.combo = combo;
        combo.DropDownClosed += (_, _) => Release();
    }

    // The drop-down button's name.
    public static string ButtonName => Texts.GetString("DropDownButton", CultureInfo.CurrentUICulture) ?? "Open";

    // Names the template's button, where it has one: a Simple Combo's has none.
    public static void NameButton(TemplateAppliedEventArgs e)
    {
        if (e.NameScope.Find<ToggleButton>("toggle") is { } button)
            AutomationProperties.SetName(button, ButtonName);
    }

    public AutomationPeer CreatePeer() => new Peer(this);

    // An item made while the list is open is kept from the focus too.
    public void Prepare(Control container)
    {
        if (held is { } items)
            items.Add(Hold(container));
    }

    private void Expand()
    {
        if (combo.IsDropDownOpen || combo.IsKeyboardFocusWithin)
        {
            combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty, true);
            return;
        }
        held = [.. combo.GetRealizedContainers().Select(Hold)];
        combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty, true);
        // A combo that refuses to drop, a Simple or a Locked one, keeps nothing.
        if (!combo.IsDropDownOpen)
            Release();
    }

    private void Collapse()
    {
        if (combo.IsKeyboardFocusWithin)
        {
            combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty, false);
            return;
        }
        // An editable combo would take the focus as its list closes: it is held from it while the list closes.
        using (Hold(combo))
            combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty, false);
    }

    private void Release()
    {
        if (held is not { } items)
            return;
        held = null;
        foreach (var item in items)
            item.Dispose();
    }

    private static IDisposable Hold(Control control) =>
        control.SetValue(InputElement.FocusableProperty, false, BindingPriority.Animation) ?? Nothing.Instance;

    private sealed class Nothing : IDisposable
    {
        public static readonly Nothing Instance = new();

        public void Dispose()
        {
        }
    }

    private sealed class Peer(RDFormsDropDown dropDown) : ComboBoxAutomationPeer(dropDown.combo), IExpandCollapseProvider
    {
        void IExpandCollapseProvider.Expand()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();
            dropDown.Expand();
        }

        void IExpandCollapseProvider.Collapse()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();
            dropDown.Collapse();
        }
    }
}
