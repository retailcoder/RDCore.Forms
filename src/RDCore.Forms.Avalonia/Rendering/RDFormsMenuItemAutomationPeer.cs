using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Avalonia.Rendering;

// A menu item as UI Automation sees it, as a Win32 menu's item is seen: one that holds items expands and collapses its menu, which
// raises its Click as the menu opens; one that holds none is invoked, which chooses it as a click does. Its toggle state is its
// Checked, and toggling it chooses it too, so that the program's Checked, or CheckOnClick's, has the last word, as after a click.
internal sealed class RDFormsMenuItemAutomationPeer : MenuItemAutomationPeer, IInvokeProvider, IExpandCollapseProvider, IToggleProvider
{
    private readonly RDFormsMenuItem item;

    public RDFormsMenuItemAutomationPeer(RDFormsMenuItem owner)
        : base(owner)
    {
        item = owner;
        owner.PropertyChanged += (_, e) =>
        {
            if (e.Property == MenuItem.IsSubMenuOpenProperty)
                RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, StateOf((bool)e.OldValue!), StateOf((bool)e.NewValue!));
        };
    }

    private bool HoldsItems => item.Item.Items.Count > 0;

    public ExpandCollapseState ExpandCollapseState => StateOf(item.IsSubMenuOpen);

    public bool ShowsMenu => HoldsItems;

    ToggleState IToggleProvider.ToggleState => item.Item.Component.Get(P.Checked) ? ToggleState.On : ToggleState.Off;

    protected override object? GetProviderCore(Type providerType)
    {
        if (providerType == typeof(IExpandCollapseProvider))
            return HoldsItems ? this : null;
        if (providerType == typeof(IInvokeProvider) || providerType == typeof(IToggleProvider))
            return HoldsItems ? null : this;
        return base.GetProviderCore(providerType);
    }

    public void Invoke()
    {
        Enabled();
        if (HoldsItems)
            throw new InvalidOperationException("A menu that holds items is expanded, not invoked.");
        item.ChooseAsClicked();
    }

    void IToggleProvider.Toggle() => Invoke();

    public void Expand()
    {
        Enabled();
        if (!HoldsItems)
            throw new InvalidOperationException("A menu item that holds no items has no menu to expand.");
        item.OpenItsMenu();
    }

    public void Collapse()
    {
        Enabled();
        if (!HoldsItems)
            throw new InvalidOperationException("A menu item that holds no items has no menu to collapse.");
        item.CloseItsMenu();
    }

    private void Enabled()
    {
        if (!IsEnabled())
            throw new ElementNotEnabledException();
    }

    private ExpandCollapseState StateOf(bool open) =>
        !HoldsItems ? ExpandCollapseState.LeafNode : open ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
}
