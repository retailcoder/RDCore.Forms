namespace RDCore.Forms.Model;

/// <summary>
/// The methods of the <c>VB</c> library's classes that RDForms runs: each definition is shared by every class that has the method.
/// </summary>
public static class Methods
{
    /// <summary>
    /// <c>SetFocus</c>: gives the focus to a control, or activates a form.
    /// </summary>
    public static readonly MethodDefinition SetFocus = new("SetFocus");

    /// <summary>
    /// <c>Move Left, [Top], [Width], [Height]</c>: sets a component's position and size in one call.
    /// </summary>
    public static readonly MethodDefinition Move = new("Move",
        new MethodParameter("Left", "Single"), new MethodParameter("Top", "Variant", IsOptional: true),
        new MethodParameter("Width", "Variant", IsOptional: true), new MethodParameter("Height", "Variant", IsOptional: true));

    /// <summary>
    /// <c>ZOrder [Position]</c>: brings a control to the front of its plane, or sends it to the back.
    /// </summary>
    public static readonly MethodDefinition ZOrder = new("ZOrder", new MethodParameter("Position", "Variant", IsOptional: true));

    /// <summary>
    /// <c>Refresh</c>: draws a component and its contents again.
    /// </summary>
    public static readonly MethodDefinition Refresh = new("Refresh");

    /// <summary>
    /// <c>UpdateRecord</c>: writes the values of a Data control's bound controls to its current record.
    /// </summary>
    public static readonly MethodDefinition UpdateRecord = new("UpdateRecord");

    /// <summary>
    /// <c>UpdateControls</c>: gives a Data control's bound controls the values of its current record again.
    /// </summary>
    public static readonly MethodDefinition UpdateControls = new("UpdateControls");

    /// <summary>
    /// <c>AddItem Item, [Index]</c>: adds an item to a ListBox or ComboBox, at the end where no index is given.
    /// </summary>
    public static readonly MethodDefinition AddItem = new("AddItem",
        new MethodParameter("Item", "String"), new MethodParameter("Index", "Variant", IsOptional: true));

    /// <summary>
    /// <c>RemoveItem Index</c>: removes an item from a ListBox or ComboBox.
    /// </summary>
    public static readonly MethodDefinition RemoveItem = new("RemoveItem", new MethodParameter("Index", "Integer"));

    /// <summary>
    /// <c>Clear</c>: removes every item of a ListBox or ComboBox.
    /// </summary>
    public static readonly MethodDefinition Clear = new("Clear");

    /// <summary>
    /// <c>List(Index)</c>, a property: the text of an item of a ListBox or ComboBox.
    /// </summary>
    public static readonly MethodDefinition List = new("List", new MethodParameter("Index", "Integer"))
    {
        Value = new MethodParameter("List", "String"),
    };

    /// <summary>
    /// <c>ItemData(Index)</c>, a property: the Long value attached to an item of a ListBox or ComboBox.
    /// </summary>
    public static readonly MethodDefinition ItemData = new("ItemData", new MethodParameter("Index", "Integer"))
    {
        Value = new MethodParameter("ItemData", "Long"),
    };

    /// <summary>
    /// <c>ItemIcon(Index)</c>, a property, an RD-VBA addition: the picture drawn before an item of a ListBox or ComboBox, or <c>Nothing</c>.
    /// The window gives and takes it as an object (<see cref="FormValue.FromObject"/>): the bytes of the picture's file, or
    /// <see langword="null"/> for none.
    /// </summary>
    public static readonly MethodDefinition ItemIcon = new("ItemIcon", new MethodParameter("Index", "Integer"))
    {
        Value = new MethodParameter("ItemIcon", "Picture"),
    };

    /// <summary>
    /// <c>Selected(Index)</c>, a property: whether an item of a ListBox is selected.
    /// </summary>
    public static readonly MethodDefinition Selected = new("Selected", new MethodParameter("Index", "Integer"))
    {
        Value = new MethodParameter("Selected", "Boolean"),
    };

    /// <summary>
    /// <c>ListIndex</c>, a property: the selected item of a ListBox or ComboBox, -1 for none.
    /// </summary>
    public static readonly MethodDefinition ListIndex = new("ListIndex") { Value = new MethodParameter("ListIndex", "Integer") };

    /// <summary>
    /// <c>ListCount</c>, a read-only property: the number of items of a ListBox or ComboBox.
    /// </summary>
    public static readonly MethodDefinition ListCount = new("ListCount") { Value = new MethodParameter("ListCount", "Integer"), IsReadOnly = true };

    /// <summary>
    /// <c>NewIndex</c>, a read-only property: the index of the item a ListBox's or ComboBox's last <c>AddItem</c> added.
    /// </summary>
    public static readonly MethodDefinition NewIndex = new("NewIndex") { Value = new MethodParameter("NewIndex", "Integer"), IsReadOnly = true };

    /// <summary>
    /// <c>SelCount</c>, a read-only property: the number of items of a ListBox that are selected.
    /// </summary>
    public static readonly MethodDefinition SelCount = new("SelCount") { Value = new MethodParameter("SelCount", "Integer"), IsReadOnly = true };

    /// <summary>
    /// <c>Text</c>, a property: the text of a ListBox's selected item, or a ComboBox's text. A ComboBox whose style is not Dropdown List
    /// keeps its text in the model as well (<see cref="Properties.Text"/>).
    /// </summary>
    public static readonly MethodDefinition ListText = new("Text") { Value = new MethodParameter("Text", "String") };
}
