using Avalonia;
using Avalonia.Headless;
using Avalonia.Input;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

[TestClass]
public sealed class TextControlTests
{
    [TestMethod]
    public void TextTyped_IsAKeyPressThenAChange_AndTheComponentsText()
    {
        var (text, raised) = UI.Run(() =>
        {
            var instance = new ComponentInstance(VBClasses.TextBox, "Text1");
            var box = new VBTextBox(instance);
            var (window, sink) = ControlHost.Show(box);
            box.Focus();
            window.KeyTextInput("ab");
            window.Close();
            return (instance.Get(P.Text), sink.Raised);
        });

        Assert.AreEqual("ab", text);
        Expect.Lines(new[]
        {
            "Text1.GotFocus",
            "Text1.KeyPress(Integer 97)", "Text1.Change",
            "Text1.KeyPress(Integer 98)", "Text1.Change",
        }, raised);
    }

    [TestMethod]
    public void AKeyPressHandler_ReplacesTheCharacterWithItsKeyAscii_OrCancelsItWithZero()
    {
        var text = UI.Run(() =>
        {
            var instance = new ComponentInstance(VBClasses.TextBox, "Text1");
            var box = new VBTextBox(instance);
            var (window, sink) = ControlHost.Show(box);
            sink.Handler = (_, definition, arguments) =>
            {
                if (ReferenceEquals(definition, VBEvents.KeyPress))
                    arguments[0] = FormValue.FromInteger(arguments[0].AsInteger() == 'x' ? (short)0 : (short)char.ToUpperInvariant((char)arguments[0].AsInteger()));
            };
            box.Focus();
            window.KeyTextInput("axb");
            window.Close();
            return instance.Get(P.Text);
        });

        Assert.AreEqual("AB", text);
    }

    [TestMethod]
    public void TheComponentsTextFromCode_ShowsInTheBox_AndRaisesChange_OnlyWhenItChanges()
    {
        var (shown, raised) = UI.Run(() =>
        {
            var instance = new ComponentInstance(VBClasses.TextBox, "Text1");
            var box = new VBTextBox(instance);
            var (window, sink) = ControlHost.Show(box);
            instance.Set(P.Text, "set by code");
            instance.Set(P.Text, "set by code");
            window.Close();
            return (box.Text, sink.Raised);
        });

        Assert.AreEqual("set by code", shown);
        Expect.Lines(new[] { "Text1.Change" }, raised);
    }

    [TestMethod]
    public void AKeyDown_GivesVBsKeyCode_AndTheShiftMask()
    {
        var raised = UI.Run(() =>
        {
            var box = new VBTextBox(new ComponentInstance(VBClasses.TextBox, "Text1"));
            var (window, sink) = ControlHost.Show(box);
            box.Focus();
            window.KeyPress(Key.Left, RawInputModifiers.Shift, PhysicalKey.ArrowLeft, null);
            window.KeyRelease(Key.Left, RawInputModifiers.Shift, PhysicalKey.ArrowLeft, null);
            window.Close();
            return sink.Raised;
        });

        CollectionAssert.Contains(raised, "Text1.KeyDown(Integer 37, Integer 1)");
        CollectionAssert.Contains(raised, "Text1.KeyUp(Integer 37, Integer 1)");
    }

    [TestMethod]
    public void ALockedBox_IsReadOnly()
    {
        var readOnly = UI.Run(() => new VBTextBox(new ComponentInstance(VBClasses.TextBox, "Text1").Set(P.Locked, true)).IsReadOnly);

        Assert.IsTrue(readOnly);
    }

    [TestMethod]
    public void ALabel_DrawsEveryAmpersand_WithoutUseMnemonic_AndMarksItsKeyWithIt()
    {
        var (with, without) = UI.Run(() =>
        {
            var with = new VBLabel(new ComponentInstance(VBClasses.Label, "Label1").Set(P.Caption, "E&xit"));
            var without = new VBLabel(new ComponentInstance(VBClasses.Label, "Label2").Set(P.Caption, "E&xit").Set(P.UseMnemonic, false));
            return (with.DrawnText, without.DrawnText);
        });

        Assert.AreEqual("E_xit", with);
        Assert.AreEqual("E&xit", without);
    }

    [TestMethod]
    public void ALabel_TakesNoFocus_AndRaisesClickOnlyWhereTheReleaseIsOverIt()
    {
        var raised = UI.Run(() =>
        {
            var (window, sink) = ControlHost.Show(new VBLabel(new ComponentInstance(VBClasses.Label, "Label1")));
            window.MouseDown(new Point(20, 10), MouseButton.Left);
            window.MouseUp(new Point(20, 10), MouseButton.Left);
            // Far from the first press, so that it is no double click's second.
            window.MouseDown(new Point(100, 25), MouseButton.Left);
            window.MouseUp(new Point(300, 200), MouseButton.Left);
            window.Close();
            return sink.WithoutMoves;
        });

        Expect.Lines(new[]
        {
            "Label1.MouseDown(Integer 1, Integer 0, Single 150, Single 75)",
            "Label1.MouseUp(Integer 1, Integer 0, Single 150, Single 75)",
            "Label1.Click",
            "Label1.MouseDown(Integer 1, Integer 0, Single 1350, Single 300)",
            "Label1.MouseUp(Integer 1, Integer 0, Single 4350, Single 2925)",
        }, raised);
    }
}
