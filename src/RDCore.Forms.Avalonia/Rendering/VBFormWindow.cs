using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Rendering;

/// <summary>
/// A window that shows a VB form: its <see cref="VBFormView"/>, titled with the form's caption.
/// </summary>
/// <remarks>
/// <para>
/// The window's client area is the form's: its size is the root's <c>Width</c> and <c>Height</c>, in twips at 15 twips a pixel,
/// and follows them. A user who resizes the window sets them. The form's menus are on its <see cref="MenuBar"/>, above the client area,
/// and a menu item's shortcut chooses it before a control or the root sees the key. The window follows the root's <c>Caption</c>, <c>MaxButton</c>,
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
/// QueryUnload, then Unload, as the user closes the window, with an <c>UnloadMode</c> of 0. A handler that sets <c>Cancel</c> to
/// another value keeps the window open. <see cref="Unload"/> raises them as VB's <c>Unload</c> statement does, and
/// <see cref="Window.Close()"/> closes the window and raises neither, as VB's <c>End</c> does.
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
public sealed class VBFormWindow : Window
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
        Content = new DockPanel { Children = { MenuBar, View } };
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = form.Root.Get(P.StartUpPosition) switch
        {
            StartUpPosition.CenterOwner => WindowStartupLocation.CenterOwner,
            StartUpPosition.CenterScreen => WindowStartupLocation.CenterScreen,
            _ => WindowStartupLocation.Manual,
        };
        Activated += (_, _) => OnWindowActivated();
        AddHandler(KeyDownEvent, OnShortcut, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
        AddHandler(TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
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

    private ComponentInstance Root => View.Form.Root;

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
        // Close from code raises nothing. The user's close is refused while QueryUnload and Unload run.
        if (unloading)
            e.Cancel |= !e.IsProgrammatic;
        else if (!e.IsProgrammatic && UnloadModeOf(e.CloseReason) is { } mode && !Unloads(mode))
            e.Cancel = true;
        base.OnClosing(e);
    }

    /// <inheritdoc/>
    protected override void OnClosed(EventArgs e)
    {
        Root.PropertyChanged -= OnRootChanged;
        if (ReferenceEquals(active, this))
            active = null;
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
            Root.Set(P.Width, Math.Round(e.ClientSize.Width) * ScaleModeExtensions.TwipsPerPixel)
                .Set(P.Height, Math.Round(e.ClientSize.Height) * ScaleModeExtensions.TwipsPerPixel);
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

    private void TakeActivation()
    {
        if (ReferenceEquals(active, this) || !Root.Get(P.Enabled))
            return;
        var previous = active;
        active = this;
        if (previous is not null)
            VBControl.Raise(previous.View, VBEvents.Deactivate);
        VBControl.Raise(View, VBEvents.Activate);
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
    private bool KeyIsTheRoots()
    {
        if (!View.IsEffectivelyEnabled)
            return false;
        Control? owner = null;
        for (var visual = FocusManager?.GetFocusedElement() as Visual; visual is not null && !ReferenceEquals(visual, View);
             visual = visual.GetVisualParent())
            if (visual is Control control && VBControl.GetInstance(control) is not null)
            {
                owner = control;
                break;
            }
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

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || !KeyIsTheRoots() || KeyCodes.Of(e) is not { } code)
            return;
        FormKeys.Raise(View, VBEvents.KeyDown, code, ControlEvents.ShiftOf(e.KeyModifiers));
        // Backspace and Ctrl+A type a character with no text, as in a TextBox.
        if (e.Key == Key.Back)
            FormKeys.Raise(View, VBEvents.KeyPress, 8);
        else if (e.Key == Key.A && e.KeyModifiers == KeyModifiers.Control)
            FormKeys.Raise(View, VBEvents.KeyPress, 1);
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (!e.Handled && KeyIsTheRoots() && KeyCodes.Of(e) is { } code)
            FormKeys.Raise(View, VBEvents.KeyUp, code, ControlEvents.ShiftOf(e.KeyModifiers));
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        // A character outside ASCII raises no KeyPress, as in a TextBox.
        if (e.Handled || string.IsNullOrEmpty(e.Text) || e.Text.Any(character => character > 127) || !KeyIsTheRoots())
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
