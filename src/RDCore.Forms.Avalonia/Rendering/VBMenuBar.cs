using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Menus;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Rendering;

/// <summary>
/// A form's menu bar: the items of its <see cref="FormMenu"/>, each opening its own menu.
/// </summary>
/// <remarks>
/// <para>
/// It is shown while one of the bar's items is visible. Each item follows its component's <c>Caption</c>, with an ampersand marking its
/// access key, <c>Enabled</c>, <c>Checked</c>, <c>Visible</c> and <c>Shortcut</c>, whose key is shown beside it. An item whose
/// caption is a hyphen is a separator.
/// </para>
/// <para>
/// It raises Click as VB6 does, as measured with native and p-code builds: an item that holds items raises it as its menu opens, and
/// one that holds none as it is chosen, alone. Choosing an item leaves its <c>Checked</c> as the program set it, unless its
/// <c>CheckOnClick</c> is True: then it ticks or unticks it, before Click. <see cref="Choose"/> chooses an item as its shortcut does.
/// </para>
/// </remarks>
public sealed class VBMenuBar : Menu
{
    /// <summary>
    /// Creates the menu bar of a form.
    /// </summary>
    /// <param name="root">The form's root.</param>
    /// <exception cref="ArgumentNullException"><paramref name="root"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="root"/> is not a designer root.</exception>
    public VBMenuBar(ComponentInstance root)
    {
        Menu = FormMenu.Of(root);
        foreach (var item in Menu.Items)
            Items.Add(Create(item));
        Follow();
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(Menu);

    /// <summary>
    /// The form's menus.
    /// </summary>
    public FormMenu Menu { get; }

    /// <summary>
    /// Chooses an item as its shortcut does. From the bar down, each menu above the item raises Click as it opens, then the item
    /// raises its own. It stops at the first of them that is hidden or disabled, asked after the Click above it has run, so a menu's
    /// Click can enable the item first.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
    public void Choose(FormMenuItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var path = new List<FormMenuItem>();
        for (var step = item; step is not null; step = step.Parent)
            path.Insert(0, step);
        foreach (var step in path)
        {
            if (!step.Visible || !step.Enabled || step.IsSeparator)
                return;
            if (ReferenceEquals(step, item))
                Chosen(item);
            else
                RaiseClick(step);
        }
    }

    // The item chosen: ticked or unticked first where it says so, then its Click.
    internal void Chosen(FormMenuItem item)
    {
        if (item.CheckOnClick && !item.IsTopLevel)
            item.Component.Set(P.Checked, !item.Component.Get(P.Checked));
        RaiseClick(item);
    }

    internal void RaiseClick(FormMenuItem item) => VBControl.RaiseFor(this, item.Component, VBEvents.Click);

    // The bar is shown while one of its items is.
    internal void Follow() => IsVisible = Menu.ShowsTheBar;

    internal Control Create(FormMenuItem item) => item.IsSeparator ? new VBMenuSeparator(this, item) : new VBMenuItem(this, item);

    // VB's caption as Avalonia's header: an ampersand marks the access key, two show one, and an underscore is shown as itself.
    internal static string Header(string caption)
    {
        var header = new StringBuilder(caption.Length);
        var marked = false;
        for (var i = 0; i < caption.Length; i++)
        {
            switch (caption[i])
            {
                case '&' when i + 1 < caption.Length && caption[i + 1] == '&':
                    header.Append('&');
                    i++;
                    break;
                // A menu answers to the first character its caption marks.
                case '&' when i + 1 < caption.Length && !marked:
                    header.Append('_');
                    marked = true;
                    break;
                case '&':
                    break;
                case '_':
                    header.Append("__");
                    break;
                default:
                    header.Append(caption[i]);
                    break;
            }
        }
        return header.ToString();
    }
}

// A menu item. Every item can be ticked, so every one draws its tick when Checked.
internal sealed class VBMenuItem : MenuItem
{
    private readonly VBMenuBar bar;
    private readonly FormMenuItem item;
    private bool isChecked;

    public VBMenuItem(VBMenuBar bar, FormMenuItem item)
    {
        this.bar = bar;
        this.item = item;
        ToggleType = MenuItemToggleType.CheckBox;
        foreach (var inner in item.Items)
            Items.Add(bar.Create(inner));
        ComponentBinding.Bind(this, item.Component, VBClasses.Menu, Apply);
        AddHandler(SubmenuOpenedEvent, OnSubmenuOpened);
    }

    protected override Type StyleKeyOverride => typeof(MenuItem);

    protected override void OnClick(RoutedEventArgs e)
    {
        // Avalonia ticks or unticks a CheckBox item before its Click. The program's Checked, or CheckOnClick's, has the last word.
        SetCurrentValue(IsCheckedProperty, isChecked);
        if (item.Items.Count == 0)
        {
            bar.Chosen(item);
            SetCurrentValue(IsCheckedProperty, isChecked);
        }
        base.OnClick(e);
    }

    // A menu's Click comes as it opens. Opening a menu inside it raises its own, which reaches this one too.
    private void OnSubmenuOpened(object? sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(e.Source, this))
            bar.RaiseClick(item);
    }

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        switch (e.Property.Name)
        {
            case nameof(P.Caption):
                Header = VBMenuBar.Header((string)e.After!);
                break;
            case nameof(P.Enabled):
                IsEnabled = (bool)e.After!;
                break;
            case nameof(P.Checked):
                isChecked = (bool)e.After!;
                SetCurrentValue(IsCheckedProperty, isChecked);
                break;
            case nameof(P.Visible):
                IsVisible = (bool)e.After!;
                if (item.IsTopLevel)
                    bar.Follow();
                break;
            case nameof(P.Shortcut):
                InputGesture = ShortcutKeys.GestureOf((MenuShortcut?)e.After);
                break;
        }
    }
}

// A separator: a line, shown or hidden with its component.
internal sealed class VBMenuSeparator : Separator
{
    public VBMenuSeparator(VBMenuBar bar, FormMenuItem item) =>
        ComponentBinding.Bind(this, item.Component, VBClasses.Menu, e =>
        {
            if (ReferenceEquals(e.Property, P.Visible))
            {
                IsVisible = (bool)e.After!;
                if (item.IsTopLevel)
                    bar.Follow();
            }
        });

    protected override Type StyleKeyOverride => typeof(Separator);
}

// The keys a menu shortcut can name, with their VB KeyCodes.
internal static class ShortcutKeys
{
    private static readonly (Key Key, short KeyCode)[] Named =
    [
        (Key.Back, 8), (Key.Cancel, 3), (Key.CapsLock, 20), (Key.Delete, 46), (Key.Down, 40), (Key.End, 35), (Key.Enter, 13),
        (Key.Escape, 27), (Key.Help, 47), (Key.Home, 36), (Key.Insert, 45), (Key.Left, 37), (Key.NumLock, 144), (Key.PageDown, 34),
        (Key.PageUp, 33), (Key.PrintScreen, 44), (Key.Right, 39), (Key.Scroll, 145), (Key.Tab, 9), (Key.Up, 38),
    ];

    // The shortcut a key pressed is, or null for one no shortcut can name.
    public static MenuShortcut? Of(Key key, KeyModifiers modifiers)
    {
        if (modifiers.HasFlag(KeyModifiers.Meta) || CodeOf(key) is not { } code)
            return null;
        return new MenuShortcut(code, (ShortcutModifiers)ControlEvents.ShiftOf(modifiers));
    }

    public static KeyGesture? GestureOf(MenuShortcut? shortcut)
    {
        if (shortcut is not { } value || KeyOf(value.KeyCode) is not { } key)
            return null;
        var modifiers = (value.Modifiers.HasFlag(ShortcutModifiers.Shift) ? KeyModifiers.Shift : KeyModifiers.None)
                        | (value.Modifiers.HasFlag(ShortcutModifiers.Ctrl) ? KeyModifiers.Control : KeyModifiers.None)
                        | (value.Modifiers.HasFlag(ShortcutModifiers.Alt) ? KeyModifiers.Alt : KeyModifiers.None);
        return new KeyGesture(key, modifiers);
    }

    private static short? CodeOf(Key key) => key switch
    {
        >= Key.A and <= Key.Z => (short)('A' + (key - Key.A)),
        >= Key.D0 and <= Key.D9 => (short)('0' + (key - Key.D0)),
        >= Key.F1 and <= Key.F16 => (short)(112 + (key - Key.F1)),
        _ => Array.FindIndex(Named, n => n.Key == key) is >= 0 and var at ? Named[at].KeyCode : null,
    };

    private static Key? KeyOf(short code) => code switch
    {
        >= 65 and <= 90 => Key.A + (code - 'A'),
        >= 48 and <= 57 => Key.D0 + (code - '0'),
        >= 112 and <= 127 => Key.F1 + (code - 112),
        _ => Array.FindIndex(Named, n => n.KeyCode == code) is >= 0 and var at ? Named[at].Key : null,
    };
}
