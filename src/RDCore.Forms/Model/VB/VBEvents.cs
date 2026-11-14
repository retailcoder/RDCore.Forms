// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

namespace RDCore.Forms.Model.VB;

/// <summary>
/// The events of VB's intrinsic forms and controls, with their parameters as a handler declares them.
/// </summary>
public static class VBEvents
{
    private static readonly EventParameter KeyCode = new("KeyCode", "Integer");
    private static readonly EventParameter Shift = new("Shift", "Integer");
    private static readonly EventParameter Button = new("Button", "Integer");
    private static readonly EventParameter X = new("X", "Single");
    private static readonly EventParameter Y = new("Y", "Single");

    /// <summary>
    /// The user clicked the component or chose it with the keyboard, or a program set a CheckBox's or an OptionButton's
    /// <c>Value</c> or a list's <c>ListIndex</c>.
    /// </summary>
    public static readonly EventDefinition Click = new("Click");

    /// <summary>
    /// The user double-clicked the component.
    /// </summary>
    public static readonly EventDefinition DblClick = new("DblClick");

    /// <summary>
    /// The component received the focus: a control, or a form none of whose controls can take it.
    /// </summary>
    public static readonly EventDefinition GotFocus = new("GotFocus");

    /// <summary>
    /// The component lost the focus.
    /// </summary>
    public static readonly EventDefinition LostFocus = new("LostFocus");

    /// <summary>
    /// A key went down.
    /// </summary>
    public static readonly EventDefinition KeyDown = new("KeyDown", KeyCode, Shift);

    /// <summary>
    /// A key produced a character, which the handler can change, or cancel by setting it to 0.
    /// </summary>
    public static readonly EventDefinition KeyPress = new("KeyPress", new EventParameter("KeyAscii", "Integer"));

    /// <summary>
    /// A key went up.
    /// </summary>
    public static readonly EventDefinition KeyUp = new("KeyUp", KeyCode, Shift);

    /// <summary>
    /// A mouse button went down over the component.
    /// </summary>
    public static readonly EventDefinition MouseDown = new("MouseDown", Button, Shift, X, Y);

    /// <summary>
    /// The mouse moved over the component.
    /// </summary>
    public static readonly EventDefinition MouseMove = new("MouseMove", Button, Shift, X, Y);

    /// <summary>
    /// A mouse button went up over the component.
    /// </summary>
    public static readonly EventDefinition MouseUp = new("MouseUp", Button, Shift, X, Y);

    /// <summary>
    /// A TextBox's or ComboBox's text, a scroll bar's value, a DirListBox's <c>Path</c> or a DriveListBox's <c>Drive</c> changed.
    /// </summary>
    public static readonly EventDefinition Change = new("Change");

    /// <summary>
    /// A FileListBox's <c>Path</c> changed.
    /// </summary>
    public static readonly EventDefinition PathChange = new("PathChange");

    /// <summary>
    /// A FileListBox's <c>Pattern</c> changed.
    /// </summary>
    public static readonly EventDefinition PatternChange = new("PatternChange");

    /// <summary>
    /// A scroll bar's thumb was dragged to a new value. The Change comes once the thumb is released, where the value is not the one
    /// the drag started at.
    /// </summary>
    public static readonly EventDefinition Scroll = new("Scroll");

    /// <summary>
    /// A ComboBox's list is about to drop down.
    /// </summary>
    public static readonly EventDefinition DropDown = new("DropDown");

    /// <summary>
    /// A Timer's interval elapsed.
    /// </summary>
    public static readonly EventDefinition Timer = new("Timer");

    /// <summary>
    /// A form was loaded.
    /// </summary>
    public static readonly EventDefinition Load = new("Load");

    /// <summary>
    /// A form or a UserControl was resized.
    /// </summary>
    public static readonly EventDefinition Resize = new("Resize");

    /// <summary>
    /// A form became the program's active form.
    /// </summary>
    public static readonly EventDefinition Activate = new("Activate");

    /// <summary>
    /// A form stopped being the program's active form, as another of its forms became active.
    /// </summary>
    public static readonly EventDefinition Deactivate = new("Deactivate");

    /// <summary>
    /// A form is about to unload. A handler that sets <c>Cancel</c> to a value other than 0 keeps it loaded. <c>UnloadMode</c> says
    /// why it unloads: 0 for its close box, 1 for an <c>Unload</c> statement, 2 for Windows ending its session, 3 for the Task
    /// Manager, 4 for its MDI form closing, and 5 for its owner closing.
    /// </summary>
    public static readonly EventDefinition QueryUnload = new("QueryUnload",
        new EventParameter("Cancel", "Integer"), new EventParameter("UnloadMode", "Integer"));

    /// <summary>
    /// A form is unloading, after its QueryUnload. A handler that sets <c>Cancel</c> to a value other than 0 keeps it loaded.
    /// </summary>
    public static readonly EventDefinition Unload = new("Unload", new EventParameter("Cancel", "Integer"));

    /// <summary>
    /// The user pressed an access key of a UserControl.
    /// </summary>
    public static readonly EventDefinition AccessKeyPress = new("AccessKeyPress", new EventParameter("KeyAscii", "Integer"));

    /// <summary>
    /// An ambient property of a UserControl's container changed.
    /// </summary>
    public static readonly EventDefinition AmbientChanged = new("AmbientChanged", new EventParameter("PropertyName", "String"));

    /// <summary>
    /// An asynchronous read of a UserControl finished.
    /// </summary>
    public static readonly EventDefinition AsyncReadComplete = new("AsyncReadComplete", new EventParameter("AsyncProp", "AsyncProperty"));

    /// <summary>
    /// An asynchronous read of a UserControl progressed.
    /// </summary>
    public static readonly EventDefinition AsyncReadProgress = new("AsyncReadProgress", new EventParameter("AsyncProp", "AsyncProperty"));

    /// <summary>
    /// A drag ended over the component.
    /// </summary>
    public static readonly EventDefinition DragDrop = new("DragDrop", new EventParameter("Source", "Control"), X, Y);

    /// <summary>
    /// A drag moved over the component.
    /// </summary>
    public static readonly EventDefinition DragOver = new("DragOver", new EventParameter("Source", "Control"), X, Y, new EventParameter("State", "Integer"));

    /// <summary>
    /// The focus entered a UserControl or one of its controls.
    /// </summary>
    public static readonly EventDefinition EnterFocus = new("EnterFocus");

    /// <summary>
    /// The focus left a UserControl and its controls.
    /// </summary>
    public static readonly EventDefinition ExitFocus = new("ExitFocus");

    /// <summary>
    /// A data-source UserControl is asked for a data member.
    /// </summary>
    public static readonly EventDefinition GetDataMember = new("GetDataMember", new EventParameter("DataMember", "String"), new EventParameter("Data", "Object"));

    /// <summary>
    /// A UserControl was hidden.
    /// </summary>
    public static readonly EventDefinition Hide = new("Hide");

    /// <summary>
    /// A windowless UserControl is asked whether a point hits it.
    /// </summary>
    public static readonly EventDefinition HitTest = new("HitTest", X, Y, new EventParameter("HitResult", "Integer"));

    /// <summary>
    /// A form, a UserControl or a PropertyPage was created.
    /// </summary>
    public static readonly EventDefinition Initialize = new("Initialize");

    /// <summary>
    /// A UserControl placed for the first time sets its properties' defaults.
    /// </summary>
    public static readonly EventDefinition InitProperties = new("InitProperties");

    /// <summary>
    /// An OLE drag from the component finished.
    /// </summary>
    public static readonly EventDefinition OLECompleteDrag = new("OLECompleteDrag", new EventParameter("Effect", "Long"));

    /// <summary>
    /// An OLE drag was dropped on the component.
    /// </summary>
    public static readonly EventDefinition OLEDragDrop = new("OLEDragDrop", new EventParameter("Data", "DataObject"), new EventParameter("Effect", "Long"), Button, Shift, X, Y);

    /// <summary>
    /// An OLE drag moved over the component.
    /// </summary>
    public static readonly EventDefinition OLEDragOver = new("OLEDragOver", new EventParameter("Data", "DataObject"), new EventParameter("Effect", "Long"), Button, Shift, X, Y, new EventParameter("State", "Integer"));

    /// <summary>
    /// An OLE drag from the component asks which pointer to show.
    /// </summary>
    public static readonly EventDefinition OLEGiveFeedback = new("OLEGiveFeedback", new EventParameter("Effect", "Long"), new EventParameter("DefaultCursors", "Boolean"));

    /// <summary>
    /// An OLE drop target asks the component for data it did not supply when the drag started.
    /// </summary>
    public static readonly EventDefinition OLESetData = new("OLESetData", new EventParameter("Data", "DataObject"), new EventParameter("DataFormat", "Integer"));

    /// <summary>
    /// An OLE drag from the component started.
    /// </summary>
    public static readonly EventDefinition OLEStartDrag = new("OLEStartDrag", new EventParameter("Data", "DataObject"), new EventParameter("AllowedEffects", "Long"));

    /// <summary>
    /// The component must be redrawn.
    /// </summary>
    public static readonly EventDefinition Paint = new("Paint");

    /// <summary>
    /// A UserControl reads its saved properties.
    /// </summary>
    public static readonly EventDefinition ReadProperties = new("ReadProperties", new EventParameter("PropBag", "PropertyBag"));

    /// <summary>
    /// A UserControl was shown.
    /// </summary>
    public static readonly EventDefinition Show = new("Show");

    /// <summary>
    /// A form, a UserControl or a PropertyPage is about to be destroyed.
    /// </summary>
    public static readonly EventDefinition Terminate = new("Terminate");

    /// <summary>
    /// A UserControl saves its properties.
    /// </summary>
    public static readonly EventDefinition WriteProperties = new("WriteProperties", new EventParameter("PropBag", "PropertyBag"));

    /// <summary>
    /// A PropertyPage applies its changes to the selected controls.
    /// </summary>
    public static readonly EventDefinition ApplyChanges = new("ApplyChanges");

    /// <summary>
    /// A PropertyPage is asked to edit one property.
    /// </summary>
    public static readonly EventDefinition EditProperty = new("EditProperty", new EventParameter("PropertyName", "String"));

    /// <summary>
    /// The controls a PropertyPage edits changed.
    /// </summary>
    public static readonly EventDefinition SelectionChanged = new("SelectionChanged");
}
