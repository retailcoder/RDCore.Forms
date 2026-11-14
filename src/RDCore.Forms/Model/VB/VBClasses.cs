// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using static RDCore.Forms.Model.VB.VBEvents;
using M = RDCore.Forms.Model.VB.VBMethods;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Model.VB;

/// <summary>
/// The component classes of VB6's <c>VB</c> library: its designer roots, its intrinsic controls and its menu.
/// </summary>
/// <remarks>
/// Each class lists the properties of it that the model holds, then the ones every class of its kind has: <c>Left</c> and
/// <c>Top</c> for a component with a place, <c>Width</c> and <c>Height</c> for one with a size, <c>Visible</c> for a control a
/// running program shows, and <c>Tag</c>. A property a designer file sets that a class does not list is kept as an
/// <see cref="UnmodelledProperty"/>. Its methods are among <see cref="VBMethods"/>: a control's are <c>Move</c>, <c>Refresh</c> and
/// <c>ZOrder</c>, and <c>SetFocus</c> where it can take the focus.
/// </remarks>
public static class VBClasses
{
    private static readonly EventDefinition[] KeyAndMouse = [KeyDown, KeyPress, KeyUp, Click, DblClick, MouseDown, MouseMove, MouseUp];

    private static readonly FormColor WindowBackground = FormColor.FromSystem(SystemColor.Window);
    private static readonly FormColor WindowForeground = FormColor.FromSystem(SystemColor.WindowText);

    /// <summary>
    /// <c>VB.Form</c>: a form's designer root.
    /// </summary>
    public static readonly ComponentClass Form = Root("VB.Form", "Form",
        [P.Caption, P.BackColor, P.Enabled, P.ForeColor, P.Icon, P.MousePointer, P.ShowInTaskbar, P.Font, P.StartUpPosition, P.KeyPreview,
         P.MaxButton, P.MinButton, P.Visible, P.AutoRedraw, P.DrawMode, P.DrawStyle, P.DrawWidth, P.FillColor, P.FillStyle, P.ContextMenu],
        [.. KeyAndMouse, Initialize, Load, Resize, Activate, Deactivate, GotFocus, LostFocus, Paint, QueryUnload, Unload, Terminate],
        [M.Move, M.Refresh, M.SetFocus]);

    /// <summary>
    /// <c>VB.MDIForm</c>: an MDI form's designer root. Its background defaults to the application workspace's colour. It has no font
    /// and no scale of its own.
    /// </summary>
    public static readonly ComponentClass MDIForm = new("VB.MDIForm", "MDIForm", ComponentKind.Root,
        [P.Caption, P.BackColor, P.Enabled, P.Icon, P.MousePointer, P.MouseIcon, P.Picture, P.StartUpPosition, P.Appearance, P.RightToLeft,
         P.HelpContextID, P.Visible, P.Left, P.Top, P.Width, P.Height, P.Tag],
        [Click, DblClick, MouseDown, MouseMove, MouseUp, Initialize, Load, Resize, Activate, Deactivate, QueryUnload, Unload, Terminate],
        Defaults((P.BackColor, FormColor.FromSystem(SystemColor.AppWorkspace))), isContainer: true, methods: [M.Move, M.Refresh, M.SetFocus]);

    /// <summary>
    /// <c>VB.UserControl</c>: a UserControl's designer root.
    /// </summary>
    public static readonly ComponentClass UserControl = Root("VB.UserControl", "UserControl",
        [P.BackColor, P.Enabled, P.Font, P.ForeColor, P.KeyPreview],
        [AccessKeyPress, AmbientChanged, AsyncReadComplete, AsyncReadProgress, Click, DblClick, DragDrop, DragOver, EnterFocus, ExitFocus,
         GetDataMember, GotFocus, Hide, HitTest, Initialize, InitProperties, KeyDown, KeyPress, KeyUp, LostFocus, MouseDown, MouseMove,
         MouseUp, OLECompleteDrag, OLEDragDrop, OLEDragOver, OLEGiveFeedback, OLESetData, OLEStartDrag, Paint, ReadProperties, Resize,
         Show, Terminate, WriteProperties],
        [M.Refresh]);

    /// <summary>
    /// <c>VB.PropertyPage</c>: a PropertyPage's designer root.
    /// </summary>
    public static readonly ComponentClass PropertyPage = Root("VB.PropertyPage", "PropertyPage",
        [P.BackColor, P.Caption, P.Font, P.ForeColor, P.HelpContextID, P.KeyPreview],
        [ApplyChanges, Click, DblClick, DragDrop, DragOver, EditProperty, GotFocus, Initialize, KeyDown, KeyPress, KeyUp, LostFocus,
         MouseDown, MouseMove, MouseUp, OLECompleteDrag, OLEDragDrop, OLEDragOver, OLEGiveFeedback, OLESetData, OLEStartDrag, Paint,
         SelectionChanged, Terminate],
        [M.Refresh]);

    /// <summary>
    /// <c>VB.CommandButton</c>.
    /// </summary>
    public static readonly ComponentClass CommandButton = Control("VB.CommandButton", "Command",
        [P.Caption, P.BackColor, P.ForeColor, P.Appearance, P.Font, P.MousePointer, P.Picture, P.Enabled, P.TabStop, P.TabIndex, P.ToolTipText,
         P.Default, P.Cancel, P.ButtonStyle, P.DownPicture, P.DisabledPicture],
        [Click, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove, MouseUp]);

    /// <summary>
    /// <c>VB.TextBox</c>. Its colours default to the window's (<c>Window</c>, <c>WindowText</c>), where most controls default to the
    /// button's (<c>ButtonFace</c>, <c>ButtonText</c>), and it has a single-line border by default.
    /// </summary>
    public static readonly ComponentClass TextBox = Control("VB.TextBox", "Text",
        [P.Text, P.Enabled, P.BackColor, P.ForeColor, P.Font, P.TabStop, P.MousePointer, P.TabIndex, P.ToolTipText, P.Locked, P.Alignment,
         P.Appearance, P.BorderStyle],
        [KeyDown, KeyPress, KeyUp, Change, Click, DblClick, GotFocus, LostFocus, MouseDown, MouseMove, MouseUp],
        Defaults((P.BackColor, WindowBackground), (P.ForeColor, WindowForeground), (P.BorderStyle, BorderStyle.FixedSingle)));

    /// <summary>
    /// <c>VB.Label</c>, windowless. Its background is opaque by default.
    /// </summary>
    public static readonly ComponentClass Label = Control("VB.Label", "Label",
        [P.Caption, P.Enabled, P.Font, P.BackColor, P.BackStyle, P.Alignment, P.Appearance, P.AutoSize, P.BorderStyle, P.ForeColor,
         P.MousePointer, P.RightToLeft, P.ToolTipText, P.UseMnemonic, P.WhatsThisHelpID, P.WordWrap, P.TabIndex],
        [Click, DblClick, MouseDown, MouseMove, MouseUp],
        Defaults((P.BackStyle, BackStyle.Opaque)), isWindowless: true);

    /// <summary>
    /// <c>VB.CheckBox</c>.
    /// </summary>
    public static readonly ComponentClass CheckBox = Control("VB.CheckBox", "Check",
        [P.BackColor, P.Caption, P.Enabled, P.Font, P.ForeColor, P.MousePointer, P.RightToLeft, P.ToolTipText, P.CheckBoxValue, P.Appearance,
         P.TabStop, P.TabIndex, P.ButtonStyle, P.Picture, P.DownPicture, P.DisabledPicture],
        [Click, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove, MouseUp]);

    /// <summary>
    /// <c>VB.OptionButton</c>.
    /// </summary>
    public static readonly ComponentClass OptionButton = Control("VB.OptionButton", "Option",
        [P.BackColor, P.Caption, P.Enabled, P.Font, P.ForeColor, P.MousePointer, P.RightToLeft, P.ToolTipText, P.OptionButtonValue,
         P.Appearance, P.TabStop, P.TabIndex, P.ButtonStyle, P.Picture, P.DownPicture, P.DisabledPicture],
        [Click, DblClick, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove, MouseUp]);

    /// <summary>
    /// <c>VB.Frame</c>, a container.
    /// </summary>
    public static readonly ComponentClass Frame = Control("VB.Frame", "Frame",
        [P.Caption, P.Enabled, P.BackColor, P.ForeColor, P.Font, P.MousePointer, P.TabIndex, P.ToolTipText],
        [Click, DblClick, MouseDown, MouseMove, MouseUp], isContainer: true);

    /// <summary>
    /// <c>VB.ListBox</c>. Its colours default to the window's.
    /// </summary>
    public static readonly ComponentClass ListBox = Control("VB.ListBox", "List",
        [P.Enabled, P.Font, P.ForeColor, P.BackColor, P.List, P.ItemData, P.ItemIcon, P.MultiSelect, P.DragIcon, P.MousePointer, P.RightToLeft,
         P.Appearance, P.TabStop, P.TabIndex, P.ToolTipText],
        [Click, DblClick, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove, MouseUp],
        Defaults((P.BackColor, WindowBackground), (P.ForeColor, WindowForeground)));

    /// <summary>
    /// <c>VB.ComboBox</c>. Its colours default to the window's.
    /// </summary>
    public static readonly ComponentClass ComboBox = Control("VB.ComboBox", "Combo",
        [P.Text, P.Enabled, P.Font, P.ForeColor, P.BackColor, P.List, P.ItemData, P.ItemIcon, P.Locked, P.ComboBoxStyle, P.MousePointer, P.RightToLeft,
         P.Appearance, P.TabStop, P.TabIndex, P.ToolTipText],
        [Change, Click, DblClick, DropDown, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp],
        Defaults((P.BackColor, WindowBackground), (P.ForeColor, WindowForeground)));

    /// <summary>
    /// <c>VB.DriveListBox</c>: the drives, one of which is its <c>Drive</c>. It raises no Click and no mouse events. Its colours
    /// default to the window's.
    /// </summary>
    public static readonly ComponentClass DriveListBox = Control("VB.DriveListBox", "Drive",
        [P.Enabled, P.Font, P.ForeColor, P.BackColor, P.DragIcon, P.MousePointer, P.Appearance, P.TabStop, P.TabIndex, P.ToolTipText],
        [Change, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp],
        Defaults((P.BackColor, WindowBackground), (P.ForeColor, WindowForeground)));

    /// <summary>
    /// <c>VB.DirListBox</c>: the directories of its <c>Path</c>, and the directories above it. It raises no DblClick. Its colours
    /// default to the window's.
    /// </summary>
    public static readonly ComponentClass DirListBox = Control("VB.DirListBox", "Dir",
        [P.Enabled, P.Font, P.ForeColor, P.BackColor, P.DragIcon, P.MousePointer, P.Appearance, P.TabStop, P.TabIndex, P.ToolTipText],
        [Change, Click, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove, MouseUp],
        Defaults((P.BackColor, WindowBackground), (P.ForeColor, WindowForeground)));

    /// <summary>
    /// <c>VB.FileListBox</c>: the files of its <c>Path</c> that its <c>Pattern</c> and its attribute properties let through.
    /// Its colours default to the window's.
    /// </summary>
    public static readonly ComponentClass FileListBox = Control("VB.FileListBox", "File",
        [P.Enabled, P.Font, P.ForeColor, P.BackColor, P.Pattern, P.Archive, P.Hidden, P.Normal, P.ReadOnly, P.SystemFiles, P.MultiSelect,
         P.DragIcon, P.MousePointer, P.Appearance, P.TabStop, P.TabIndex, P.ToolTipText],
        [Click, DblClick, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove, MouseUp, PathChange, PatternChange],
        Defaults((P.BackColor, WindowBackground), (P.ForeColor, WindowForeground)));

    /// <summary>
    /// <c>VB.PictureBox</c>, a container. It has a single-line border by default.
    /// </summary>
    public static readonly ComponentClass PictureBox = Control("VB.PictureBox", "Picture",
        [P.Align, P.Appearance, P.AutoRedraw, P.AutoSize, P.BackColor, P.BorderStyle, P.CausesValidation, P.Enabled, P.FillColor, P.FillStyle,
         P.Font, P.ForeColor, P.MousePointer, P.TabIndex, P.TabStop, P.ToolTipText, P.Picture, P.DrawMode, P.DrawStyle, P.DrawWidth,
         .. ScaleProperties],
        [Click, DblClick, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove, MouseUp, Paint],
        Defaults((P.BorderStyle, BorderStyle.FixedSingle)), isContainer: true);

    /// <summary>
    /// <c>VB.Image</c>, windowless: it shows a picture, and holds no controls.
    /// </summary>
    public static readonly ComponentClass Image = Control("VB.Image", "Image",
        [P.Picture, P.Stretch, P.BorderStyle, P.MousePointer, P.MouseIcon, P.DragIcon, P.Enabled, P.ToolTipText],
        [Click, DblClick, MouseDown, MouseMove, MouseUp], isWindowless: true);

    /// <summary>
    /// <c>VB.Timer</c>. It has a place in the designer and no size, and a running program does not show it.
    /// </summary>
    public static readonly ComponentClass Timer = new("VB.Timer", "Timer", ComponentKind.Control,
        [P.Enabled, P.Interval, P.Left, P.Top, P.Tag], [VBEvents.Timer], hasExtent: false);

    /// <summary>
    /// <c>VB.HScrollBar</c>.
    /// </summary>
    public static readonly ComponentClass HScrollBar = Control("VB.HScrollBar", "HScroll", ScrollBarProperties, ScrollBarEvents);

    /// <summary>
    /// <c>VB.VScrollBar</c>.
    /// </summary>
    public static readonly ComponentClass VScrollBar = Control("VB.VScrollBar", "VScroll", ScrollBarProperties, ScrollBarEvents);

    /// <summary>
    /// <c>VB.Shape</c>, windowless. It raises no events.
    /// </summary>
    public static readonly ComponentClass Shape = Control("VB.Shape", "Shape",
        [P.BackColor, P.BackStyle, P.Shape, P.BorderWidth, P.FillColor, P.BorderColor, P.ShapeBorderStyle, P.FillStyle], [],
        isWindowless: true);

    /// <summary>
    /// <c>VB.Line</c>, windowless. Its geometry is its two ends, in its container's scale: it has no <c>Left</c>, <c>Top</c>,
    /// <c>Width</c> or <c>Height</c>. It raises no events, and its colour defaults to the window's text colour.
    /// </summary>
    public static readonly ComponentClass Line = new("VB.Line", "Line", ComponentKind.Control,
        [P.BorderColor, P.ShapeBorderStyle, P.BorderWidth, P.DrawMode, P.X1, P.X2, P.Y1, P.Y2, P.Visible, P.Tag], [],
        Defaults((P.BorderColor, WindowForeground)), isWindowless: true, methods: [M.Refresh, M.ZOrder]);

    /// <summary>
    /// <c>VB.Menu</c>: a menu item. It has no place or size; its submenu's items are its contents. A form's menus, with VB's rules
    /// for them, are its <see cref="Menus.FormMenu"/>.
    /// </summary>
    public static readonly ComponentClass Menu = new("VB.Menu", "mnu", ComponentKind.Menu,
        [P.Caption, P.Enabled, P.Checked, P.WindowList, P.Visible, P.Shortcut, P.CheckOnClick, P.Picture, P.Tag], [Click], hasExtent: false);

    /// <summary>
    /// Every class of the catalog.
    /// </summary>
    public static IReadOnlyList<ComponentClass> All { get; } =
        [Form, MDIForm, UserControl, PropertyPage, CommandButton, TextBox, Label, CheckBox, OptionButton, Frame, ListBox, ComboBox, PictureBox, Image,
         Timer, HScrollBar, VScrollBar, Shape, Line, Menu, DriveListBox, DirListBox, FileListBox];

    private static readonly Dictionary<string, ComponentClass> byTypeName =
        All.ToDictionary(c => c.TypeName, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The catalog's class of a given type name, such as <c>VB.TextBox</c>, compared without regard to case.
    /// </summary>
    /// <param name="typeName">The class's qualified type name.</param>
    /// <returns>The class, or <see langword="null"/> when the catalog has none of that name.</returns>
    public static ComponentClass? ByTypeName(string typeName) => byTypeName.GetValueOrDefault(typeName);

    private static PropertyDefinition[] ScrollBarProperties =>
        [P.CausesValidation, P.Enabled, P.SmallChange, P.MousePointer, P.LargeChange, P.Min, P.ScrollBarValue, P.Max, P.TabStop, P.TabIndex];

    private static EventDefinition[] ScrollBarEvents => [Change, GotFocus, LostFocus, Scroll];

    private static PropertyDefinition[] ScaleProperties => [P.ScaleMode, P.ScaleLeft, P.ScaleTop, P.ScaleWidth, P.ScaleHeight];

    private static ComponentClass Root(string typeName, string defaultName, PropertyDefinition[] properties, EventDefinition[] events,
        MethodDefinition[] methods) =>
        new(typeName, defaultName, ComponentKind.Root, [.. properties, P.Left, P.Top, P.Width, P.Height, .. ScaleProperties, P.Tag], events,
            isContainer: true, methods: methods);

    // A control that takes the mouse also has the ContextMenu of RD-VBA. One that can take the focus has SetFocus.
    private static ComponentClass Control(string typeName, string defaultName, PropertyDefinition[] properties, EventDefinition[] events,
        IReadOnlyDictionary<PropertyDefinition, object?>? defaults = null, bool isContainer = false, bool isWindowless = false) =>
        new(typeName, defaultName, ComponentKind.Control,
            [.. properties, P.Left, P.Top, P.Width, P.Height, P.Visible, P.Tag, .. events.Contains(MouseUp) ? [P.ContextMenu] : Array.Empty<PropertyDefinition>()],
            events, defaults, isContainer, isWindowless,
            methods: [M.Move, M.Refresh, .. events.Contains(GotFocus) ? [M.SetFocus] : Array.Empty<MethodDefinition>(), M.ZOrder]);

    private static Dictionary<PropertyDefinition, object?> Defaults(params (PropertyDefinition Property, object? Value)[] defaults) =>
        defaults.ToDictionary(d => d.Property, d => d.Value);
}
