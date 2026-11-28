// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using static RDCore.Forms.Model.Events;
using M = RDCore.Forms.Model.Methods;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Model;

/// <summary>
/// The component classes of the <c>RDForms</c> library that VB6's <c>VB</c> library and MSForms port to: the designer roots, the
/// intrinsic controls and the menu. A VB6 designer file's <c>VB.TextBox</c> is an <c>RDForms.TextBox</c>, and an MSForms
/// <c>ToggleButton</c> is an <c>RDForms.ToggleButton</c>.
/// </summary>
/// <remarks>
/// Each class lists the properties of it that the model holds, then the ones every class of its kind has: <c>Left</c> and
/// <c>Top</c> for a component with a place, <c>Width</c> and <c>Height</c> for one with a size, <c>Visible</c> and RD-VBA's
/// <see cref="Properties.Opacity"/> for a control a running program shows, and <c>Tag</c>. A property a designer file sets that a class does
/// not list is kept as an <see cref="UnmodelledProperty"/>. Its methods are among <see cref="Methods"/>: a control's are <c>Move</c>, <c>Refresh</c> and
/// <c>ZOrder</c>, and <c>SetFocus</c> where it can take the focus.
/// </remarks>
public static class ComponentClasses
{
    private static readonly EventDefinition[] KeyAndMouse = [KeyDown, KeyPress, KeyUp, Click, DblClick, MouseDown, MouseMove, MouseUp];

    private static readonly FormColor WindowBackground = FormColor.FromSystem(SystemColor.Window);
    private static readonly FormColor WindowForeground = FormColor.FromSystem(SystemColor.WindowText);

    // The member each class's default member stands for, as measured with VB6: what `x = Text1` reads and `Text1 = x` assigns. A
    // form's takes an argument, as in `Form1("Command1")`. A UserControl's and a PropertyPage's were not measured.
    private static readonly Dictionary<string, string> DefaultMembers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RDForms.Form"] = "Controls",
        ["RDForms.MDIForm"] = "Controls",
        ["RDForms.CommandButton"] = "Value",
        ["RDForms.CheckBox"] = "Value",
        ["RDForms.OptionButton"] = "Value",
        ["RDForms.HScrollBar"] = "Value",
        ["RDForms.VScrollBar"] = "Value",
        ["RDForms.TextBox"] = "Text",
        ["RDForms.ComboBox"] = "Text",
        ["RDForms.ListBox"] = "Text",
        ["RDForms.Label"] = "Caption",
        ["RDForms.Frame"] = "Caption",
        ["RDForms.PictureBox"] = "Picture",
        ["RDForms.Image"] = "Picture",
        ["RDForms.DriveListBox"] = "Drive",
        ["RDForms.DirListBox"] = "Path",
        ["RDForms.FileListBox"] = "FileName",
        ["RDForms.Shape"] = "Shape",
        ["RDForms.Line"] = "Visible",
        ["RDForms.Timer"] = "Enabled",
        ["RDForms.Data"] = "Caption",
        ["RDForms.Menu"] = "Enabled",
        ["RDForms.ToggleButton"] = "Value",
        ["RDForms.SpinButton"] = "Value",
        ["RDForms.MultiPage"] = "Value",
        ["RDForms.TabStrip"] = "Value",
    };

    /// <summary>
    /// <c>RDForms.Form</c>: a form's designer root.
    /// </summary>
    public static readonly ComponentClass Form = Root("RDForms.Form", "Form",
        [P.Caption, P.BackColor, P.Enabled, P.ForeColor, P.Icon, P.Picture, P.MousePointer, P.MouseIcon, P.ShowInTaskbar, P.Font, P.FontTransparent,
         P.StartUpPosition, P.KeyPreview, P.MaxButton, P.MinButton, P.Visible, P.AutoRedraw, P.DrawMode, P.DrawStyle, P.DrawWidth, P.FillColor,
         P.FillStyle, P.ContextMenu, .. ScrollingProperties],
        [.. KeyAndMouse, Initialize, Load, Resize, Activate, Deactivate, GotFocus, LostFocus, Paint, QueryUnload, Unload, Terminate, ContainerScroll,
         ContainerBeforeDragOver, ContainerBeforeDropOrPaste, Error],
        [M.Move, M.Refresh, M.SetFocus]);

    /// <summary>
    /// <c>RDForms.MDIForm</c>: an MDI form's designer root. Its background defaults to the application workspace's colour. It has no font,
    /// no scale of its own and, as VB6's has none, no <c>Refresh</c>.
    /// </summary>
    public static readonly ComponentClass MDIForm = new("RDForms.MDIForm", "MDIForm", ComponentKind.Root,
        [P.Caption, P.BackColor, P.Enabled, P.Icon, P.MousePointer, P.MouseIcon, P.Picture, P.StartUpPosition, P.Appearance, P.RightToLeft,
         P.HelpContextID, P.Visible, P.Left, P.Top, P.Width, P.Height, P.Tag],
        [Click, DblClick, MouseDown, MouseMove, MouseUp, Initialize, Load, Resize, Activate, Deactivate, QueryUnload, Unload, Terminate],
        Defaults((P.BackColor, FormColor.FromSystem(SystemColor.AppWorkspace))), isContainer: true, methods: [M.Move, M.SetFocus],
        defaultMember: DefaultMembers["RDForms.MDIForm"]);

    /// <summary>
    /// <c>RDForms.UserControl</c>: a UserControl's designer root, with the properties VB6's Properties window lists for it and their
    /// defaults. It is opaque, its <c>MaskColor</c> is the button face's colour, and its palette is its container's. A placed
    /// UserControl is an instance of the class <see cref="UserControlClasses.Of"/> makes from its designer file.
    /// </summary>
    public static readonly ComponentClass UserControl = Root("RDForms.UserControl", "UserControl",
        [P.AccessKeys, P.Alignable, P.Appearance, P.AutoRedraw, P.BackColor, P.BackStyle, P.BorderStyle, P.CanGetFocus, P.ClipBehavior,
         P.ClipControls, P.ControlContainer, P.DataBindingBehavior, P.DataSourceBehavior, P.DefaultCancel, P.DrawMode, P.DrawStyle,
         P.DrawWidth, P.EditAtDesignTime, P.Enabled, P.FillColor, P.FillStyle, P.Font, P.FontTransparent, P.ForeColor, P.ForwardFocus, P.HasDC,
         P.HitBehavior,
         P.InvisibleAtRuntime, P.KeyPreview, P.MaskColor, P.MaskPicture, P.MouseIcon, P.MousePointer, P.OLEDropMode, P.Palette,
         P.PaletteMode, P.Picture, P.Public, P.RightToLeft, P.ToolboxBitmap, P.Windowless],
        [AccessKeyPress, AmbientChanged, AsyncReadComplete, AsyncReadProgress, Click, DblClick, DragDrop, DragOver, EnterFocus, ExitFocus,
         GetDataMember, GotFocus, Hide, HitTest, Initialize, InitProperties, KeyDown, KeyPress, KeyUp, LostFocus, MouseDown, MouseMove,
         MouseUp, OLECompleteDrag, OLEDragDrop, OLEDragOver, OLEGiveFeedback, OLESetData, OLEStartDrag, Paint, ReadProperties, Resize,
         Show, Terminate, WriteProperties],
        [M.Refresh],
        Defaults((P.BackStyle, BackStyle.Opaque), (P.MaskColor, FormColor.FromSystem(SystemColor.ButtonFace)),
            (P.PaletteMode, PaletteMode.Container)));

    /// <summary>
    /// <c>RDForms.PropertyPage</c>: a PropertyPage's designer root.
    /// </summary>
    public static readonly ComponentClass PropertyPage = Root("RDForms.PropertyPage", "PropertyPage",
        [P.BackColor, P.Caption, P.Font, P.ForeColor, P.HelpContextID, P.KeyPreview],
        [ApplyChanges, Click, DblClick, DragDrop, DragOver, EditProperty, GotFocus, Initialize, KeyDown, KeyPress, KeyUp, LostFocus,
         MouseDown, MouseMove, MouseUp, OLECompleteDrag, OLEDragDrop, OLEDragOver, OLEGiveFeedback, OLESetData, OLEStartDrag, Paint,
         SelectionChanged, Terminate],
        [M.Refresh]);

    /// <summary>
    /// <c>RDForms.CommandButton</c>. It has no <c>ForeColor</c>, as VB6's has none: its caption is drawn in the system's <c>ButtonText</c>
    /// colour. Its <c>PicturePosition</c> is an RD-VBA addition: a Graphical button where it is set lays its picture out as MSForms'
    /// CommandButton does, and one where it is not keeps VB6's layout.
    /// </summary>
    public static readonly ComponentClass CommandButton = Control("RDForms.CommandButton", "Command",
        [P.Caption, P.BackColor, P.Appearance, P.Font, P.MousePointer, P.MouseIcon, P.Picture, P.Enabled, P.TabStop, P.TabIndex, P.ToolTipText,
         P.Default, P.Cancel, P.ButtonStyle, P.DownPicture, P.DisabledPicture, P.CommandButtonValue, P.MaskColor, P.UseMaskColor,
         P.CausesValidation, P.PicturePosition],
        [Click, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove, MouseUp, Validate, BeforeDragOver, BeforeDropOrPaste,
         Error]);

    /// <summary>
    /// <c>RDForms.TextBox</c>. Its colours default to the window's (<c>Window</c>, <c>WindowText</c>), where most controls default to the
    /// button's (<c>ButtonFace</c>, <c>ButtonText</c>), and it has a single-line border by default.
    /// </summary>
    public static readonly ComponentClass TextBox = Control("RDForms.TextBox", "Text",
        [P.Text, P.Enabled, P.BackColor, P.ForeColor, P.Font, P.TabStop, P.MousePointer, P.MouseIcon, P.TabIndex, P.ToolTipText, P.Locked,
         P.Alignment,
         P.Appearance, P.BorderStyle, P.MultiLine, P.ScrollBars, P.PasswordChar, P.MaxLength, P.SelStart, P.SelLength, P.SelText,
         P.CausesValidation, P.ControlSource],
        [KeyDown, KeyPress, KeyUp, Change, Click, DblClick, GotFocus, LostFocus, MouseDown, MouseMove, MouseUp, Validate, BeforeUpdate,
         AfterUpdate, BeforeDragOver, BeforeDropOrPaste, Error],
        Defaults((P.BackColor, WindowBackground), (P.ForeColor, WindowForeground), (P.BorderStyle, BorderStyle.FixedSingle)),
        writableOnAnRDFormsForm: [P.MultiLine, P.ScrollBars]);

    /// <summary>
    /// <c>RDForms.Label</c>, windowless. Its background is opaque by default. Its <c>Picture</c> and <c>PicturePosition</c> are an RD-VBA
    /// addition, laid out as MSForms' Label lays them out.
    /// </summary>
    public static readonly ComponentClass Label = Control("RDForms.Label", "Label",
        [P.Caption, P.Enabled, P.Font, P.BackColor, P.BackStyle, P.Alignment, P.Appearance, P.AutoSize, P.BorderStyle, P.ForeColor,
         P.MousePointer, P.MouseIcon, P.RightToLeft, P.ToolTipText, P.UseMnemonic, P.WhatsThisHelpID, P.WordWrap, P.TabIndex, P.Picture,
         P.PicturePosition],
        [Click, DblClick, MouseDown, MouseMove, MouseUp, BeforeDragOver, BeforeDropOrPaste, Error],
        Defaults((P.BackStyle, BackStyle.Opaque)), isWindowless: true);

    /// <summary>
    /// <c>RDForms.CheckBox</c>.
    /// </summary>
    public static readonly ComponentClass CheckBox = Control("RDForms.CheckBox", "Check",
        [P.BackColor, P.Caption, P.Enabled, P.Font, P.ForeColor, P.MousePointer, P.MouseIcon, P.RightToLeft, P.ToolTipText, P.CheckBoxValue,
         P.Appearance, P.TabStop, P.TabIndex, P.ButtonStyle, P.Picture, P.DownPicture, P.DisabledPicture, P.MaskColor, P.UseMaskColor,
         P.CausesValidation, P.ControlSource],
        [Click, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove, MouseUp, Validate, BeforeUpdate, AfterUpdate,
         BeforeDragOver, BeforeDropOrPaste, Error]);

    /// <summary>
    /// <c>RDForms.OptionButton</c>.
    /// </summary>
    public static readonly ComponentClass OptionButton = Control("RDForms.OptionButton", "Option",
        [P.BackColor, P.Caption, P.Enabled, P.Font, P.ForeColor, P.MousePointer, P.MouseIcon, P.RightToLeft, P.ToolTipText, P.OptionButtonValue,
         P.Appearance, P.TabStop, P.TabIndex, P.ButtonStyle, P.Picture, P.DownPicture, P.DisabledPicture, P.MaskColor, P.UseMaskColor,
         P.CausesValidation, P.ControlSource],
        [Click, DblClick, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove, MouseUp, Validate, BeforeUpdate,
         AfterUpdate, BeforeDragOver, BeforeDropOrPaste, Error]);

    /// <summary>
    /// <c>RDForms.Frame</c>, a container.
    /// </summary>
    public static readonly ComponentClass Frame = Control("RDForms.Frame", "Frame",
        [P.Caption, P.Enabled, P.BackColor, P.ForeColor, P.Font, P.MousePointer, P.MouseIcon, P.TabIndex, P.ToolTipText, .. ScrollingProperties],
        [Click, DblClick, MouseDown, MouseMove, MouseUp, ContainerScroll, ContainerBeforeDragOver, ContainerBeforeDropOrPaste, Error], isContainer: true);

    /// <summary>
    /// <c>RDForms.ListBox</c>. Its colors default to the window's. A program reaches its items through its list members, which its window
    /// answers: <c>AddItem</c>, <c>RemoveItem</c>, <c>Clear</c>, <c>List</c>, <c>ItemData</c>, <c>ItemIcon</c>, <c>Selected</c>,
    /// <c>ListIndex</c>, <c>ListCount</c>, <c>NewIndex</c>, <c>SelCount</c>, <c>Text</c> and <c>TopIndex</c>.
    /// </summary>
    public static readonly ComponentClass ListBox = Control("RDForms.ListBox", "List",
        [P.Enabled, P.Font, P.ForeColor, P.BackColor, P.List, P.ItemData, P.ItemIcon, P.MultiSelect, P.IntegralHeight, P.Sorted, P.DragIcon,
         P.MousePointer, P.MouseIcon, P.RightToLeft, P.Appearance, P.TabStop, P.TabIndex, P.ToolTipText, P.CausesValidation, P.ControlSource,
         P.RowSource],
        [Click, DblClick, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove, MouseUp, Scroll, Validate, BeforeUpdate,
         AfterUpdate, BeforeDragOver, BeforeDropOrPaste, Error],
        Defaults((P.BackColor, WindowBackground), (P.ForeColor, WindowForeground)),
        methods: [M.AddItem, M.RemoveItem, M.Clear, M.List, M.ItemData, M.ItemIcon, M.Selected, M.ListIndex, M.ListCount, M.NewIndex, M.SelCount,
            M.ListText, M.TopIndex],
        writableOnAnRDFormsForm: [P.MultiSelect, P.IntegralHeight]);

    /// <summary>
    /// <c>RDForms.ComboBox</c>. Its colors default to the window's. A program reaches its items through the list members of a ListBox
    /// (<see cref="ListBox"/>), except <c>Selected</c> and <c>SelCount</c>.
    /// </summary>
    public static readonly ComponentClass ComboBox = Control("RDForms.ComboBox", "Combo",
        [P.Text, P.Enabled, P.Font, P.ForeColor, P.BackColor, P.List, P.ItemData, P.ItemIcon, P.Locked, P.ComboBoxStyle, P.IntegralHeight,
         P.Sorted, P.MousePointer, P.MouseIcon, P.RightToLeft, P.Appearance, P.TabStop, P.TabIndex, P.ToolTipText, P.SelStart, P.SelLength, P.SelText,
         P.CausesValidation, P.ControlSource, P.RowSource],
        [Change, Click, DblClick, DropDown, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, Scroll, Validate, BeforeUpdate, AfterUpdate,
         BeforeDragOver, BeforeDropOrPaste, Error],
        Defaults((P.BackColor, WindowBackground), (P.ForeColor, WindowForeground)),
        methods: [M.AddItem, M.RemoveItem, M.Clear, M.List, M.ItemData, M.ItemIcon, M.ListIndex, M.ListCount, M.NewIndex, M.ListText,
            M.TopIndex],
        writableOnAnRDFormsForm: [P.ComboBoxStyle]);

    /// <summary>
    /// <c>RDForms.DriveListBox</c>: the drives, one of which is its <c>Drive</c>. It raises no Click and no mouse events. Its colours
    /// default to the window's.
    /// </summary>
    public static readonly ComponentClass DriveListBox = Control("RDForms.DriveListBox", "Drive",
        [P.Enabled, P.Font, P.ForeColor, P.BackColor, P.DragIcon, P.MousePointer, P.MouseIcon, P.Appearance, P.TabStop, P.TabIndex, P.ToolTipText,
         P.CausesValidation],
        [Change, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, Validate],
        Defaults((P.BackColor, WindowBackground), (P.ForeColor, WindowForeground)));

    /// <summary>
    /// <c>RDForms.DirListBox</c>: the directories of its <c>Path</c>, and the directories above it. It raises no DblClick. Its colours
    /// default to the window's.
    /// </summary>
    public static readonly ComponentClass DirListBox = Control("RDForms.DirListBox", "Dir",
        [P.Enabled, P.Font, P.ForeColor, P.BackColor, P.DragIcon, P.MousePointer, P.MouseIcon, P.Appearance, P.TabStop, P.TabIndex, P.ToolTipText,
         P.CausesValidation],
        [Change, Click, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove, MouseUp, Validate],
        Defaults((P.BackColor, WindowBackground), (P.ForeColor, WindowForeground)));

    /// <summary>
    /// <c>RDForms.FileListBox</c>: the files of its <c>Path</c> that its <c>Pattern</c> and its attribute properties let through.
    /// Its colours default to the window's.
    /// </summary>
    public static readonly ComponentClass FileListBox = Control("RDForms.FileListBox", "File",
        [P.Enabled, P.Font, P.ForeColor, P.BackColor, P.Pattern, P.Archive, P.Hidden, P.Normal, P.ReadOnly, P.SystemFiles, P.MultiSelect,
         P.DragIcon, P.MousePointer, P.MouseIcon, P.Appearance, P.TabStop, P.TabIndex, P.ToolTipText, P.CausesValidation],
        [Click, DblClick, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove, MouseUp, PathChange, PatternChange,
         Validate],
        Defaults((P.BackColor, WindowBackground), (P.ForeColor, WindowForeground)));

    /// <summary>
    /// <c>RDForms.PictureBox</c>, a container. It has a single-line border by default.
    /// </summary>
    public static readonly ComponentClass PictureBox = Control("RDForms.PictureBox", "Picture",
        [P.Align, P.Appearance, P.AutoRedraw, P.AutoSize, P.BackColor, P.BorderStyle, P.CausesValidation, P.Enabled, P.FillColor, P.FillStyle,
         P.Font, P.FontTransparent, P.ForeColor, P.MousePointer, P.MouseIcon, P.TabIndex, P.TabStop, P.ToolTipText, P.Picture, P.DrawMode,
         P.DrawStyle, P.DrawWidth,
         .. ScaleProperties],
        [Click, DblClick, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove, MouseUp, Paint, Resize, Validate],
        Defaults((P.BorderStyle, BorderStyle.FixedSingle)), isContainer: true);

    /// <summary>
    /// <c>RDForms.Image</c>, windowless: it shows a picture, and holds no controls.
    /// </summary>
    public static readonly ComponentClass Image = Control("RDForms.Image", "Image",
        [P.Picture, P.Stretch, P.BorderStyle, P.MousePointer, P.MouseIcon, P.DragIcon, P.Enabled, P.ToolTipText],
        [Click, DblClick, MouseDown, MouseMove, MouseUp, BeforeDragOver, BeforeDropOrPaste, Error], isWindowless: true);

    /// <summary>
    /// <c>RDForms.Timer</c>. It has a place in the designer and no size, and a running program does not show it and can neither read
    /// nor set its <c>Left</c> and <c>Top</c>. It has no <c>Container</c>.
    /// </summary>
    public static readonly ComponentClass Timer = new("RDForms.Timer", "Timer", ComponentKind.Control,
        [P.Enabled, P.Interval, P.Left, P.Top, P.Tag], [Events.Timer], hasExtent: false, notAvailableAtRunTime: [P.Left, P.Top],
        defaultMember: DefaultMembers["RDForms.Timer"], hasContainer: false);

    /// <summary>
    /// <c>RDForms.ImageList</c>, MSComctlLib's: the pictures other controls show, in <c>ListImages</c>. It has a place in the designer
    /// and no size, and a running program does not show it and can neither read nor set its <c>Left</c> and <c>Top</c>. Its
    /// <c>BackColor</c> is the window colour, and it leaves its silver <c>MaskColor</c> out of its pictures by default. It has no
    /// default member.
    /// </summary>
    public static readonly ComponentClass ImageList = new("RDForms.ImageList", "ImageList", ComponentKind.Control,
        [P.BackColor, P.ImageWidth, P.ImageHeight, P.MaskColor, P.UseMaskColor, P.ListImages, P.Left, P.Top, P.Tag], [],
        Defaults((P.BackColor, WindowBackground), (P.UseMaskColor, true)), hasExtent: false,
        notAvailableAtRunTime: [P.Left, P.Top]);

    /// <summary>
    /// <c>RDForms.HScrollBar</c>.
    /// </summary>
    public static readonly ComponentClass HScrollBar = Control("RDForms.HScrollBar", "HScroll", ScrollBarProperties, ScrollBarEvents);

    /// <summary>
    /// <c>RDForms.VScrollBar</c>.
    /// </summary>
    public static readonly ComponentClass VScrollBar = Control("RDForms.VScrollBar", "VScroll", ScrollBarProperties, ScrollBarEvents);

    /// <summary>
    /// <c>RDForms.Shape</c>, windowless. It raises no events.
    /// </summary>
    public static readonly ComponentClass Shape = Control("RDForms.Shape", "Shape",
        [P.BackColor, P.BackStyle, P.Shape, P.BorderWidth, P.FillColor, P.BorderColor, P.ShapeBorderStyle, P.FillStyle], [],
        isWindowless: true);

    /// <summary>
    /// <c>RDForms.Line</c>, windowless. Its geometry is its two ends, in its container's scale: it has no <c>Left</c>, <c>Top</c>,
    /// <c>Width</c> or <c>Height</c>. It raises no events, and its colour defaults to the window's text colour.
    /// </summary>
    public static readonly ComponentClass Line = new("RDForms.Line", "Line", ComponentKind.Control,
        [P.BorderColor, P.ShapeBorderStyle, P.BorderWidth, P.DrawMode, P.X1, P.X2, P.Y1, P.Y2, P.Visible, P.Opacity, P.Tag], [],
        Defaults((P.BorderColor, WindowForeground)), isWindowless: true, methods: [M.Refresh, M.ZOrder],
        defaultMember: DefaultMembers["RDForms.Line"]);

    /// <summary>
    /// <c>RDForms.Data</c>: the four buttons that move through a database's records, and its caption between them. Its caption's colours
    /// default to the window's. It takes no focus and has no <c>TabIndex</c>. Its <c>UpdateRecord</c> and <c>UpdateControls</c> act
    /// on its recordset.
    /// </summary>
    public static readonly ComponentClass Data = Control("RDForms.Data", "Data",
        [P.Caption, P.Align, P.Appearance, P.BackColor, P.ForeColor, P.Font, P.Enabled, P.MousePointer, P.MouseIcon, P.DragIcon, P.RightToLeft,
         P.ToolTipText, P.WhatsThisHelpID, P.Connect, P.DatabaseName, P.RecordSource, P.RecordsetType, P.DefaultType, P.DefaultCursorType,
         P.BOFAction, P.EOFAction, P.Exclusive, P.ReadOnly, P.Options, P.Negotiate],
        [DataValidate, DataError, Reposition, Resize, DragDrop, DragOver, MouseDown, MouseMove, MouseUp, OLECompleteDrag, OLEDragDrop,
         OLEDragOver,
         OLEGiveFeedback, OLESetData, OLEStartDrag],
        Defaults((P.BackColor, WindowBackground), (P.ForeColor, WindowForeground), (P.ReadOnly, false)),
        methods: [M.UpdateRecord, M.UpdateControls]);

    /// <summary>
    /// <c>RDForms.Menu</c>: a menu item. It has no place or size; its submenu's items are its contents. A form's menus, with VB's rules
    /// for them, are its <see cref="Menus.FormMenu"/>.
    /// </summary>
    public static readonly ComponentClass Menu = new("RDForms.Menu", "mnu", ComponentKind.Menu,
        [P.Caption, P.Enabled, P.Checked, P.WindowList, P.Visible, P.Shortcut, P.HelpContextID, P.NegotiatePosition, P.CheckOnClick, P.Picture,
         P.Tag], [Click], hasExtent: false,
        defaultMember: DefaultMembers["RDForms.Menu"]);

    /// <summary>
    /// <c>RDForms.ToggleButton</c>: a push button that stays down once clicked, and comes up at the next click. Its <c>Value</c> is
    /// True while it is down, False while it is up, and Null where it is neither. Its caption wraps by default.
    /// </summary>
    public static readonly ComponentClass ToggleButton = Control("RDForms.ToggleButton", "ToggleButton",
        [P.BackColor, P.Caption, P.Enabled, P.Font, P.ForeColor, P.MousePointer, P.MouseIcon, P.Picture, P.PicturePosition, P.ToggleButtonValue,
         P.TripleState, P.Locked, P.AutoSize, P.BackStyle, P.WordWrap, P.ToolTipText, P.TabStop, P.TabIndex, P.CausesValidation,
         P.ControlSource],
        [Click, DblClick, Change, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove, MouseUp, Validate, BeforeUpdate,
         AfterUpdate, BeforeDragOver, BeforeDropOrPaste, Error],
        Defaults((P.BackStyle, BackStyle.Opaque), (P.WordWrap, true)));

    /// <summary>
    /// <c>RDForms.SpinButton</c>: two arrows that move a value up and down, one above the other or side by side by its
    /// <c>Orientation</c>.
    /// </summary>
    public static readonly ComponentClass SpinButton = Control("RDForms.SpinButton", "SpinButton",
        [P.BackColor, P.ForeColor, P.Enabled, P.MousePointer, P.MouseIcon, P.SpinButtonValue, P.SpinButtonMin, P.SpinButtonMax,
         P.SpinButtonSmallChange, P.Orientation, P.Delay, P.ToolTipText, P.TabStop, P.TabIndex, P.CausesValidation, P.ControlSource],
        [Change, SpinUp, SpinDown, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, Validate, BeforeUpdate, AfterUpdate, BeforeDragOver,
         BeforeDropOrPaste, Error]);

    /// <summary>
    /// <c>RDForms.MultiPage</c>: pages, one shown at a time, chosen by their tabs. It holds only <see cref="Page"/>s, and a page holds the
    /// controls shown while it is selected. Its <c>Value</c> is the selected page's index, and a program reaches its pages through
    /// <see cref="Pages"/>. Its events name the page they are about first.
    /// </summary>
    public static readonly ComponentClass MultiPage = Control("RDForms.MultiPage", "MultiPage",
        [P.BackColor, P.ForeColor, P.Font, P.MultiRow, P.TabStyle, P.TabOrientation, P.Enabled, P.SelectedIndex, P.TabFixedWidth, P.TabFixedHeight,
         P.TabStop, P.TabIndex, P.ToolTipText, P.CausesValidation],
        [Change, TabClick, TabDblClick, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, TabMouseDown, TabMouseMove, TabMouseUp, PageLayout, PageAddControl,
         PageRemoveControl, Validate, BeforeUpdate, AfterUpdate, PageBeforeDragOver, PageBeforeDropOrPaste, PageError],
        isContainer: true);

    /// <summary>
    /// <c>RDForms.Page</c>: a page of a <see cref="MultiPage"/>, and nothing else holds one. It fills its MultiPage's client area, so it has
    /// no place or size of its own, and it holds the controls shown while it is selected. Its MultiPage raises the events about it.
    /// </summary>
    public static readonly ComponentClass Page = new("RDForms.Page", "Page", ComponentKind.Control,
        [P.Caption, P.ToolTipText, P.Picture, P.PicturePosition, P.Enabled, P.Visible, P.Opacity, P.Tag], [], isContainer: true, hasExtent: false);

    /// <summary>
    /// <c>RDForms.TabStrip</c>: a row of tabs, one selected, which holds no controls: a program shows what the selected tab stands for.
    /// Its tabs are its <c>Tabs</c>, which a program reaches through <see cref="Model.Tabs"/>, and its <c>Value</c> is the selected tab's
    /// index. Its events name the tab they are about first.
    /// </summary>
    public static readonly ComponentClass TabStrip = Control("RDForms.TabStrip", "TabStrip",
        [P.BackColor, P.ForeColor, P.Font, P.Enabled, P.MouseIcon, P.MousePointer, P.MultiRow, P.TabStyle, P.TabOrientation, P.Tabs, P.SelectedIndex,
         P.TabFixedWidth, P.TabFixedHeight, P.TabStop, P.TabIndex, P.ToolTipText, P.CausesValidation],
        [Change, TabClick, TabDblClick, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, TabMouseDown, TabMouseMove, TabMouseUp, Validate, BeforeUpdate,
         AfterUpdate, TabBeforeDragOver, TabBeforeDropOrPaste, Error]);

    /// <summary>
    /// Every class of the catalog.
    /// </summary>
    public static IReadOnlyList<ComponentClass> All { get; } =
        [Form, MDIForm, UserControl, PropertyPage, CommandButton, TextBox, Label, CheckBox, OptionButton, Frame, ListBox, ComboBox, PictureBox, Image,
         Timer, HScrollBar, VScrollBar, Shape, Line, Menu, DriveListBox, DirListBox, FileListBox, Data, ToggleButton, SpinButton, MultiPage, Page, TabStrip,
         ImageList];

    private static readonly Dictionary<string, ComponentClass> byTypeName =
        All.ToDictionary(c => c.TypeName, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The catalog's class of a given type name, such as <c>RDForms.TextBox</c>, compared without regard to case.
    /// </summary>
    /// <param name="typeName">The class's qualified type name.</param>
    /// <returns>The class, or <see langword="null"/> when the catalog has none of that name.</returns>
    public static ComponentClass? ByTypeName(string typeName) => byTypeName.GetValueOrDefault(typeName);

    /// <summary>
    /// Whether a container of class <paramref name="container"/> holds a control of class <paramref name="control"/>, as far as their
    /// classes go: a <see cref="Page"/> goes in a <see cref="MultiPage"/> only, and a MultiPage holds pages only. A class that holds no
    /// controls (<see cref="ComponentClass.IsContainer"/>) holds none of any class.
    /// </summary>
    /// <param name="container">The container's class.</param>
    /// <param name="control">The control's class.</param>
    /// <returns><see langword="true"/> when the container's class holds the control's.</returns>
    public static bool CanHold(ComponentClass container, ComponentClass control) =>
        container.IsContainer && ReferenceEquals(control, Page) == ReferenceEquals(container, MultiPage);

    private static PropertyDefinition[] ScrollBarProperties =>
        [P.CausesValidation, P.Enabled, P.SmallChange, P.MousePointer, P.MouseIcon, P.LargeChange, P.Min, P.ScrollBarValue, P.Max, P.TabStop,
         P.TabIndex, P.ControlSource];

    private static EventDefinition[] ScrollBarEvents => [Change, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus, Scroll, Validate,
    BeforeUpdate, AfterUpdate, BeforeDragOver, BeforeDropOrPaste, Error];

    // A form's and a Frame's scroll bars and the area they scroll.
    private static PropertyDefinition[] ScrollingProperties =>
        [P.ContainerScrollBars, P.KeepScrollBarsVisible, P.ScrollHeight, P.ScrollWidth, P.ScrollLeft, P.ScrollTop];

    private static PropertyDefinition[] ScaleProperties => [P.ScaleMode, P.ScaleLeft, P.ScaleTop, P.ScaleWidth, P.ScaleHeight];

    private static ComponentClass Root(string typeName, string defaultName, PropertyDefinition[] properties, EventDefinition[] events,
        MethodDefinition[] methods, IReadOnlyDictionary<PropertyDefinition, object?>? defaults = null) =>
        new(typeName, defaultName, ComponentKind.Root, [.. properties, P.Left, P.Top, P.Width, P.Height, .. ScaleProperties, P.Tag], events,
            defaults, isContainer: true, methods: methods, defaultMember: DefaultMembers.GetValueOrDefault(typeName));

    // A control that takes the mouse also has the ContextMenu of RD-VBA. One that can take the focus has SetFocus. A class's own
    // methods follow the ones every control has.
    private static ComponentClass Control(string typeName, string defaultName, PropertyDefinition[] properties, EventDefinition[] events,
        IReadOnlyDictionary<PropertyDefinition, object?>? defaults = null, bool isContainer = false, bool isWindowless = false,
        MethodDefinition[]? methods = null, PropertyDefinition[]? writableOnAnRDFormsForm = null) =>
        new(typeName, defaultName, ComponentKind.Control,
            [.. properties, P.Left, P.Top, P.Width, P.Height, P.Visible, P.Opacity, P.Tag,
             .. events.Contains(MouseUp) ? [P.ContextMenu] : Array.Empty<PropertyDefinition>()],
            events, defaults, isContainer, isWindowless,
            methods: [M.Move, M.Refresh, .. events.Contains(GotFocus) ? [M.SetFocus] : Array.Empty<MethodDefinition>(), M.ZOrder, .. methods ?? []],
            defaultMember: DefaultMembers.GetValueOrDefault(typeName), writableAtRunTimeOnAnRDFormsForm: writableOnAnRDFormsForm);

    private static Dictionary<PropertyDefinition, object?> Defaults(params (PropertyDefinition Property, object? Value)[] defaults) =>
        defaults.ToDictionary(d => d.Property, d => d.Value);
}
