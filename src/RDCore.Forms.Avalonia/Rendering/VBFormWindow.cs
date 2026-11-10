using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Menus;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Rendering;

/// <summary>
/// A window that shows a VB form: its <see cref="VBFormView"/>, titled with the form's caption.
/// </summary>
/// <remarks>
/// <para>
/// The window's client area is the form's: its size is the root's <c>Width</c> and <c>Height</c>, in pixels, and follows them. A user who resizes the window sets them. The form's menus are on its <see cref="MenuBar"/>, above the client area,
/// and a menu item's shortcut chooses it before a control or the root sees the key. Enter clicks the focused CommandButton, or else the
/// form's first CommandButton whose <c>Default</c> is True, and Esc the first whose <c>Cancel</c> is True, with no key event, the root's
/// included: a button that is hidden or disabled takes no key, and a combo's open list keeps both keys. While it is open, the window is its root's
/// <see cref="IMenuHost"/>, registered with <see cref="MenuHosts"/>, and a right release over a component whose <c>ContextMenu</c> names
/// one of the form's menus pops it up at the pointer, after the component's MouseUp and Click. The window follows the root's <c>Caption</c>, <c>MaxButton</c>,
/// <c>MinButton</c> and <c>ShowInTaskbar</c>. Its <c>StartUpPosition</c> chooses where it starts: CenterOwner and CenterScreen centre
/// it, and Manual and Windows Default leave its place to the window system.
/// </para>
/// <para>
/// The controls and the root raise their events to the sink set on the window with <see cref="VBControl.SetEventSink"/>. A window
/// with no sink raises none. The root's own events, as VB6 raises them:
/// </para>
/// <list type="bullet">
/// <item><description>
/// Resize, inside <see cref="Window.Show()"/> the first time the window is shown, and once for each change of the root's
/// <c>Width</c> or <c>Height</c> after that. A size the root already has raises none, and neither does showing the window again
/// after it was hidden.
/// </description></item>
/// <item><description>
/// Activate, as the window becomes the active one of the program's form windows, after the Resize that shows it; Deactivate on the
/// window that was, before it. Another application taking the activation and giving it back raises nothing. A window whose root is
/// disabled is shown without being activated.
/// </description></item>
/// <item><description>
/// The focus, as the window is activated: after the Activate, the control that last had the focus on the form takes it again, or the
/// first control of the tab order that can take it, with a GotFocus. A form with none raises its own GotFocus. The control that has the
/// focus raises its LostFocus before the form's Deactivate, and a form with none its own.
/// </description></item>
/// <item><description>
/// DblClick, on a double click's second press on none of the form's controls, in place of its MouseDown.
/// </description></item>
/// <item><description>
/// QueryUnload, then Unload, as the user closes the window, with an <c>UnloadMode</c> of 0. A handler that sets <c>Cancel</c> to
/// another value keeps the window open. <see cref="Unload"/> raises them as VB's <c>Unload</c> statement does, and
/// <see cref="Window.Close()"/> closes the window and raises neither, as VB's <c>End</c> does. A window whose
/// <see cref="UnloadRequestHandler"/> is set raises neither as the user closes it: it stays open and hands the close to the handler.
/// </description></item>
/// <item><description>
/// KeyDown, KeyPress and KeyUp, for a key pressed while none of the form's controls has the focus, and for every key first where
/// the root's <c>KeyPreview</c> is True.
/// </description></item>
/// </list>
/// <para>
/// Initialize, Load and Terminate are the program's to raise: the window raises none of them.
/// </para>
/// </remarks>
public sealed class VBFormWindow : Window, IMenuHost, IFormFocus
{
    // The window an Activate was last raised for: the program's active form.
    private static VBFormWindow? active;

    // Whether the window has been shown, and the root's size when Resize was last raised.
    private bool opened;
    private (double Width, double Height) resizedAt;
    // The window system activated the window before it was shown: Activate follows the Resize that shows it.
    private bool activatePending;
    // Set while the window's size goes to the root, so that the two values raise one Resize.
    private bool following;
    // Set while QueryUnload and Unload are raised, and once the form has unloaded.
    private bool unloading;
    // Where the pointer was last seen, in the client area's pixels: where a menu pops up when no point is given.
    private Point pointer;
    // Enter or Esc, while a button took it, or an open list kept it, and its KeyUp is to raise nothing.
    private Key? keyUpTaken;
    // The control that has VB's focus, while the form is the program's active one, and the control that last had it: VB's
    // ActiveControl, which takes the focus again as the form is activated again. The root has the focus itself where the form had
    // no control to take it.
    private Control? focused;
    private Control? activeControl;
    private bool rootFocused;

    /// <summary>
    /// Creates a window that shows a form.
    /// </summary>
    /// <param name="form">The form.</param>
    /// <exception cref="ArgumentNullException"><paramref name="form"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The root of <paramref name="form"/> is not a <see cref="VBClasses.Form"/>.</exception>
    public VBFormWindow(FormDefinition form)
    {
        ArgumentNullException.ThrowIfNull(form);
        if (!ReferenceEquals(form.Root.Class, VBClasses.Form))
            throw new ArgumentException($"The form's root is a {form.Root.Class.TypeName}, not a VB.Form.", nameof(form));
        View = new VBFormView(form);
        MenuBar = new VBMenuBar(form.Root);
        DockPanel.SetDock(MenuBar, Dock.Top);
        // A window of no pixels has no surface to draw: closing it fails as it renders a last frame. The window takes its content's size.
        Content = new DockPanel { MinWidth = 1, MinHeight = 1, Children = { MenuBar, View } };
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = form.Root.Get(P.StartUpPosition) switch
        {
            StartUpPosition.CenterOwner => WindowStartupLocation.CenterOwner,
            StartUpPosition.CenterScreen => WindowStartupLocation.CenterScreen,
            _ => WindowStartupLocation.Manual,
        };
        Activated += (_, _) => OnWindowActivated();
        AddHandler(KeyDownEvent, OnShortcut, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnDialogKey, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnDialogKeyUp, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
        AddHandler(TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, (_, e) => pointer = e.GetPosition(View), RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerPressedEvent, (_, e) => pointer = e.GetPosition(View), RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnPointerReleasedOverForm, RoutingStrategies.Bubble, handledEventsToo: true);
        Apply();
    }

    /// <summary>
    /// The form's client area.
    /// </summary>
    public VBFormView View { get; }

    /// <summary>
    /// The form's menu bar, above its client area: hidden while none of the bar's items is visible.
    /// </summary>
    public VBMenuBar MenuBar { get; }

    /// <summary>
    /// The form's root.
    /// </summary>
    public ComponentInstance Root => View.Form.Root;

    /// <summary>
    /// Pops up the items of one of the form's menus, as VB's <c>PopupMenu</c> method does, at a point of the client area. The menu
    /// raises its Click as it opens, as it does on the bar, and an item chosen from it raises its own.
    /// </summary>
    /// <param name="menu">The menu whose items pop up: a hidden top-level menu is the usual one.</param>
    /// <param name="x">The point's horizontal coordinate in the root's scale, or <see langword="null"/> for the pointer's.</param>
    /// <param name="y">The point's vertical coordinate in the root's scale, or <see langword="null"/> for the pointer's.</param>
    /// <param name="alignment">Where the menu goes across from the point.</param>
    /// <param name="boldItem">
    /// An item of the menu, at any depth, to show in bold, as VB's <c>BoldCommand</c> argument, or <see langword="null"/>. An item
    /// that is not in the menu bolds none.
    /// </param>
    /// <returns>
    /// A task that completes when the menu closes, after the Click of an item chosen from it. A menu that holds no items shows nothing
    /// and raises nothing, and the task returned has already completed.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="menu"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="menu"/> is not one of the form's menus.</exception>
    public Task PopupMenu(FormMenuItem menu, double? x = null, double? y = null, PopupMenuAlignment alignment = PopupMenuAlignment.Left,
        FormMenuItem? boldItem = null)
    {
        ArgumentNullException.ThrowIfNull(menu);
        if (!MenuBar.Menu.AllItems.Contains(menu))
            throw new ArgumentException($"'{menu.Component.Name}' is not one of the form's menus.", nameof(menu));
        if (menu.Items.Count == 0)
            return Task.CompletedTask;
        var at = PointOf(x, y);
        MenuBar.RaiseClick(menu);
        var popup = new ContextMenu
        {
            Placement = PlacementMode.AnchorAndGravity,
            PlacementAnchor = PopupAnchor.TopLeft,
            PlacementGravity = alignment switch
            {
                PopupMenuAlignment.Center => PopupGravity.Bottom,
                PopupMenuAlignment.Right => PopupGravity.BottomLeft,
                _ => PopupGravity.BottomRight,
            },
            PlacementRect = new Rect(at, new Size(1, 1)),
        };
        foreach (var item in menu.Items)
            popup.Items.Add(MenuBar.Create(item));
        if (boldItem is not null)
            foreach (var shown in popup.GetLogicalDescendants().OfType<VBMenuItem>().Where(shown => ReferenceEquals(shown.Item, boldItem)))
                shown.FontWeight = FontWeight.Bold;
        var closed = new TaskCompletionSource();
        popup.Closed += (_, _) =>
        {
            // The items leave the popup, so that they stop following their components.
            popup.Items.Clear();
            if (ReferenceEquals(PoppedUpMenu, popup))
                PoppedUpMenu = null;
            closed.TrySetResult();
        };
        PoppedUpMenu = popup;
        popup.Open(View);
        return closed.Task;
    }

    /// <summary>
    /// The menu <see cref="PopupMenu"/> last popped up, while it is open: <see langword="null"/> once it has closed.
    /// </summary>
    public ContextMenu? PoppedUpMenu { get; private set; }

    /// <summary>
    /// What handles a close the user or the system asks for, in place of the window's own QueryUnload and Unload: <see langword="null"/>,
    /// the default, for a window that raises them itself.
    /// </summary>
    /// <remarks>
    /// A host whose program raises the form's QueryUnload and Unload itself, such as one whose program runs in another process, sets it.
    /// The window then refuses the close, raises neither event, and calls the handler with the close's <c>UnloadMode</c>: 0 for the
    /// close box or Alt+F4, 2 as the system shuts down, 5 as the window's owner closes. The handler is called on the UI thread after the
    /// close is refused, never inside it. Once the program has unloaded the form, the host closes the window with
    /// <see cref="Window.Close()"/>, which raises nothing; a program that cancelled leaves it open.
    /// </remarks>
    public Action<int>? UnloadRequestHandler { get; set; }

    /// <summary>
    /// Unloads the form as VB's <c>Unload</c> statement does: raises the root's QueryUnload, with an <c>UnloadMode</c> of 1, then its
    /// Unload, and closes the window unless a handler set <c>Cancel</c>.
    /// </summary>
    /// <returns>
    /// Whether the form unloaded: <see langword="false"/> where a handler cancelled, and where it is called from the root's own
    /// QueryUnload or Unload, which does nothing.
    /// </returns>
    public bool Unload()
    {
        if (unloading || !Unloads(1))
            return false;
        unloading = true;
        Close();
        return true;
    }

    // It follows its root while it is open, so that a closed window holds no handler of its root's.
    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        MenuHosts.Register(this);
        Root.PropertyChanged -= OnRootChanged;
        Root.PropertyChanged += OnRootChanged;
        Apply();
        if (opened)
            return;
        opened = true;
        resizedAt = SizeOfRoot();
        VBControl.Raise(View, VBEvents.Resize);
        if (activatePending)
        {
            activatePending = false;
            TakeActivation();
        }
    }

    /// <inheritdoc/>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Close from code raises nothing. The user's close is refused while QueryUnload and Unload run, and goes to the handler, outside
        // the close, where one is set.
        if (unloading)
            e.Cancel |= !e.IsProgrammatic;
        else if (!e.IsProgrammatic && UnloadModeOf(e.CloseReason) is { } mode)
        {
            if (UnloadRequestHandler is { } handler)
            {
                e.Cancel = true;
                Dispatcher.UIThread.Post(() => handler(mode));
            }
            else if (!Unloads(mode))
                e.Cancel = true;
        }
        base.OnClosing(e);
    }

    /// <inheritdoc/>
    protected override void OnClosed(EventArgs e)
    {
        Root.PropertyChanged -= OnRootChanged;
        MenuHosts.Unregister(this);
        if (ReferenceEquals(active, this))
            active = null;
        focused = null;
        rootFocused = false;
        base.OnClosed(e);
    }

    /// <inheritdoc/>
    protected override void OnResized(WindowResizedEventArgs e)
    {
        base.OnResized(e);
        if (!opened || e.Reason != WindowResizeReason.User)
            return;
        following = true;
        try
        {
            Root.Set(P.Width, Math.Round(e.ClientSize.Width)).Set(P.Height, Math.Round(e.ClientSize.Height));
        }
        finally
        {
            following = false;
        }
        RaiseResize();
    }

    private void OnRootChanged(object? sender, ComponentPropertyChangedEventArgs e)
    {
        Apply();
        if (following || e.Property.Name is not (nameof(P.Width) or nameof(P.Height)))
            return;
        // A size set from code is the window's again, after the user resized it.
        SizeToContent = SizeToContent.WidthAndHeight;
        RaiseResize();
    }

    private (double Width, double Height) SizeOfRoot() => (Root.Get(P.Width), Root.Get(P.Height));

    private void RaiseResize()
    {
        if (!opened || SizeOfRoot() == resizedAt)
            return;
        resizedAt = SizeOfRoot();
        VBControl.Raise(View, VBEvents.Resize);
    }

    private void OnWindowActivated()
    {
        if (opened)
            TakeActivation();
        else
            activatePending = true;
    }

    // The focus as VB moves it between forms: the old form's control's LostFocus, the old form's Deactivate, the new form's Activate,
    // then the new form's control's GotFocus. A form with no control to take the focus raises its own GotFocus and LostFocus.
    private void TakeActivation()
    {
        if (ReferenceEquals(active, this) || !Root.Get(P.Enabled))
            return;
        var previous = active;
        active = this;
        if (previous is not null)
        {
            previous.LoseFocus();
            VBControl.Raise(previous.View, VBEvents.Deactivate);
        }
        VBControl.Raise(View, VBEvents.Activate);
        TakeFocus();
    }

    // The form's ActiveControl takes the focus again, or else the first control of the tab order, or else the root.
    private void TakeFocus()
    {
        var control = activeControl is not null && VBControl.GetInstance(activeControl) is { } instance
                      && ReferenceEquals(View.ControlOf(instance), activeControl) && VBFormView.CanTakeTheFocus(activeControl)
            ? activeControl
            : View.FirstToTakeTheFocus();
        if (control is null)
        {
            rootFocused = true;
            VBControl.Raise(View, VBEvents.GotFocus);
            return;
        }
        // The window system's focus raises it, where it moves; where it already is on the control, the window raises it.
        focused = null;
        control.Focus();
        if (focused is null)
            ((IFormFocus)this).FocusMoved(control, got: true, toNone: false);
    }

    private void LoseFocus()
    {
        if (focused is { } control)
        {
            focused = null;
            VBControl.Raise(control, VBEvents.LostFocus);
        }
        else if (rootFocused)
        {
            rootFocused = false;
            VBControl.Raise(View, VBEvents.LostFocus);
        }
    }

    // The window system moves the focus as it activates and deactivates windows, the program's or another application's, taking it
    // to no element and giving it back; VB raises nothing for that. Between the controls of the active form, the focus raises
    // LostFocus and GotFocus as it moves.
    void IFormFocus.FocusMoved(Control control, bool got, bool toNone)
    {
        if (got)
        {
            activeControl = control;
            if (!ReferenceEquals(active, this) || ReferenceEquals(control, focused))
                return;
            // The root, or a control that lost the window system's focus to none, as it was hidden or disabled.
            LoseFocus();
            focused = control;
            VBControl.Raise(control, VBEvents.GotFocus);
        }
        else if (ReferenceEquals(control, focused) && !toNone && IsActive)
        {
            focused = null;
            VBControl.Raise(control, VBEvents.LostFocus);
        }
    }

    // VB's UnloadMode for a close the user or the system asks for: none for a close VB has no mode for.
    private static int? UnloadModeOf(WindowCloseReason reason) => reason switch
    {
        WindowCloseReason.WindowClosing => 0,
        WindowCloseReason.OSShutdown => 2,
        WindowCloseReason.OwnerWindowClosing => 5,
        _ => null,
    };

    // Raises QueryUnload, then Unload, and answers whether neither handler cancelled.
    private bool Unloads(int mode)
    {
        unloading = true;
        try
        {
            return !Cancelled(VBControl.Raise(View, VBEvents.QueryUnload, FormValue.FromInteger(0), FormValue.FromInteger((short)mode)))
                   && !Cancelled(VBControl.Raise(View, VBEvents.Unload, FormValue.FromInteger(0)));
        }
        finally
        {
            unloading = false;
        }
    }

    private static bool Cancelled(FormValue[]? after) =>
        after is { } arguments && arguments[0].Kind == FormValueKind.Integer && arguments[0].AsInteger() != 0;

    // Whether a key is the root's: none of the form's controls has the focus, or, where the root's KeyPreview is True, one that
    // raises no key events of its own does. A control that raises its own gives the root the key first itself.
    private bool KeyIsTheRoots(object? source)
    {
        if (!View.IsEffectivelyEnabled)
            return false;
        // The key's own element first: a combo's open list is in a popup, whose focus is not the window's, and whose element is the
        // combo's.
        var owner = ControlKeys.Owner(source) ?? ControlKeys.Owner(FocusManager?.GetFocusedElement());
        if (ReferenceEquals(owner, View))
            owner = null;
        return owner is not IRaisesKeyEvents && (owner is null || Root.Get(P.KeyPreview));
    }

    // A menu item's shortcut chooses it before any control or the root sees the key. Where two items share one, the first in the
    // form's order has it.
    private void OnShortcut(object? sender, KeyEventArgs e)
    {
        if (e.Handled || !View.IsEffectivelyEnabled || ShortcutKeys.Of(e.Key, e.KeyModifiers) is not { } pressed
            || MenuBar.Menu.AllItems.FirstOrDefault(item => item.Shortcut == pressed && !item.IsSeparator) is not { } chosen)
            return;
        e.Handled = true;
        MenuBar.Choose(chosen);
    }

    // A point in the root's scale as the client area's pixels, the pointer's coordinate for one not given.
    private Point PointOf(double? x, double? y)
    {
        var (across, down) = VBDrawingLayer.ScaleOf(Root).ToTwips(x ?? 0, y ?? 0);
        return new Point(x is null ? pointer.X : across / ScaleModeExtensions.TwipsPerPixel,
            y is null ? pointer.Y : down / ScaleModeExtensions.TwipsPerPixel);
    }

    // A right release over a component whose ContextMenu names one of the form's menus pops it up, at the pointer, after the
    // component's own MouseUp and Click.
    private void OnPointerReleasedOverForm(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Right || !View.IsEffectivelyEnabled || ComponentUnder(e.Source) is not { } component
            || !component.Class.Has(P.ContextMenu) || component.Get(P.ContextMenu) is not { Length: > 0 } name
            || MenuBar.Menu.AllItems.FirstOrDefault(item => string.Equals(item.Component.Name, name, StringComparison.OrdinalIgnoreCase))
                is not { } menu)
            return;
        pointer = e.GetPosition(View);
        _ = PopupMenu(menu);
    }

    // The innermost component of the client area a pointer event came from: the root for the area itself, none outside it.
    private ComponentInstance? ComponentUnder(object? source)
    {
        for (var visual = source as Visual; visual is not null; visual = visual.GetVisualParent())
        {
            if (ReferenceEquals(visual, View))
                return Root;
            if (visual is Control control && VBControl.GetInstance(control) is { } instance && !ReferenceEquals(instance, Root))
                return View.IsVisualAncestorOf(control) ? instance : null;
        }
        return null;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || !KeyIsTheRoots(e.Source) || KeyCodes.Of(e) is not { } code)
            return;
        FormKeys.Raise(View, VBEvents.KeyDown, code, ControlEvents.ShiftOf(e.KeyModifiers));
        // Backspace and Ctrl+A type a character with no text, as in a TextBox, and so do Enter and Esc.
        if (e.Key == Key.Back)
            FormKeys.Raise(View, VBEvents.KeyPress, 8);
        else if (e.Key == Key.A && e.KeyModifiers == KeyModifiers.Control)
            FormKeys.Raise(View, VBEvents.KeyPress, 1);
        else if (ControlKeys.AsciiOf(e) is { } ascii)
            FormKeys.Raise(View, VBEvents.KeyPress, ascii);
    }

    // Enter clicks the focused CommandButton, or else the form's default button, and Esc the form's cancel button, before any key
    // event, the root's included, and the key raises no KeyUp after it. An open list keeps both keys, and raises no KeyUp for one
    // where the form has a button for it.
    private void OnDialogKey(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Escape))
            return;
        keyUpTaken = null;
        if (e.Handled || e.KeyModifiers != KeyModifiers.None || !View.IsEffectivelyEnabled)
            return;
        var owner = ControlKeys.Owner(e.Source) ?? ControlKeys.Owner(FocusManager?.GetFocusedElement());
        var button = ButtonFor(e.Key == Key.Enter ? P.Default : P.Cancel);
        if (owner is VBComboBox { IsDropDownOpen: true })
        {
            if (button is not null)
                keyUpTaken = e.Key;
            return;
        }
        if (e.Key == Key.Enter && owner is VBCommandButton focused)
            button = focused;
        if (button is null)
            return;
        e.Handled = true;
        keyUpTaken = e.Key;
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }

    private void OnDialogKeyUp(object? sender, KeyEventArgs e)
    {
        if (keyUpTaken != e.Key)
            return;
        keyUpTaken = null;
        e.Handled = true;
    }

    // The button that Enter or Esc clicks: the first of the form's CommandButtons whose Default or Cancel is True, while it is shown
    // and enabled. One that is hidden or disabled takes no key.
    private VBCommandButton? ButtonFor(PropertyDefinition<bool> property) =>
        View.Form.Components.FirstOrDefault(component => ReferenceEquals(component.Class, VBClasses.CommandButton) && component.Get(property))
            is { } chosen && View.ControlOf(chosen) is VBCommandButton { IsEffectivelyEnabled: true, IsEffectivelyVisible: true } button
            ? button
            : null;

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (!e.Handled && KeyIsTheRoots(e.Source) && KeyCodes.Of(e) is { } code)
            FormKeys.Raise(View, VBEvents.KeyUp, code, ControlEvents.ShiftOf(e.KeyModifiers));
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        // A character outside ASCII raises no KeyPress, as in a TextBox.
        if (e.Handled || string.IsNullOrEmpty(e.Text) || e.Text.Any(character => character > 127) || !KeyIsTheRoots(e.Source))
            return;
        foreach (var character in e.Text)
            FormKeys.Raise(View, VBEvents.KeyPress, (short)character);
    }

    private void Apply()
    {
        Title = Root.Get(P.Caption);
        CanMaximize = Root.Get(P.MaxButton);
        CanMinimize = Root.Get(P.MinButton);
        ShowInTaskbar = Root.Get(P.ShowInTaskbar);
        ShowActivated = Root.Get(P.Enabled);
        MenuBar.IsEnabled = Root.Get(P.Enabled);
    }
}
