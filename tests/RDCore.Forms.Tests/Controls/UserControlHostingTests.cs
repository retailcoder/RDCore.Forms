using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using static RDCore.Forms.Tests.Controls.PictureControlTests;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

[TestClass]
public sealed class UserControlHostingTests
{
    private static readonly Color White = Color.FromRgb(255, 255, 255), Red = Color.FromRgb(255, 0, 0);

    // A white form of 300 by 200 pixels.
    private static ComponentInstance Form() =>
        new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, 300d).Set(P.Height, 200d).Set(P.BackColor, FormColor.FromRgb(255, 255, 255));

    // A red UserControl of 120 by 80 pixels, with what is set on it.
    private static ComponentInstance UserControl(string name = "Gauge") =>
        new ComponentInstance(VBClasses.UserControl, name).Set(P.Width, 120d).Set(P.Height, 80d).Set(P.BackColor, FormColor.FromRgb(255, 0, 0));

    // A component placed in a container, in pixels.
    private static ComponentInstance Add(ComponentInstance container, ComponentClass type, string name, double left, double top,
        double width, double height)
    {
        var component = new ComponentInstance(type, name).Set(P.Left, left).Set(P.Top, top).Set(P.Width, width).Set(P.Height, height);
        component.SetContainer(container);
        return component;
    }

    private static VBFormWindow Show(ComponentInstance root)
    {
        var window = new VBFormWindow(new FormDefinition(root));
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        return window;
    }

    [TestMethod]
    public void APlacedUserControl_ShowsItsOwnCopyOfItsUserControlsControls_AtItsPlace()
    {
        var (bounds, showsItsOwn, copies, ownCaptions) = UI.Run(() =>
        {
            var gauge = UserControl();
            Add(gauge, VBClasses.CommandButton, "Command1", 10, 10, 60, 25).Set(P.Caption, "Go");
            var definition = new FormDefinition(gauge);
            var type = UserControlClasses.Of(definition, "Project1");
            var root = Form();
            var gauge1 = Add(root, type, "Gauge1", 20, 30, 120, 80);
            var gauge2 = Add(root, type, "Gauge2", 150, 30, 120, 80);
            var window = Show(root);
            var shown1 = (VBUserControl)window.View.ControlOf(gauge1)!;
            var shown2 = (VBUserControl)window.View.ControlOf(gauge2)!;
            var own = shown1.UserControl.Find("Command1")!;
            var button = shown1.View!.ControlOf(own) as VBCommandButton;
            own.Set(P.Caption, "Stop");
            var result = (shown1.Bounds, button is not null && ReferenceEquals(VBControl.GetInstance(button), own),
                new[] { shown1.UserControl, shown2.UserControl }.Distinct().Count() == 2 && !ReferenceEquals(shown1.UserControl, definition),
                (shown2.UserControl.Find("Command1")!.Get(P.Caption), definition.Find("Command1")!.Get(P.Caption)));
            window.Close();
            return result;
        });

        Assert.AreEqual(new Rect(20, 30, 120, 80), bounds);
        Assert.IsTrue(showsItsOwn);
        Assert.IsTrue(copies);
        Assert.AreEqual(("Go", "Go"), ownCaptions);
    }

    [TestMethod]
    public void ItsUserControlsRoot_TakesThePlacedControlsSize()
    {
        var sizes = UI.Run(() =>
        {
            var type = UserControlClasses.Of(new FormDefinition(UserControl()), "Project1");
            var root = Form();
            var gauge1 = Add(root, type, "Gauge1", 0, 0, 90, 50);
            var window = Show(root);
            var shown = (VBUserControl)window.View.ControlOf(gauge1)!;
            var before = (shown.UserControl.Root.Get(P.Width), shown.UserControl.Root.Get(P.Height));
            gauge1.Set(P.Width, 140d);
            Dispatcher.UIThread.RunJobs();
            var after = (shown.UserControl.Root.Get(P.Width), shown.View!.Bounds.Width);
            window.Close();
            return (before, after);
        });

        Assert.AreEqual((90d, 50d), sizes.before);
        Assert.AreEqual((140d, 140d), sizes.after);
    }

    [TestMethod]
    [DataRow(BackStyle.Opaque, false, DisplayName = "BackStyle 1, windowed")]
    [DataRow(BackStyle.Transparent, false, DisplayName = "BackStyle 0, windowed")]
    [DataRow(BackStyle.Opaque, true, DisplayName = "BackStyle 1, windowless")]
    public void AUserControl_PaintsItsBackColor_WhereItIsOpaque_AndShowsTheFormWhereItIsTransparent(BackStyle backStyle, bool windowless)
    {
        var centre = UI.Run(() =>
        {
            var type = UserControlClasses.Of(new FormDefinition(UserControl().Set(P.BackStyle, backStyle).Set(P.Windowless, windowless)), "Project1");
            var root = Form();
            Add(root, type, "Gauge1", 20, 20, 120, 80);
            var window = Show(root);
            var colour = PixelsAt(window, new PixelPoint(80, 60)).Single();
            window.Close();
            return colour;
        });

        Assert.AreEqual(backStyle == BackStyle.Opaque ? Red : White, centre);
    }

    [TestMethod]
    public void AUserControlWhoseEnabledIsFalse_TakesNoInputInItsControls()
    {
        var enabled = UI.Run(() =>
        {
            var gauge = UserControl().Set(P.Enabled, false);
            Add(gauge, VBClasses.TextBox, "Text1", 10, 10, 60, 19);
            var type = UserControlClasses.Of(new FormDefinition(gauge), "Project1");
            var root = Form();
            var gauge1 = Add(root, type, "Gauge1", 0, 0, 120, 80);
            var window = Show(root);
            var shown = (VBUserControl)window.View.ControlOf(gauge1)!;
            var text = shown.View!.ControlOf(shown.UserControl.Find("Text1")!)!;
            var result = text.IsEffectivelyEnabled;
            window.Close();
            return result;
        });

        Assert.IsFalse(enabled);
    }

    [TestMethod]
    public void AContainerUserControl_HoldsTheControlsTheFormPlacesInIt_FromItsTopLeftCorner()
    {
        var (inHost, place) = UI.Run(() =>
        {
            var type = UserControlClasses.Of(new FormDefinition(UserControl().Set(P.ControlContainer, true)), "Project1");
            var root = Form();
            var panel1 = Add(root, type, "Panel1", 40, 30, 120, 80);
            var command = Add(panel1, VBClasses.CommandButton, "Command1", 8, 6, 60, 25);
            var window = Show(root);
            var shown = (VBUserControl)window.View.ControlOf(panel1)!;
            var button = window.View.ControlOf(command)!;
            var result = (shown.ChildHost.Children.Contains(button), button.TranslatePoint(default, window.View));
            window.Close();
            return result;
        });

        Assert.IsTrue(inHost);
        Assert.AreEqual(new Point(48, 36), place);
    }

    [TestMethod]
    public void AUserControlInvisibleAtRunTime_IsNotShown()
    {
        var shown = UI.Run(() =>
        {
            var type = UserControlClasses.Of(new FormDefinition(UserControl().Set(P.InvisibleAtRuntime, true)), "Project1");
            var root = Form();
            var timer = new ComponentInstance(type, "Ticker1");
            timer.SetContainer(root);
            return new VBFormView(new FormDefinition(root)).ControlOf(timer) is not null;
        });

        Assert.IsFalse(shown);
    }

    [TestMethod]
    public void AUserControlInAUserControl_IsShownWithItsOwnControls()
    {
        var inner = UI.Run(() =>
        {
            var panel = UserControl("Panel");
            Add(panel, VBClasses.Label, "Label1", 4, 4, 60, 15);
            var panelType = UserControlClasses.Of(new FormDefinition(panel), "Project1");
            var gauge = UserControl();
            Add(gauge, panelType, "Panel1", 10, 10, 80, 40);
            var gaugeType = UserControlClasses.Of(new FormDefinition(gauge), "Project1");
            var root = Form();
            var gauge1 = Add(root, gaugeType, "Gauge1", 0, 0, 120, 80);
            var shown = (VBUserControl)new VBFormView(new FormDefinition(root)).ControlOf(gauge1)!;
            var shownPanel = (VBUserControl)shown.View!.ControlOf(shown.UserControl.Find("Panel1")!)!;
            return shownPanel.View?.ControlOf(shownPanel.UserControl.Find("Label1")!) is VBLabel;
        });

        Assert.IsTrue(inner);
    }

    [TestMethod]
    public void AUserControlThatHoldsItself_IsShown_WithAPlaceholderWhereItHoldsItself()
    {
        var (outer, inner, label) = UI.Run(() =>
        {
            var definition = new FormDefinition(UserControl());
            var type = UserControlClasses.Of(definition, "Project1");
            definition.AddControl(type, "Gauge2").Set(P.Visible, true);
            var root = Form();
            var gauge1 = Add(root, type, "Gauge1", 0, 0, 120, 80);
            var shown = (VBUserControl)new VBFormView(new FormDefinition(root)).ControlOf(gauge1)!;
            var held = (VBUserControl)shown.View!.ControlOf(shown.UserControl.Find("Gauge2")!)!;
            var text = held.GetLogicalDescendants().OfType<TextBlock>().Single().Text;
            return (shown.View is not null, held.View is null, text);
        });

        Assert.IsTrue(outer);
        Assert.IsTrue(inner);
        Assert.AreEqual("Project1.Gauge", label);
    }

    [TestMethod]
    public void TabFromAUserControlsControl_GoesToTheFormsNextControl()
    {
        var focused = UI.Run(() =>
        {
            var gauge = UserControl();
            Add(gauge, VBClasses.TextBox, "Text2", 10, 10, 60, 19);
            Add(gauge, VBClasses.TextBox, "Text3", 10, 40, 60, 19).Set(P.TabIndex, (short)1);
            var type = UserControlClasses.Of(new FormDefinition(gauge), "Project1");
            var root = Form();
            Add(root, VBClasses.TextBox, "Text1", 0, 0, 80, 19).Set(P.TabIndex, (short)0);
            var gauge1 = Add(root, type, "Gauge1", 0, 40, 120, 80).Set(P.TabIndex, (short)1);
            Add(root, VBClasses.TextBox, "Text4", 0, 140, 80, 19).Set(P.TabIndex, (short)2);
            var window = Show(root);
            var shown = (VBUserControl)window.View.ControlOf(gauge1)!;
            shown.View!.ControlOf(shown.UserControl.Find("Text2")!)!.Focus();
            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            var at = window.FocusManager?.GetFocusedElement() as Control;
            var name = at is null ? "none" : VBControl.GetInstance(at)?.Name ?? at.GetType().Name;
            window.Close();
            return name;
        });

        Assert.AreEqual("Text4", focused);
    }
}
