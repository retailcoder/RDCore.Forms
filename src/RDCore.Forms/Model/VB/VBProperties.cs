// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

namespace RDCore.Forms.Model.VB;

/// <summary>
/// The properties of VB's intrinsic forms and controls that a designer file sets, with their VB6 defaults.
/// </summary>
/// <remarks>
/// <para>
/// A component's name and its control-array index are not properties here: they are <see cref="ComponentInstance.Name"/> and
/// <see cref="ComponentInstance.Index"/>. Where two classes have a property of the same name with different types, there is one
/// definition per type, such as <see cref="CheckBoxValue"/> and <see cref="OptionButtonValue"/>. A picture is held as the bytes of
/// its image file.
/// </para>
/// <para>
/// A number has the range of its VB type: a <see cref="short"/> is an Integer and an <see cref="int"/> a Long. A
/// <see cref="double"/> property, such as <see cref="Left"/>, is a Single: the VB6 import refuses a value outside a Single's range.
/// </para>
/// </remarks>
public static class VBProperties
{
    /// <summary>
    /// A control's left edge, in its container's scale; a form's client area's left edge on the screen, in twips.
    /// </summary>
    public static readonly PropertyDefinition<double> Left = new("Left", PropertyCategory.Position);

    /// <summary>
    /// A control's top edge, in its container's scale; a form's client area's top edge on the screen, in twips.
    /// </summary>
    public static readonly PropertyDefinition<double> Top = new("Top", PropertyCategory.Position);

    /// <summary>
    /// A control's width, in its container's scale; a form's client area's width, in twips.
    /// </summary>
    public static readonly PropertyDefinition<double> Width = new("Width", PropertyCategory.Position);

    /// <summary>
    /// A control's height, in its container's scale; a form's client area's height, in twips.
    /// </summary>
    public static readonly PropertyDefinition<double> Height = new("Height", PropertyCategory.Position);

    /// <summary>
    /// The unit of a form's or a PictureBox's scale, which its contents' positions and sizes are in.
    /// </summary>
    public static readonly PropertyDefinition<ScaleMode> ScaleMode = new("ScaleMode", PropertyCategory.Position, Model.ScaleMode.Twip);

    /// <summary>
    /// The horizontal coordinate of a form's or a PictureBox's inside left edge, in its own scale.
    /// </summary>
    public static readonly PropertyDefinition<double> ScaleLeft = new("ScaleLeft", PropertyCategory.Position);

    /// <summary>
    /// The vertical coordinate of a form's or a PictureBox's inside top edge, in its own scale.
    /// </summary>
    public static readonly PropertyDefinition<double> ScaleTop = new("ScaleTop", PropertyCategory.Position);

    /// <summary>
    /// The inside width of a form or a PictureBox, in its own scale. A designer file writes it for every Form, UserControl and
    /// PropertyPage; in a scale other than <see cref="Model.ScaleMode.User"/> it follows from the component's size.
    /// </summary>
    public static readonly PropertyDefinition<double> ScaleWidth = new("ScaleWidth", PropertyCategory.Position);

    /// <summary>
    /// The inside height of a form or a PictureBox, in its own scale. A designer file writes it for every Form, UserControl and
    /// PropertyPage; in a scale other than <see cref="Model.ScaleMode.User"/> it follows from the component's size.
    /// </summary>
    public static readonly PropertyDefinition<double> ScaleHeight = new("ScaleHeight", PropertyCategory.Position);

    /// <summary>
    /// The text of a form's title bar, a property page's tab, a button's face, a label, a frame's title or a menu item, or the text
    /// beside a check box or an option button.
    /// </summary>
    public static readonly PropertyDefinition<string> Caption = new("Caption", PropertyCategory.Appearance, "");

    /// <summary>
    /// A string of the program's own, attached to the component.
    /// </summary>
    public static readonly PropertyDefinition<string> Tag = new("Tag", PropertyCategory.Misc, "");

    /// <summary>
    /// Whether the component is shown while the program runs.
    /// </summary>
    public static readonly PropertyDefinition<bool> Visible = new("Visible", PropertyCategory.Behavior, true);

    /// <summary>
    /// Whether the component responds to the user.
    /// </summary>
    public static readonly PropertyDefinition<bool> Enabled = new("Enabled", PropertyCategory.Behavior, true);

    /// <summary>
    /// Whether moving the focus to the control validates the control that loses it.
    /// </summary>
    public static readonly PropertyDefinition<bool> CausesValidation = new("CausesValidation", PropertyCategory.Behavior, true);

    /// <summary>
    /// The pointer shown over the component.
    /// </summary>
    public static readonly PropertyDefinition<MousePointer> MousePointer = new("MousePointer", PropertyCategory.Misc, VB.MousePointer.Default);

    /// <summary>
    /// Whether the component's text and layout read right to left.
    /// </summary>
    public static readonly PropertyDefinition<bool> RightToLeft = new("RightToLeft", PropertyCategory.Behavior);

    /// <summary>
    /// The tooltip shown when the pointer rests on the control. No tooltip is shown when it is empty.
    /// </summary>
    public static readonly PropertyDefinition<string> ToolTipText = new("ToolTipText", PropertyCategory.Misc, "");

    /// <summary>
    /// The context number of the component's help topic.
    /// </summary>
    public static readonly PropertyDefinition<int> HelpContextID = new("HelpContextID", PropertyCategory.Misc);

    /// <summary>
    /// The context number of the control's "What's This?" help topic.
    /// </summary>
    public static readonly PropertyDefinition<int> WhatsThisHelpID = new("WhatsThisHelpID", PropertyCategory.Misc);

    /// <summary>
    /// The font of the component's text.
    /// </summary>
    public static readonly PropertyDefinition<FormFont> Font = new("Font", PropertyCategory.Appearance, FormFont.Default);

    /// <summary>
    /// How the control aligns its text, or its caption.
    /// </summary>
    public static readonly PropertyDefinition<TextAlignment> Alignment = new("Alignment", PropertyCategory.Misc);

    /// <summary>
    /// Whether the control is drawn flat or with a three-dimensional edge.
    /// </summary>
    public static readonly PropertyDefinition<Appearance> Appearance = new("Appearance", PropertyCategory.Appearance, VB.Appearance.ThreeD);

    /// <summary>
    /// Whether the control resizes itself to fit its contents.
    /// </summary>
    public static readonly PropertyDefinition<bool> AutoSize = new("AutoSize", PropertyCategory.Position);

    /// <summary>
    /// The control's border.
    /// </summary>
    public static readonly PropertyDefinition<BorderStyle> BorderStyle = new("BorderStyle", PropertyCategory.Appearance, VB.BorderStyle.None);

    /// <summary>
    /// The colour of the component's text and drawing.
    /// </summary>
    public static readonly PropertyDefinition<FormColor> ForeColor = new("ForeColor", PropertyCategory.Appearance, FormColor.FromSystem(SystemColor.ButtonText));

    /// <summary>
    /// The colour of the component's background.
    /// </summary>
    public static readonly PropertyDefinition<FormColor> BackColor = new("BackColor", PropertyCategory.Appearance, FormColor.FromSystem(SystemColor.ButtonFace));

    /// <summary>
    /// Whether an ampersand in a Label's caption marks the next character as its access key.
    /// </summary>
    public static readonly PropertyDefinition<bool> UseMnemonic = new("UseMnemonic", PropertyCategory.Misc, true);

    /// <summary>
    /// Whether a Label whose <see cref="AutoSize"/> is set grows down, wrapping its caption at its width, or across, keeping its caption
    /// on one line. It has no effect when <see cref="AutoSize"/> is not set.
    /// </summary>
    public static readonly PropertyDefinition<bool> WordWrap = new("WordWrap", PropertyCategory.Misc);

    /// <summary>
    /// The edge of its form a PictureBox is docked to.
    /// </summary>
    public static readonly PropertyDefinition<Align> Align = new("Align", PropertyCategory.Position);

    /// <summary>
    /// Whether drawing is kept in a bitmap and repainted from it.
    /// </summary>
    public static readonly PropertyDefinition<bool> AutoRedraw = new("AutoRedraw", PropertyCategory.Behavior);

    /// <summary>
    /// The picture the component shows, as the bytes of its image file (a bitmap, icon, cursor, GIF, JPEG or metafile), or
    /// <see langword="null"/> for none.
    /// </summary>
    public static readonly PropertyDefinition<byte[]?> Picture = new("Picture", PropertyCategory.Appearance, null);

    /// <summary>
    /// The icon a form shows in its title bar, as the bytes of its icon file, or <see langword="null"/> for none.
    /// </summary>
    public static readonly PropertyDefinition<byte[]?> Icon = new("Icon", PropertyCategory.Appearance, null);

    /// <summary>
    /// The pointer shown when <see cref="MousePointer"/> is <see cref="VB.MousePointer.Custom"/>, as the bytes of its icon or cursor
    /// file, or <see langword="null"/> for none.
    /// </summary>
    public static readonly PropertyDefinition<byte[]?> MouseIcon = new("MouseIcon", PropertyCategory.Behavior, null);

    /// <summary>
    /// The image shown while the control is dragged, as the bytes of its icon file, or <see langword="null"/> for none.
    /// </summary>
    public static readonly PropertyDefinition<byte[]?> DragIcon = new("DragIcon", PropertyCategory.Behavior, null);

    /// <summary>
    /// Whether an Image scales its picture to its size; when not set, the Image takes its picture's size.
    /// </summary>
    public static readonly PropertyDefinition<bool> Stretch = new("Stretch", PropertyCategory.Appearance);

    /// <summary>
    /// A ListBox's or ComboBox's items, in order.
    /// </summary>
    public static readonly PropertyDefinition<IReadOnlyList<string>> List = new("List", PropertyCategory.List, []);

    /// <summary>
    /// The Long value attached to each item of a ListBox or ComboBox, in the order of <see cref="List"/>.
    /// </summary>
    public static readonly PropertyDefinition<IReadOnlyList<int>> ItemData = new("ItemData", PropertyCategory.Misc, []);

    /// <summary>
    /// The milliseconds between a Timer's events. 0 stops the Timer.
    /// </summary>
    public static readonly PropertyDefinition<int> Interval = new("Interval", PropertyCategory.Misc);

    /// <summary>
    /// The text a TextBox or a ComboBox holds.
    /// </summary>
    public static readonly PropertyDefinition<string> Text = new("Text", PropertyCategory.Misc, "");

    /// <summary>
    /// Whether a Label's or Shape's background is drawn.
    /// </summary>
    public static readonly PropertyDefinition<BackStyle> BackStyle = new("BackStyle", PropertyCategory.Appearance, VB.BackStyle.Transparent);

    /// <summary>
    /// The colour of a Shape's border or of a Line.
    /// </summary>
    public static readonly PropertyDefinition<FormColor> BorderColor = new("BorderColor", PropertyCategory.Appearance, FormColor.Black);

    /// <summary>
    /// How a Shape's border or a Line is drawn.
    /// </summary>
    public static readonly PropertyDefinition<ShapeBorderStyle> ShapeBorderStyle = new("BorderStyle", PropertyCategory.Appearance, VB.ShapeBorderStyle.Solid);

    /// <summary>
    /// The width of a Shape's border or of a Line, in pixels.
    /// </summary>
    public static readonly PropertyDefinition<short> BorderWidth = new("BorderWidth", PropertyCategory.Appearance, 1);

    /// <summary>
    /// The colour a figure is filled with.
    /// </summary>
    public static readonly PropertyDefinition<FormColor> FillColor = new("FillColor", PropertyCategory.Appearance, FormColor.Black);

    /// <summary>
    /// How a figure is filled.
    /// </summary>
    public static readonly PropertyDefinition<FillStyle> FillStyle = new("FillStyle", PropertyCategory.Appearance, VB.FillStyle.Transparent);

    /// <summary>
    /// The figure a Shape draws.
    /// </summary>
    public static readonly PropertyDefinition<ShapeType> Shape = new("Shape", PropertyCategory.Appearance);

    /// <summary>
    /// The horizontal coordinate of a Line's first end, in its container's scale.
    /// </summary>
    public static readonly PropertyDefinition<double> X1 = new("X1", PropertyCategory.Position);

    /// <summary>
    /// The vertical coordinate of a Line's first end, in its container's scale.
    /// </summary>
    public static readonly PropertyDefinition<double> Y1 = new("Y1", PropertyCategory.Position);

    /// <summary>
    /// The horizontal coordinate of a Line's second end, in its container's scale.
    /// </summary>
    public static readonly PropertyDefinition<double> X2 = new("X2", PropertyCategory.Position);

    /// <summary>
    /// The vertical coordinate of a Line's second end, in its container's scale.
    /// </summary>
    public static readonly PropertyDefinition<double> Y2 = new("Y2", PropertyCategory.Position);

    /// <summary>
    /// How a Line's colour combines with what is drawn under it: a raster operation from 1 to 16. 13, the default, draws the colour
    /// as it is.
    /// </summary>
    public static readonly PropertyDefinition<short> DrawMode = new("DrawMode", PropertyCategory.Appearance, 13);

    /// <summary>
    /// How much a scroll bar's value changes when the user clicks its track.
    /// </summary>
    public static readonly PropertyDefinition<short> LargeChange = new("LargeChange", PropertyCategory.Behavior, 1);

    /// <summary>
    /// How much a scroll bar's value changes when the user clicks one of its arrows.
    /// </summary>
    public static readonly PropertyDefinition<short> SmallChange = new("SmallChange", PropertyCategory.Behavior, 1);

    /// <summary>
    /// A scroll bar's value at its left or top end.
    /// </summary>
    public static readonly PropertyDefinition<short> Min = new("Min", PropertyCategory.Behavior);

    /// <summary>
    /// A scroll bar's value at its right or bottom end.
    /// </summary>
    public static readonly PropertyDefinition<short> Max = new("Max", PropertyCategory.Behavior, 32767);

    /// <summary>
    /// A scroll bar's value, from <see cref="Min"/> to <see cref="Max"/>.
    /// </summary>
    public static readonly PropertyDefinition<short> ScrollBarValue = new("Value", PropertyCategory.Misc);

    /// <summary>
    /// A CheckBox's state, an Integer to a program.
    /// </summary>
    public static readonly PropertyDefinition<CheckBoxValue> CheckBoxValue = new("Value", PropertyCategory.Misc, VB.CheckBoxValue.Unchecked);

    /// <summary>
    /// Whether an OptionButton is the selected one of its group, a Boolean to a program.
    /// </summary>
    public static readonly PropertyDefinition<bool> OptionButtonValue = new("Value", PropertyCategory.Misc);

    /// <summary>
    /// Whether the user is prevented from changing a TextBox's or a ComboBox's text.
    /// </summary>
    public static readonly PropertyDefinition<bool> Locked = new("Locked", PropertyCategory.Behavior);

    /// <summary>
    /// Whether a ListBox selects more than one item, and how.
    /// </summary>
    public static readonly PropertyDefinition<ListBoxMultiSelect> MultiSelect = new("MultiSelect", PropertyCategory.Behavior, ListBoxMultiSelect.None);

    /// <summary>
    /// How a ComboBox is drawn and behaves.
    /// </summary>
    public static readonly PropertyDefinition<ComboBoxStyle> ComboBoxStyle = new("Style", PropertyCategory.Behavior, VB.ComboBoxStyle.DropdownCombo);

    /// <summary>
    /// Where a form opens on the screen.
    /// </summary>
    public static readonly PropertyDefinition<StartUpPosition> StartUpPosition = new("StartUpPosition", PropertyCategory.Position, VB.StartUpPosition.Manual);

    /// <summary>
    /// Whether a form has a button on the task bar.
    /// </summary>
    public static readonly PropertyDefinition<bool> ShowInTaskbar = new("ShowInTaskbar", PropertyCategory.Misc, true);

    /// <summary>
    /// The control's position in its form's tab order.
    /// </summary>
    public static readonly PropertyDefinition<short> TabIndex = new("TabIndex", PropertyCategory.Behavior);

    /// <summary>
    /// Whether the Tab key moves the focus to the control.
    /// </summary>
    public static readonly PropertyDefinition<bool> TabStop = new("TabStop", PropertyCategory.Behavior, true);

    /// <summary>
    /// Whether a form receives keyboard events before the control that has the focus.
    /// </summary>
    public static readonly PropertyDefinition<bool> KeyPreview = new("KeyPreview", PropertyCategory.Behavior);

    /// <summary>
    /// Whether a form's title bar has a Maximize button.
    /// </summary>
    public static readonly PropertyDefinition<bool> MaxButton = new("MaxButton", PropertyCategory.Appearance, true);

    /// <summary>
    /// Whether a form's title bar has a Minimize button.
    /// </summary>
    public static readonly PropertyDefinition<bool> MinButton = new("MinButton", PropertyCategory.Appearance, true);

    /// <summary>
    /// Whether a menu item shows a check mark.
    /// </summary>
    public static readonly PropertyDefinition<bool> Checked = new("Checked", PropertyCategory.Appearance);

    /// <summary>
    /// Whether a menu lists the open child windows of a multiple-document interface form.
    /// </summary>
    public static readonly PropertyDefinition<bool> WindowList = new("WindowList", PropertyCategory.Misc);
}
