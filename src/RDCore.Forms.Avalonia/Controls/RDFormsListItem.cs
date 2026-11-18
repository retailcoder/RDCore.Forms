using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace RDCore.Forms.Avalonia.Controls;

// An item of a ListBox, a FileListBox or a DirListBox, as UI Automation selects it: through the list's own selection, as code selects
// an item, so that Click is raised once for each change, and none for an item already selected. The list answers whether the item at
// an index is selected, and selects or clears it.
internal sealed class RDFormsListItem(Func<int, bool> isSelected, Action<int, bool> select) : ListBoxItem
{
    private readonly Func<int, bool> isSelected = isSelected;
    private readonly Action<int, bool> select = select;

    protected override Type StyleKeyOverride => typeof(ListBoxItem);

    protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this);

    private int Index => ItemsControl.ItemsControlFromItemContainer(this) is { } list ? list.IndexFromContainer(this) : -1;

    private sealed class Peer(RDFormsListItem owner) : ListItemAutomationPeer(owner), ISelectionItemProvider
    {
        bool ISelectionItemProvider.IsSelected => owner.Index is var index and >= 0 && owner.isSelected(index);

        ISelectionProvider? ISelectionItemProvider.SelectionContainer =>
            ItemsControl.ItemsControlFromItemContainer(owner) is { } list ? CreatePeerForElement(list).GetProvider<ISelectionProvider>() : null;

        void ISelectionItemProvider.Select() => Choose(true);

        void ISelectionItemProvider.AddToSelection() => Choose(true);

        void ISelectionItemProvider.RemoveFromSelection() => Choose(false);

        private void Choose(bool selected)
        {
            if (!IsEnabled())
                throw new global::Avalonia.Automation.ElementNotEnabledException();
            if (owner.Index is var index and >= 0)
                owner.select(index, selected);
        }
    }
}
