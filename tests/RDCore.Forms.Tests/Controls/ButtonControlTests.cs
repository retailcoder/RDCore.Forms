using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Headless;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

[TestClass]
public sealed class ButtonControlTests
{
    [TestMethod]
    public void ACommandButtonsClick_ComesBeforeItsMouseUp_WithTheMouseInTwipsFromItsOrigin()
    {
        var raised = UI.Run(() =>
        {
            var (window, sink) = ControlHost.Show(new VBCommandButton(new ComponentInstance(VBClasses.CommandButton, "Command1")));
            window.MouseDown(new Point(20, 10), MouseButton.Left);
            window.MouseUp(new Point(20, 10), MouseButton.Left);
            window.Close();
            return sink.WithoutMoves;
        });

        Expect.Lines(new[]
        {
            "Command1.GotFocus",
            "Command1.MouseDown(Integer 1, Integer 0, Single 150, Single 75)",
            "Command1.Click",
            "Command1.MouseUp(Integer 1, Integer 0, Single 150, Single 75)",
        }, raised);
    }

    [TestMethod]
    public void ACaptionsAmpersand_MarksItsAccessKey_AndADoubledOneShowsOne()
    {
        var (key, doubled) = UI.Run(() =>
        {
            var command = new VBCommandButton(new ComponentInstance(VBClasses.CommandButton, "Command1").Set(P.Caption, "E&xit"));
            var key = VBCaption.Present((string)command.Content!);
            var doubled = VBCaption.Present("Tom && Jerry");
            return ((key.GetType(), key.Text), (doubled.GetType(), doubled.Text));
        });

        Assert.AreEqual((typeof(AccessText), "E_xit"), key);
        Assert.AreEqual((typeof(TextBlock), "Tom & Jerry"), doubled);
    }

    [TestMethod]
    public void ACaptionWithAnUnderscoreBeforeItsKey_IsPlainText_SoThatItAnswersToNoKey()
    {
        var parsed = VBCaption.Parse("a_b &c");

        Assert.AreEqual("a_b c", parsed.Shown);
        Assert.AreEqual(4, parsed.AccessKey);
        Assert.IsFalse(parsed.FitsAccessText);
        Assert.AreEqual("Trail", VBCaption.AsShown("Trail&"));
    }

    [TestMethod]
    public void ACheckBoxClicked_TakesTheNewValue_AndRaisesClick_ClickBeforeMouseUp()
    {
        var (value, raised) = UI.Run(() =>
        {
            var instance = new ComponentInstance(VBClasses.CheckBox, "Check1");
            var (window, sink) = ControlHost.Show(new VBCheckBox(instance));
            window.MouseDown(new Point(20, 10), MouseButton.Left);
            window.MouseUp(new Point(20, 10), MouseButton.Left);
            window.Close();
            return (instance.Get(P.CheckBoxValue), sink.WithoutMoves);
        });

        Assert.AreEqual(CheckBoxValue.Checked, value);
        Expect.Lines(new[]
        {
            "Check1.GotFocus",
            "Check1.MouseDown(Integer 1, Integer 0, Single 150, Single 75)",
            "Check1.Click",
            "Check1.MouseUp(Integer 1, Integer 0, Single 150, Single 75)",
        }, raised);
    }

    [TestMethod]
    public void ACheckBoxsValueFromCode_RaisesClick_OnlyWhenItChanges_AndAClickClearsAGrayedBox()
    {
        var (raised, shown, afterClick) = UI.Run(() =>
        {
            var instance = new ComponentInstance(VBClasses.CheckBox, "Check1");
            var check = new VBCheckBox(instance);
            var (window, sink) = ControlHost.Show(check);
            instance.Set(P.CheckBoxValue, CheckBoxValue.Grayed);
            instance.Set(P.CheckBoxValue, CheckBoxValue.Grayed);
            var shown = check.IsChecked;
            window.MouseDown(new Point(20, 10), MouseButton.Left);
            window.MouseUp(new Point(20, 10), MouseButton.Left);
            window.Close();
            return (sink.Raised.Count(e => e == "Check1.Click"), shown, instance.Get(P.CheckBoxValue));
        });

        Assert.AreEqual(2, raised);
        Assert.IsNull(shown);
        Assert.AreEqual(CheckBoxValue.Unchecked, afterClick);
    }

    [TestMethod]
    public void AnOptionButtonSelectedFromCode_ClearsTheOtherInItsContainer_AndRaisesItsOwnClickOnly()
    {
        var (first, second, raised) = UI.Run(() =>
        {
            var one = new ComponentInstance(VBClasses.OptionButton, "Option1").Set(P.OptionButtonValue, true);
            var two = new ComponentInstance(VBClasses.OptionButton, "Option2");
            var (window, sink) = ControlHost.Show((new VBOptionButton(one), new Rect(10, 5, 120, 20)), (new VBOptionButton(two), new Rect(10, 30, 120, 20)));
            two.Set(P.OptionButtonValue, true);
            two.Set(P.OptionButtonValue, true);
            window.Close();
            return (one.Get(P.OptionButtonValue), two.Get(P.OptionButtonValue), sink.Raised);
        });

        Assert.IsFalse(first);
        Assert.IsTrue(second);
        Expect.Lines(new[] { "Option2.Click" }, raised);
    }

    [TestMethod]
    public void TheSelectedOptionButtonClearedFromCode_LeavesNoneSelected_AndRaisesNoClick()
    {
        var (first, second, raised) = UI.Run(() =>
        {
            var one = new ComponentInstance(VBClasses.OptionButton, "Option1").Set(P.OptionButtonValue, true);
            var two = new ComponentInstance(VBClasses.OptionButton, "Option2");
            var (window, sink) = ControlHost.Show((new VBOptionButton(one), new Rect(10, 5, 120, 20)), (new VBOptionButton(two), new Rect(10, 30, 120, 20)));
            one.Set(P.OptionButtonValue, false);
            window.Close();
            return (one.Get(P.OptionButtonValue), two.Get(P.OptionButtonValue), sink.Raised);
        });

        Assert.IsFalse(first);
        Assert.IsFalse(second);
        Assert.IsEmpty(raised);
    }

    [TestMethod]
    public void AnOptionButtonFocusedFromCode_IsSelected_AndItsClickComesAfterItsGotFocus()
    {
        var raised = UI.Run(() =>
        {
            var one = new ComponentInstance(VBClasses.OptionButton, "Option1");
            var option = new VBOptionButton(one);
            var (window, sink) = ControlHost.Show(option);
            option.Focus(NavigationMethod.Unspecified);
            window.Close();
            return sink.Raised;
        });

        Expect.Lines(new[] { "Option1.GotFocus", "Option1.Click" }, raised);
    }
}
