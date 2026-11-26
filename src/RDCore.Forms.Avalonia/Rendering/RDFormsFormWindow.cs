using System.IO.Abstractions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Drawing;
using RDCore.Forms.Menus;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Avalonia.Rendering;

/// <summary>
/// A window that shows a VB form: its <see cref="RDFormsFormView"/>, titled with the form's caption.
/// </summary>
/// <remarks>
/// <para>
/// The window's client area is the form's: its size is the root's <c>Width</c> and <c>Height</c>, in pixels, and follows them. A user
/// who resizes the window sets them. The client area scrolls in its <see cref="Scroller"/>, by the root's <c>ScrollLeft</c> and
/// <c>ScrollTop</c>, over at least its <c>ScrollWidth</c> by <c>ScrollHeight</c>, and raises the root's Scroll. The form's menus are on its
/// <see cref="MenuBar"/>, above the client area, and a menu item's shortcut chooses it before a control or the root sees the key. Enter clicks the
/// focused CommandButton, or else the form's first CommandButton whose <c>Default</c> is True, and Esc the first whose <c>Cancel</c> is True, with
/// no key event, the root's included: a button that is hidden or disabled takes no key, and a combo's open list keeps both keys. The button Enter clicks has
/// the default look, and no other (see <see cref="RDFormsCommandButton"/>). While it is open, the window is its root's
/// <see cref="IMenuHost"/>, registered with <see cref="MenuHosts"/>, and a right release over a component whose <c>ContextMenu</c> names
/// one of the form's menus pops it up at the pointer, after the component's MouseUp and Click. The window follows the root's <c>Caption</c>, <c>Icon</c>,
/// <c>MaxButton</c>, <c>MinButton</c> and <c>ShowInTaskbar</c>. Its <c>StartUpPosition</c> chooses where it starts: CenterOwner and CenterScreen centre
/// it, and Manual and Windows Default leave its place to the window system.
/// </para>
/// <para>
/// The controls and the root raise their events to the sink set on the window with <see cref="RDFormsControl.SetEventSink"/>. A window
/// with no sink raises none. The root's own events, as VB6 raises them:
/// </para>
/// <list type="bullet">
/// <item><description>
/// Resize, once for each change of the root's <c>Width</c> or <c>Height</c>, whether or not the window is shown, and inside
/// <see cref="Window.Show()"/> the first time the window is shown, whatever its size. A size the root already has raises none, and
/// neither does showing the window again after it was hidden.
/// </description></item>
/// <item><description>
/// Activate, as the window becomes the active one of its <see cref="Program"/>'s form windows, after the Resize that shows it; Deactivate
/// on the window of that program that was, before it. Another application, or another program's window, taking the activation and giving
/// it back raises nothing. A window whose root is disabled is shown without being activated.
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
public sealed class RDFormsFormWindow : Window, IMenuHost, IFormFocus
{
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
    // The root's Icon the window's icon was made from, once it was made.
    private byte[]? iconFrom;
    private bool iconMade;
    // The control that has VB's focus, while the form is the program's active one, and the control that last had it: VB's
    // ActiveControl, which takes the focus again as the form is activated again. The root has the focus itself where the form had
    // no control to take it.
    private Control? focused;

    // The control a Validate or a BeforeUpdate kept the focus on, while the focus goes back to it: the moves to the other control and
    // back raise nothing.
    private Control? keeping;

    // The control a SetFocus gives the focus to, while it takes it: that move raises no Validate.
    private Control? setFocusTo;
    private Control? activeControl;
    private bool rootFocused;

    /// <summary>
    /// Creates a window that shows a form.
    /// </summary>
    /// <param name="form">The form.</param>
    /// <exception cref="ArgumentNullException"><paramref name="form"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The root of <paramref name="form"/> is not a <see cref="ComponentClasses.Form"/>.</exception>
    /// <remarks>
    /// Its file lists show the machine's file system, from the process's current directory.
    /// </remarks>
    public RDFormsFormWindow(FormDefinition form)
        : this(form, new FileSystem())
    {
    }

    /// <summary>
    /// Creates a window that shows a form, with the file system its file lists show.
    /// </summary>
    /// <param name="form">The form.</param>
    /// <param name="fileSystem">The file system the form's file lists show, from its current directory.</param>
    /// <exception cref="ArgumentNullException"><paramref name="form"/> or <paramref name="fileSystem"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The root of <paramref name="form"/> is not a <see cref="ComponentClasses.Form"/>.</exception>
    public RDFormsFormWindow(FormDefinition form, IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(fileSystem);
        if (!ReferenceEquals(form.Root.Class, ComponentClasses.Form))
            throw new ArgumentException($"The form's root is a {form.Root.Class.TypeName}, not an RDForms.Form.", nameof(form));
        View = new RDFormsFormView(form, fileSystem);
        MenuBar = new RDFormsMenuBar(form.Root);
        DockPanel.SetDock(MenuBar, Dock.Top);
        // The client area scrolls by the form's ScrollLeft and ScrollTop, and keeps the form's size.
        Scroller = new RDFormsScroller(form.Root, View, View, followsSize: true);
        // A window of no pixels has no surface to draw: closing it fails as it renders a last frame. The window takes its content's size.
        Content = new DockPanel { MinWidth = 1, MinHeight = 1, Children = { MenuBar, Scroller } };
        // Light, whatever the application's theme, until the host keeps the system's.
        RequestedThemeVariant = ThemeVariant.Light;
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
        // A form loaded and not shown raises Resize as its size changes, as VB6's does.
        resizedAt = SizeOfRoot();
        Root.PropertyChanged += OnRootChanged;
    }

    /// <summary>
    /// The form's client area.
    /// </summary>
    public RDFormsFormView View { get; }

    /// <summary>
    /// The program the form is one of, whose windows share one active form: Activate, Deactivate, and the GotFocus and LostFocus that come with them, follow
    /// the activation among that program's windows only.
    /// </summary>
    /// <remarks>
    /// <see cref="RDFormsProgram.Default"/> unless it is set as the window is made: every window given none is of the one program.
    /// </remarks>
    public RDFormsProgram Program { get; init; } = RDFormsProgram.Default;

    /// <summary>
    /// The scroll viewer the client area is in: its size is the form's <c>Width</c> and <c>Height</c>, and its offset the form's
    /// <c>ScrollLeft</c> and <c>ScrollTop</c>.
    /// </summary>
    public ScrollViewer Scroller { get; }

    /// <summary>
    /// The form's menu bar, above its client area: hidden while none of the bar's items is visible.
    /// </summary>
    public RDFormsMenuBar MenuBar { get; }

    /// <summary>
    /// Whether the window keeps the system's theme: RD-VBA's <c>App.KeepSystemTheme</c>, which the host gives each window it makes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see langword="false"/>, the default, shows the form in the light theme variant whatever the application's theme, as VB6 shows a
    /// form in Windows' light colours. <see langword="true"/> shows it in the application's theme variant, and follows it as it changes.
    /// </para>
    /// <para>
    /// The form's system colours, its menu bar's included, are drawn in the window's variant, with <see cref="FormPalette"/>'s palette
    /// for it; a colour set as RGB is drawn as it is. The value can change while the window is shown: the form is drawn again in its new
    /// variant. Each window has its own value.
    /// </para>
    /// </remarks>
    public bool KeepSystemTheme
    {
        get => View.KeepSystemTheme;
        set
        {
            RequestedThemeVariant = value ? null : ThemeVariant.Light;
            View.KeepSystemTheme = value;
        }
    }

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
            foreach (var shown in popup.GetLogicalDescendants().OfType<RDFormsMenuItem>().Where(shown => ReferenceEquals(shown.Item, boldItem)))
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

    /// <summary>
    /// The component whose control has the focus on the form, or had it last: VB's <c>ActiveControl</c>. <see langword="null"/> where
    /// none of the form's controls has had the focus.
    /// </summary>
    /// <remarks>
    /// A <c>SetFocus</c> call names its control here at once, before the focus moves.
    /// </remarks>
    public ComponentInstance? ActiveControl => activeControl is { } control ? RDFormsControl.GetInstance(control) : null;

    /// <summary>
    /// Runs one of the methods of the root or of a control of the form, or reads one of the properties the window answers, as a
    /// program's code calls it. Call it on the UI thread.
    /// </summary>
    /// <param name="component">The root, or a component the form shows.</param>
    /// <param name="method">
    /// The method, or the property (<see cref="MethodDefinition.IsProperty"/>): one of <paramref name="component"/>'s class's
    /// <see cref="ComponentClass.Methods"/>.
    /// </param>
    /// <param name="arguments">
    /// The call's arguments, in the order of the method's parameters: <see langword="null"/> for an argument left out, and none past the
    /// last one given. A <c>Variant</c> parameter takes any value and the method coerces it; a parameter of another type takes a value of
    /// that type.
    /// </param>
    /// <param name="unit">The unit the code reads and writes positions and sizes in, which <c>Move</c> takes.</param>
    /// <returns>The method's result, <see cref="FormValue.Empty"/> for a method that returns none; or the property's value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="component"/>, <paramref name="method"/> or <paramref name="arguments"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="component"/> is neither the root nor shown on the form, or <paramref name="method"/> is not a method of its class.
    /// </exception>
    /// <exception cref="InvalidOperationException">The call is not on the UI thread.</exception>
    /// <exception cref="FormErrorException">
    /// The call is refused with VB's error, and changes nothing:
    /// <list type="bullet">
    /// <item><description>450 for too many arguments, 449 for a required one left out, 448 for one left out before one given;</description></item>
    /// <item><description>13 for a value that is not a number, 94 for Null where <c>Move</c> takes a number, and 6 for a number out of range;</description></item>
    /// <item><description>5 for a value the method refuses, and for <c>SetFocus</c> where nothing can take the focus;</description></item>
    /// <item><description>438 for a method of the class that the window does not run.</description></item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// <para>
    /// <c>Move</c> sets <c>Left</c>, then <c>Top</c>, <c>Width</c> and <c>Height</c>, as many as it is given, in <paramref name="unit"/>
    /// (see <see cref="CodeUnits"/>). It checks every value before it sets one: a negative size is error 5, and a value past a Long in
    /// twips is error 6. In <see cref="CodeUnit.VB6"/>, a value is rounded to a whole twip, half to even. On the root, a <c>Move</c> that
    /// changes the size raises one Resize, where setting <c>Width</c> and then <c>Height</c> raises two.
    /// </para>
    /// <para>
    /// <c>SetFocus</c> is error 5 while the window is not shown, and on a control that is hidden or disabled. Otherwise
    /// <see cref="ActiveControl"/> names the control at once, and the focus moves after the call returns, with the LostFocus of the control
    /// that had it, then the GotFocus of this one. Where another of the program's forms is the active one, this window is activated. A
    /// control that has the focus already raises nothing, and so does the root's <c>SetFocus</c> on the active form. <c>SetFocus</c> raises
    /// no Validate. A disabled or hidden container makes its controls refuse the focus, and the root's <c>SetFocus</c> activates a shown form
    /// that is not the active one: these two rules were not measured with VB6, and RDForms sets them.
    /// </para>
    /// <para>
    /// <c>ZOrder</c> brings a control to the front of its plane in its container, for a <c>Position</c> of 0 or none, and sends it to the
    /// back for 1. A windowless control stays under every windowed one, and a container takes its controls with it. The position is an
    /// Integer, rounded half to even: another number is error 5, and a string that is not a number, or Null, is error 13.
    /// </para>
    /// <para>
    /// <c>Refresh</c> draws the component and its contents again. On the root or a PictureBox whose <c>AutoRedraw</c> is False, it raises
    /// Paint inside the call. VB6's <c>Refresh</c> was not measured: RDForms sets this rule.
    /// </para>
    /// <para>
    /// A Data control's <c>UpdateRecord</c> and <c>UpdateControls</c> are as <see cref="RDFormsData.UpdateRecord"/> and
    /// <see cref="RDFormsData.UpdateControls"/>: error 91, as the control opens no recordset.
    /// </para>
    /// <para>
    /// A ListBox's and a ComboBox's list members are as <see cref="RDFormsListBox"/> and <see cref="RDFormsComboBox"/> run them: <c>AddItem</c>,
    /// <c>RemoveItem</c> and <c>Clear</c> return none, and a property that <see cref="MethodDefinition.IsProperty"/> returns its value
    /// (<see cref="Assign"/> assigns it). An index is an Integer, rounded half to even, and Null is error 94. <c>AddItem</c> is error 5
    /// for an index outside 0 to <c>ListCount</c>, and <c>RemoveItem</c> for one outside the items; <c>ItemData</c>, <c>ItemIcon</c> and
    /// <c>Selected</c> are error 381 for an index outside the items, and <c>List</c> is "" there; <c>ListIndex</c> is error 380 for a
    /// value outside -1 to <c>ListCount</c> - 1, and <c>TopIndex</c> for one outside 0 to <c>ListCount</c> - 1 (see
    /// <see cref="RDFormsListBox.TopIndex"/> and <see cref="RDFormsComboBox.TopIndex"/>). <c>ItemIcon</c>, an RD-VBA addition, is an
    /// object (<see cref="FormValue.FromObject"/>): the bytes of the item's picture file, or <c>Nothing</c>. It is assigned one too:
    /// anything else is error 13, and a picture
    /// <see cref="FormPictures.Inspect"/> refuses is error 481. <c>ListIndex</c>, <c>ListCount</c>, <c>NewIndex</c>, <c>SelCount</c> and
    /// <c>TopIndex</c> are Integers. Past 32767, a count or an index is a Long: RDForms sets this rule.
    /// </para>
    /// </remarks>
    public FormValue Invoke(ComponentInstance component, MethodDefinition method, IReadOnlyList<FormValue?> arguments, CodeUnit unit = CodeUnit.Pixel)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(arguments);
        Dispatcher.UIThread.VerifyAccess();
        var control = ReferenceEquals(component, Root) ? View : View.ControlOf(component)
            ?? throw new ArgumentException($"'{component.Name}' is not shown on the form.", nameof(component));
        if (!ReferenceEquals(component.Class.Method(method.Name), method))
            throw new ArgumentException($"{component.Class.TypeName} has no method {method.Name}.", nameof(method));
        var given = MethodArguments.Of(method, arguments);
        if (ReferenceEquals(method, Methods.Move))
            Move(component, control, given, unit);
        else if (ReferenceEquals(method, Methods.SetFocus))
            SetFocus(control);
        else if (ReferenceEquals(method, Methods.ZOrder))
            View.ZOrder(control, toFront: given[0] is not { } position || ToFront(position));
        else if (ReferenceEquals(method, Methods.Refresh))
            Refresh(component, control);
        else if (ReferenceEquals(method, Methods.UpdateRecord) && control is RDFormsData updated)
            updated.UpdateRecord();
        else if (ReferenceEquals(method, Methods.UpdateControls) && control is RDFormsData updating)
            updating.UpdateControls();
        else if (control is IRDFormsList list && ListMember(list, method, given) is { } result)
            return result;
        else
            throw new FormErrorException(FormError.ObjectDoesntSupportPropertyOrMethod);
        return FormValue.Empty;
    }

    /// <summary>
    /// Assigns one of the properties that the window answers for a control of the form, as a program's code assigns it. Call it on the
    /// UI thread.
    /// </summary>
    /// <param name="component">A component the form shows.</param>
    /// <param name="property">
    /// The property: one of <paramref name="component"/>'s class's <see cref="ComponentClass.Methods"/> that
    /// <see cref="MethodDefinition.IsProperty"/>.
    /// </param>
    /// <param name="arguments">The property's arguments, as <see cref="Invoke"/> takes a method's, such as the index of <c>List(Index)</c>.</param>
    /// <param name="value">
    /// The value assigned, of the property's type (<see cref="MethodDefinition.Value"/>): a number for a number, which the window rounds
    /// and checks as it does an argument, and for <c>ItemIcon</c> an object, as <see cref="Invoke"/> gives it.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="component"/>, <paramref name="property"/> or <paramref name="arguments"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="component"/> is not shown on the form, or <paramref name="property"/> is not a property of its class.
    /// </exception>
    /// <exception cref="InvalidOperationException">The call is not on the UI thread.</exception>
    /// <exception cref="FormErrorException">
    /// The assignment is refused with VB's error, and changes nothing: the errors of <see cref="Invoke"/> for the arguments and for the
    /// value, then 383 for a read-only property, and those of the list members (see <see cref="Invoke"/>).
    /// </exception>
    public void Assign(ComponentInstance component, MethodDefinition property, IReadOnlyList<FormValue?> arguments, FormValue value)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(arguments);
        Dispatcher.UIThread.VerifyAccess();
        var control = View.ControlOf(component) ?? throw new ArgumentException($"'{component.Name}' is not shown on the form.", nameof(component));
        if (!property.IsProperty || !ReferenceEquals(component.Class.Method(property.Name), property))
            throw new ArgumentException($"{component.Class.TypeName} has no property {property.Name} that its window answers.", nameof(property));
        var given = MethodArguments.Of(property, arguments);
        if (control is not IRDFormsList list)
            throw new FormErrorException(FormError.ObjectDoesntSupportPropertyOrMethod);
        if (property.IsReadOnly)
        {
            // The value is coerced first: a value of the wrong type is 13, as measured with VB6.
            _ = MethodArguments.Integer(value, ofNull: FormError.InvalidUseOfNull);
            throw new FormErrorException(FormError.PropertyIsReadOnly(property.Name));
        }
        if (ReferenceEquals(property, Methods.List))
            list.SetList(Index(given[0]!.Value), MethodArguments.String(value));
        else if (ReferenceEquals(property, Methods.ItemData))
            list.SetItemData(Index(given[0]!.Value), MethodArguments.Long(value));
        else if (ReferenceEquals(property, Methods.ItemIcon))
            list.SetItemIcon(Index(given[0]!.Value), PictureOf(value));
        else if (ReferenceEquals(property, Methods.Selected) && list is RDFormsListBox selecting)
            selecting.SetSelected(Index(given[0]!.Value), MethodArguments.Boolean(value));
        else if (ReferenceEquals(property, Methods.ListIndex))
            list.ListIndex = MethodArguments.Integer(value, ofNull: FormError.InvalidUseOfNull);
        else if (ReferenceEquals(property, Methods.ListText))
            list.SetText(MethodArguments.String(value));
        else if (ReferenceEquals(property, Methods.TopIndex))
            list.TopIndex = MethodArguments.Integer(value, ofNull: FormError.InvalidUseOfNull);
        else
            throw new FormErrorException(FormError.ObjectDoesntSupportPropertyOrMethod);
    }

    // A list member's call or read, or null for a method that is not one.
    private static FormValue? ListMember(IRDFormsList list, MethodDefinition method, FormValue?[] given)
    {
        if (ReferenceEquals(method, Methods.AddItem))
            list.AddItem(MethodArguments.String(given[0]!.Value), given[1] is { } index ? Index(index) : null);
        else if (ReferenceEquals(method, Methods.RemoveItem))
            list.RemoveItem(Index(given[0]!.Value));
        else if (ReferenceEquals(method, Methods.Clear))
            list.Clear();
        else if (ReferenceEquals(method, Methods.List))
            return FormValue.FromString(list.GetList(Index(given[0]!.Value)));
        else if (ReferenceEquals(method, Methods.ItemData))
            return FormValue.FromLong(list.GetItemData(Index(given[0]!.Value)));
        else if (ReferenceEquals(method, Methods.ItemIcon))
            return FormValue.FromObject(list.GetItemIcon(Index(given[0]!.Value)));
        else if (ReferenceEquals(method, Methods.Selected) && list is RDFormsListBox selected)
            return FormValue.FromBoolean(selected.GetSelected(Index(given[0]!.Value)));
        else if (ReferenceEquals(method, Methods.ListIndex))
            return Integer(list.ListIndex);
        else if (ReferenceEquals(method, Methods.ListCount))
            return Integer(list.ListCount);
        else if (ReferenceEquals(method, Methods.NewIndex))
            return Integer(list.NewIndex);
        else if (ReferenceEquals(method, Methods.SelCount) && list is RDFormsListBox counted)
            return Integer(counted.SelCount);
        else if (ReferenceEquals(method, Methods.ListText))
            return FormValue.FromString(list.Text);
        else if (ReferenceEquals(method, Methods.TopIndex))
            return Integer(list.TopIndex);
        else
            return null;
        return FormValue.Empty;
    }

    // An item's icon, as ItemIcon is assigned it: the bytes of a picture's file, or Nothing for none. Anything else is 13.
    private static byte[]? PictureOf(FormValue value) => value switch
    {
        { Kind: FormValueKind.Object, ClrValue: null } => null,
        { Kind: FormValueKind.Object, ClrValue: byte[] picture } => picture,
        _ => throw new FormErrorException(FormError.TypeMismatch),
    };

    // An item's index, an Integer: Null is 94.
    private static short Index(FormValue index) => MethodArguments.Integer(index, ofNull: FormError.InvalidUseOfNull);

    // A count or an index of a list, an Integer as VB6 types it, or a Long past an Integer's range.
    private static FormValue Integer(int value) =>
        value is >= short.MinValue and <= short.MaxValue ? FormValue.FromInteger((short)value) : FormValue.FromLong(value);

    private void Move(ComponentInstance component, Control control, FormValue?[] given, CodeUnit unit)
    {
        PropertyDefinition<double>[] extents = [P.Left, P.Top, P.Width, P.Height];
        // Every value is checked before any is set.
        var moves = new List<(PropertyDefinition<double> Property, double Pixels)>();
        for (var i = 0; i < given.Length && given[i] is { } value; i++)
        {
            var coded = (double)MethodArguments.Single(value);
            if (i >= 2 && coded < 0)
                throw new FormErrorException(FormError.InvalidProcedureCall);
            var twips = CodeUnits.FromCode(component, extents[i], coded, unit) * ScaleModeExtensions.TwipsPerPixel;
            if (unit == CodeUnit.VB6)
                twips = Math.Round(twips);
            if (twips is < int.MinValue or > int.MaxValue)
                throw new FormErrorException(FormError.Overflow);
            moves.Add((extents[i], twips / ScaleModeExtensions.TwipsPerPixel));
        }
        // The root's two sizes raise one Resize, and so do a PictureBox's.
        var root = ReferenceEquals(component, Root);
        following = root;
        void SetAll()
        {
            foreach (var (property, pixels) in moves)
                component.Set(property, pixels);
        }
        try
        {
            if (control is RDFormsPictureBox box)
                box.Resizing(SetAll);
            else
                SetAll();
        }
        finally
        {
            following = false;
        }
        if (root && moves.Count > 2)
        {
            SizeToContent = SizeToContent.WidthAndHeight;
            RaiseResize();
        }
    }

    // The focus moves after the call, as Windows posts its focus messages; ActiveControl names the control at once.
    private void SetFocus(Control control)
    {
        if (!IsVisible)
            throw new FormErrorException(FormError.InvalidProcedureCall);
        if (ReferenceEquals(control, View))
        {
            if (!ReferenceEquals(Program.Active, this))
                Dispatcher.UIThread.Post(Activate);
            return;
        }
        if (control is not { Focusable: true, IsEffectivelyEnabled: true, IsEffectivelyVisible: true })
            throw new FormErrorException(FormError.InvalidProcedureCall);
        activeControl = control;
        if (!ReferenceEquals(Program.Active, this))
            Dispatcher.UIThread.Post(Activate);
        else if (!ReferenceEquals(control, focused))
            Dispatcher.UIThread.Post(() =>
            {
                setFocusTo = control;
                try
                {
                    control.Focus();
                }
                finally
                {
                    setFocusTo = null;
                }
            });
    }

    private static bool ToFront(FormValue position) =>
        MethodArguments.Integer(position, ofNull: FormError.TypeMismatch) switch
        {
            0 => true,
            1 => false,
            _ => throw new FormErrorException(FormError.InvalidProcedureCall),
        };

    private static void Refresh(ComponentInstance component, Control control)
    {
        control.InvalidateVisual();
        foreach (var visual in control.GetVisualDescendants())
            visual.InvalidateVisual();
        (DrawingSurfaces.Of(component) as RDFormsDrawingLayer)?.PaintNow();
    }

    // It follows its root from its creation until it is closed, so that a closed window holds no handler of its root's.
    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        MenuHosts.Register(this);
        Root.PropertyChanged -= OnRootChanged;
        Root.PropertyChanged += OnRootChanged;
        foreach (var component in Controls)
        {
            component.PropertyChanged -= OnControlChanged;
            component.PropertyChanged += OnControlChanged;
        }
        Apply();
        ShowTheDefault();
        // While it is open, the window lays out the aligned boxes after it raises the form's Resize.
        Aligned.FollowsTheForm = false;
        if (opened)
            return;
        opened = true;
        resizedAt = SizeOfRoot();
        RDFormsControl.Raise(View, Events.Resize);
        // The first Show raises each aligned box's Resize after the form's, the top and bottom ones first: each box whose class has VB's
        // Resize. A placed UserControl's class has only the events its code declares, and the platform raises none of them, Resize included.
        var docked = Aligned.Docked.Where(box => box.Get(P.Visible) && box.Class.Events.Any(e => ReferenceEquals(e, Events.Resize))).ToArray();
        foreach (var box in docked.Where(box => box.Get(P.Align) is Align.Top or Align.Bottom)
                     .Concat(docked.Where(box => box.Get(P.Align) is Align.Left or Align.Right)))
        {
            if (View.ControlOf(box) is { } control)
                RDFormsControl.Raise(control, Events.Resize);
        }
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
        foreach (var component in Controls)
            component.PropertyChanged -= OnControlChanged;
        Aligned.FollowsTheForm = true;
        MenuHosts.Unregister(this);
        if (ReferenceEquals(Program.Active, this))
            Program.Active = null;
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

    // The form's components other than its root.
    private IEnumerable<ComponentInstance> Controls => View.Form.Components.Where(component => !ReferenceEquals(component, Root));

    // A Default, an Enabled or a Visible can move the default look.
    private void OnControlChanged(object? sender, ComponentPropertyChangedEventArgs e)
    {
        if (e.Property.Name is nameof(P.Default) or nameof(P.Enabled) or nameof(P.Visible))
            ShowTheDefault();
    }

    // The button Enter clicks has the default look: the focused CommandButton, or else the form's default button.
    private void ShowTheDefault()
    {
        var wearer = ControlKeys.Owner(FocusManager?.GetFocusedElement()) as RDFormsCommandButton ?? ButtonFor(P.Default);
        foreach (var component in Controls.Where(component => ReferenceEquals(component.Class, ComponentClasses.CommandButton)))
        {
            if (View.ControlOf(component) is RDFormsCommandButton button)
                button.ShowAsDefault(ReferenceEquals(button, wearer));
        }
    }

    private void OnRootChanged(object? sender, ComponentPropertyChangedEventArgs e)
    {
        Apply();
        if (e.Property.Name is nameof(P.Enabled) or nameof(P.Visible))
            ShowTheDefault();
        if (following || e.Property.Name is not (nameof(P.Width) or nameof(P.Height)))
            return;
        // A size set from code is the window's again, after the user resized it.
        SizeToContent = SizeToContent.WidthAndHeight;
        RaiseResize();
    }

    private (double Width, double Height) SizeOfRoot() => (Root.Get(P.Width), Root.Get(P.Height));

    private AlignLayout Aligned => AlignLayout.Of(View.Form);

    // The aligned boxes follow the form's size after its Resize, and raise theirs after it.
    private void RaiseResize()
    {
        if (SizeOfRoot() != resizedAt)
        {
            resizedAt = SizeOfRoot();
            RDFormsControl.Raise(View, Events.Resize);
        }
        Aligned.LayOut();
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
        if (ReferenceEquals(Program.Active, this) || !Root.Get(P.Enabled))
            return;
        var previous = Program.Active;
        Program.Active = this;
        if (previous is not null)
        {
            previous.LoseFocus();
            RDFormsControl.Raise(previous.View, Events.Deactivate);
        }
        RDFormsControl.Raise(View, Events.Activate);
        TakeFocus();
    }

    // The form's ActiveControl takes the focus again, or else the first control of the tab order, or else the root.
    private void TakeFocus()
    {
        var control = activeControl is not null && RDFormsControl.GetInstance(activeControl) is { } instance
                      && ReferenceEquals(View.ControlOf(instance), activeControl) && RDFormsFormView.CanTakeTheFocus(activeControl)
            ? activeControl
            : View.FirstToTakeTheFocus();
        if (control is null)
        {
            rootFocused = true;
            RDFormsControl.Raise(View, Events.GotFocus);
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
            RDFormsControl.Raise(control, Events.LostFocus);
        }
        else if (rootFocused)
        {
            rootFocused = false;
            RDFormsControl.Raise(View, Events.LostFocus);
        }
    }

    // The window system moves the focus as it activates and deactivates windows, the program's or another application's or program's,
    // taking it to no element or to another window's and giving it back; VB raises nothing for that. Between the controls of the active
    // form, the focus raises LostFocus and GotFocus as it moves.
    void IFormFocus.FocusMoved(Control control, bool got, bool toNone, Control? to)
    {
        ShowTheDefault();
        if (got)
        {
            if (keeping is not null)
            {
                if (ReferenceEquals(control, keeping))
                    keeping = null;
                return;
            }
            activeControl = control;
            if (!ReferenceEquals(Program.Active, this) || ReferenceEquals(control, focused))
                return;
            // The root, or a control that lost the window system's focus to none, as it was hidden or disabled.
            LoseFocus();
            focused = control;
            RDFormsControl.Raise(control, Events.GotFocus);
        }
        else if (ReferenceEquals(control, focused) && !toNone && IsActive
                 && !(to is not null && TopLevel.GetTopLevel(to) is RDFormsFormWindow other && !ReferenceEquals(other, this)))
        {
            if (to is not null && !Leaves(control, to))
            {
                keeping = control;
                Dispatcher.UIThread.Post(() =>
                {
                    if (!control.Focus())
                        keeping = null;
                });
                return;
            }
            focused = null;
            RDFormsControl.Raise(control, Events.LostFocus);
        }
    }

    // Whether the focus leaves a control for another of the form: its edit is kept, with BeforeUpdate and AfterUpdate, then it raises
    // Validate where the other control's CausesValidation is True, unless a SetFocus moves the focus. A handler that cancels either
    // keeps the focus on the control.
    private bool Leaves(Control control, Control to)
    {
        if (control is IKeepsEdits edits && !edits.KeepEdit())
            return false;
        if (ReferenceEquals(to, setFocusTo) || RDFormsControl.GetInstance(control) is not { } instance
            || !instance.Class.Events.Contains(Events.Validate) || RDFormsControl.GetInstance(to) is not { } next
            || !next.Class.Has(P.CausesValidation) || !next.Get(P.CausesValidation))
            return true;
        return !ControlEvents.Cancelled(RDFormsControl.Raise(control, Events.Validate, FormValue.FromBoolean(false)));
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
            return !Cancelled(RDFormsControl.Raise(View, Events.QueryUnload, FormValue.FromInteger(0), FormValue.FromInteger((short)mode)))
                   && !Cancelled(RDFormsControl.Raise(View, Events.Unload, FormValue.FromInteger(0)));
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
        var (across, down) = RDFormsDrawingLayer.ScaleOf(Root).ToTwips(x ?? 0, y ?? 0);
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
            if (visual is Control control && RDFormsControl.GetInstance(control) is { } instance && !ReferenceEquals(instance, Root))
                return View.IsVisualAncestorOf(control) ? instance : null;
        }
        return null;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || !KeyIsTheRoots(e.Source) || KeyCodes.Of(e) is not { } code)
            return;
        FormKeys.Raise(View, Events.KeyDown, code, ControlEvents.ShiftOf(e.KeyModifiers));
        // Backspace and Ctrl+A type a character with no text, as in a TextBox, and so do Enter and Esc.
        if (e.Key == Key.Back)
            FormKeys.Raise(View, Events.KeyPress, 8);
        else if (e.Key == Key.A && e.KeyModifiers == KeyModifiers.Control)
            FormKeys.Raise(View, Events.KeyPress, 1);
        else if (ControlKeys.AsciiOf(e) is { } ascii)
            FormKeys.Raise(View, Events.KeyPress, ascii);
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
        if (owner is RDFormsComboBox { IsDropDownOpen: true })
        {
            if (button is not null)
                keyUpTaken = e.Key;
            return;
        }
        if (e.Key == Key.Enter && owner is RDFormsCommandButton focused)
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
    private RDFormsCommandButton? ButtonFor(PropertyDefinition<bool> property) =>
        View.Form.Components.FirstOrDefault(component => ReferenceEquals(component.Class, ComponentClasses.CommandButton) && component.Get(property))
            is { } chosen && View.ControlOf(chosen) is RDFormsCommandButton { IsEffectivelyEnabled: true, IsEffectivelyVisible: true } button
            ? button
            : null;

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (!e.Handled && KeyIsTheRoots(e.Source) && KeyCodes.Of(e) is { } code)
            FormKeys.Raise(View, Events.KeyUp, code, ControlEvents.ShiftOf(e.KeyModifiers));
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        // A character outside ASCII raises no KeyPress, as in a TextBox.
        if (e.Handled || string.IsNullOrEmpty(e.Text) || e.Text.Any(character => character > 127) || !KeyIsTheRoots(e.Source))
            return;
        foreach (var character in e.Text)
            FormKeys.Raise(View, Events.KeyPress, (short)character);
    }

    private void Apply()
    {
        Title = Root.Get(P.Caption);
        CanMaximize = Root.Get(P.MaxButton);
        CanMinimize = Root.Get(P.MinButton);
        ShowInTaskbar = Root.Get(P.ShowInTaskbar);
        ShowActivated = Root.Get(P.Enabled);
        MenuBar.IsEnabled = Root.Get(P.Enabled);
        ApplyIcon();
    }

    // The window shows the root's Icon, made once for each value it takes. With none, or one the window system cannot read, it
    // shows the window system's own.
    private void ApplyIcon()
    {
        var bytes = Root.Class.Has(P.Icon) ? Root.Get(P.Icon) : null;
        if (iconMade && ReferenceEquals(bytes, iconFrom))
            return;
        iconMade = true;
        iconFrom = bytes;
        Icon = IconOf(bytes);
    }

    private static WindowIcon? IconOf(byte[]? bytes)
    {
        if (bytes is null || FormPictures.Inspect(bytes, out _) is not null)
            return null;
        try
        {
            return new WindowIcon(new MemoryStream(bytes, writable: false));
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return null;
        }
    }
}
