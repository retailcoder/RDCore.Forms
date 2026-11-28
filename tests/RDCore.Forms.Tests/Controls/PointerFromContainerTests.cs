using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.VisualTree;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using MousePointer = RDCore.Forms.Model.MousePointer;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// The pointer of a control whose MousePointer is Default (0), as measured with VB6, native and p-code agreeing: the pointer of its
// nearest container (its Frame or its PictureBox, then the form) whose MousePointer is not Default, and else its own. A control whose
// MousePointer is not Default shows its own, whatever its containers'.
[TestClass]
public sealed class PointerFromContainerTests
{
    // The controls of the form VB6 was measured with: Command1 and Text1 on the form, Text2 in Picture1, Text3 and Command2 in Frame1.
    private static readonly string[] Names = ["Command1", "Text1", "Picture1", "Text2", "Frame1", "Text3", "Command2"];

    private static FormDefinition Form()
    {
        var root = new ComponentInstance(ComponentClasses.Form, "Form1");
        var picture = Add(ComponentClasses.PictureBox, "Picture1", root);
        var frame = Add(ComponentClasses.Frame, "Frame1", root);
        Add(ComponentClasses.CommandButton, "Command1", root);
        Add(ComponentClasses.TextBox, "Text1", root);
        Add(ComponentClasses.TextBox, "Text2", picture);
        Add(ComponentClasses.TextBox, "Text3", frame);
        Add(ComponentClasses.CommandButton, "Command2", frame);
        return new FormDefinition(root);
    }

    private static ComponentInstance Add(ComponentClass type, string name, ComponentInstance container)
    {
        var component = new ComponentInstance(type, name);
        component.SetContainer(container);
        return component;
    }

    private static string Name(Cursor? cursor) => cursor?.ToString() ?? "none";

    // Where the pointer is over a control: a TextBox's text, which its theme gives a pointer of its own, or else the control.
    private static InputElement Over(Control control) =>
        control is TextBox ? control.GetVisualDescendants().OfType<TextPresenter>().Single() : control;

    // The pointer each control shows, by name, with the form shown, after the pointers are set, then after a change to them.
    private static (Dictionary<string, string> Set, Dictionary<string, string> Changed) Shown(Action<FormDefinition> set,
        Action<FormDefinition>? change = null) => UI.Run(() =>
    {
        var form = Form();
        set(form);
        var view = new RDFormsFormView(form);
        var (window, _) = ControlHost.Show(view);
        Dictionary<string, string> Read() => Names.ToDictionary(name => name, name => Name(Over(view.ControlOf(form.Find(name)!)!).Cursor));
        var before = Read();
        change?.Invoke(form);
        var after = Read();
        window.Close();
        return (before, after);
    });

    // The pointer each control shows with no MousePointer anywhere: its own, its theme's, such as a TextBox's I-beam.
    private static readonly Lazy<Dictionary<string, string>> Own = new(() => Shown(_ => { }).Set);

    private static string Of(MousePointer pointer) => UI.Run(() => Name(new Cursor(pointer switch
    {
        MousePointer.Hourglass => StandardCursorType.Wait,
        MousePointer.Cross => StandardCursorType.Cross,
        MousePointer.IBeam => StandardCursorType.Ibeam,
        _ => StandardCursorType.Arrow,
    })));

    private static void Pointer(FormDefinition form, string name, MousePointer pointer) =>
        (name == "Form1" ? form.Root : form.Find(name)!).Set(P.MousePointer, pointer);

    [TestMethod]
    public void TheFormsPointer_IsEveryDefaultControls_InAFrameAndAPictureBoxToo()
    {
        var (shown, _) = Shown(form => Pointer(form, "Form1", MousePointer.Hourglass));

        CollectionAssert.AreEquivalent(Names.ToDictionary(n => n, _ => Of(MousePointer.Hourglass)), shown);
    }

    [TestMethod]
    public void AControlsOwnPointer_IsShown_WhateverItsFormsIs()
    {
        var (shown, _) = Shown(form =>
        {
            Pointer(form, "Form1", MousePointer.Hourglass);
            Pointer(form, "Text1", MousePointer.Arrow);
            Pointer(form, "Command1", MousePointer.Cross);
        });

        Assert.AreEqual(Of(MousePointer.Arrow), shown["Text1"]);
        Assert.AreEqual(Of(MousePointer.Cross), shown["Command1"]);
        Assert.AreEqual(Of(MousePointer.Hourglass), shown["Text2"]);
        Assert.AreEqual(Of(MousePointer.Hourglass), shown["Text3"]);
    }

    [TestMethod]
    public void TheNearestContainersPointer_IsShown()
    {
        var (frameOnly, _) = Shown(form => Pointer(form, "Frame1", MousePointer.Hourglass));
        var (both, _) = Shown(form =>
        {
            Pointer(form, "Form1", MousePointer.Hourglass);
            Pointer(form, "Frame1", MousePointer.IBeam);
            Pointer(form, "Picture1", MousePointer.Cross);
        });

        var own = Own.Value;
        CollectionAssert.AreEquivalent(new Dictionary<string, string>
        {
            ["Command1"] = own["Command1"], ["Text1"] = own["Text1"], ["Picture1"] = own["Picture1"], ["Text2"] = own["Text2"],
            ["Frame1"] = Of(MousePointer.Hourglass), ["Text3"] = Of(MousePointer.Hourglass), ["Command2"] = Of(MousePointer.Hourglass),
        }, frameOnly);
        CollectionAssert.AreEquivalent(new Dictionary<string, string>
        {
            ["Command1"] = Of(MousePointer.Hourglass), ["Text1"] = Of(MousePointer.Hourglass),
            ["Picture1"] = Of(MousePointer.Cross), ["Text2"] = Of(MousePointer.Cross),
            ["Frame1"] = Of(MousePointer.IBeam), ["Text3"] = Of(MousePointer.IBeam), ["Command2"] = Of(MousePointer.IBeam),
        }, both);
    }

    // A program's change to a container's pointer shows at once in the controls it holds, and Default again gives them back their own.
    [TestMethod]
    public void AChangeToAContainersPointer_ShowsInItsDefaultControls()
    {
        var (_, set) = Shown(_ => { }, form => Pointer(form, "Frame1", MousePointer.Hourglass));
        var (_, reset) = Shown(form => Pointer(form, "Form1", MousePointer.Cross), form => Pointer(form, "Form1", MousePointer.Default));

        Assert.AreEqual(Of(MousePointer.Hourglass), set["Text3"]);
        Assert.AreEqual(Of(MousePointer.Hourglass), set["Command2"]);
        Assert.AreEqual(Own.Value["Text1"], set["Text1"]);
        CollectionAssert.AreEquivalent(Own.Value, reset);
    }

    // A TextBox's own pointer is its theme's I-beam, kept where no container has a pointer.
    [TestMethod]
    public void ATextBoxWithNoPointerAnywhere_ShowsItsOwnIBeam()
    {
        Assert.AreEqual(Of(MousePointer.IBeam), Own.Value["Text1"]);
        Assert.AreEqual(Of(MousePointer.IBeam), Own.Value["Text3"]);
    }
}
