using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

[TestClass]
public sealed class PagesAndTabsControlTests
{
    // A form with a MultiPage of two pages, General and More, each holding a TextBox.
    private static (FormDefinition Form, ComponentInstance MultiPage) TwoPages()
    {
        var root = new ComponentInstance(ComponentClasses.Form, "Form1").Set(P.Width, 320d).Set(P.Height, 240d);
        var multiPage = new ComponentInstance(ComponentClasses.MultiPage, "MultiPage1")
            .Set(P.Left, 8d).Set(P.Top, 8d).Set(P.Width, 240d).Set(P.Height, 160d);
        multiPage.SetContainer(root);
        foreach (var (name, caption, text) in new[] { ("Page1", "&General", "Text1"), ("Page2", "More", "Text2") })
        {
            var page = new ComponentInstance(ComponentClasses.Page, name).Set(P.Caption, caption);
            page.SetContainer(multiPage);
            new ComponentInstance(ComponentClasses.TextBox, text).Set(P.Left, 8d).Set(P.Top, 8d).Set(P.Width, 80d).Set(P.Height, 20d).SetContainer(page);
        }

        return (new FormDefinition(root), multiPage);
    }

    private static (RDFormsFormWindow Window, RecordingSink Sink) Show(FormDefinition form)
    {
        var window = new RDFormsFormWindow(form);
        var sink = new RecordingSink();
        RDFormsControl.SetEventSink(window, sink);
        window.Closing += (_, _) => RDFormsControl.SetEventSink(window, null);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        sink.Raised.Clear();
        return (window, sink);
    }

    // The events but the mouse's, which a click brings.
    private static string[] NotTheMouse(RecordingSink sink) => [.. sink.Raised.Where(e => !e.Contains(".Mouse") && !e.Contains("Focus"))];

    // A click in the middle of the tab of a caption.
    private static void ClickTab(TopLevel window, Control control, string caption)
    {
        var header = control.GetVisualDescendants().OfType<Border>().First(b => b.Child is TextBlock t && t.Text?.Replace("_", "") == caption);
        var centre = header.TranslatePoint(new Point(header.Bounds.Width / 2, header.Bounds.Height / 2), window)!.Value;
        window.MouseDown(centre, MouseButton.Left);
        window.MouseUp(centre, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    [TestMethod]
    public void AMultiPage_ShowsItsPagesCaptionsOnItsTabs_AndTheSelectedPagesControlsOnly()
    {
        var (captions, first, second) = UI.Run(() =>
        {
            var (form, multiPage) = TwoPages();
            var (window, _) = Show(form);
            var view = (RDFormsMultiPage)window.View.ControlOf(multiPage)!;
            var text1 = window.View.ControlOf(form.Root.Descendants().First(c => c.Name == "Text1"))!;
            var text2 = window.View.ControlOf(form.Root.Descendants().First(c => c.Name == "Text2"))!;
            var result = (view.TabCaptions.ToArray(), (text1.IsEffectivelyVisible, text2.IsEffectivelyVisible), (false, false));
            multiPage.Set(P.SelectedIndex, 1);
            Dispatcher.UIThread.RunJobs();
            result.Item3 = (text1.IsEffectivelyVisible, text2.IsEffectivelyVisible);
            window.Close();
            return result;
        });

        CollectionAssert.AreEqual(new[] { "General", "More" }, captions);
        Assert.AreEqual((true, false), first);
        Assert.AreEqual((false, true), second);
    }

    [TestMethod]
    public void ClickingATab_SelectsItsPage_AndRaisesChangeThenClick_WithItsIndex()
    {
        var (value, raised, shown) = UI.Run(() =>
        {
            var (form, multiPage) = TwoPages();
            var (window, sink) = Show(form);
            var view = (RDFormsMultiPage)window.View.ControlOf(multiPage)!;
            ClickTab(window, view, "More");
            var result = (multiPage.Get(P.SelectedIndex), NotTheMouse(sink), RDFormsControl.GetInstance(view.ShownPage!)!.Name);
            window.Close();
            return result;
        });

        Assert.AreEqual(1, value);
        Expect.Lines(["MultiPage1.Change", "MultiPage1.Click(Long 1)"], raised);
        Assert.AreEqual("Page2", shown);
    }

    [TestMethod]
    public void ThePressOnATab_RaisesMouseDown_WithTheTabsIndexFirst()
    {
        var raised = UI.Run(() =>
        {
            var (form, multiPage) = TwoPages();
            var (window, sink) = Show(form);
            ClickTab(window, window.View.ControlOf(multiPage)!, "General");
            var result = sink.Raised.Where(e => e.Contains(".MouseDown") || e.Contains(".MouseUp")).ToArray();
            window.Close();
            return result;
        });

        Assert.AreEqual(2, raised.Length);
        StringAssert.StartsWith(raised[0], "MultiPage1.MouseDown(Long 0, Integer 1, Integer 0, ");
        StringAssert.StartsWith(raised[1], "MultiPage1.MouseUp(Long 0, Integer 1, Integer 0, ");
    }

    [TestMethod]
    public void ADisabledPagesTab_SelectsNothing_AndRaisesNoClick()
    {
        var (value, raised) = UI.Run(() =>
        {
            var (form, multiPage) = TwoPages();
            multiPage.Contents[1].Set(P.Enabled, false);
            var (window, sink) = Show(form);
            ClickTab(window, window.View.ControlOf(multiPage)!, "More");
            var result = (multiPage.Get(P.SelectedIndex), sink.Raised.Where(e => !e.Contains("Focus")).ToArray());
            window.Close();
            return result;
        });

        // The press and the release are still the MultiPage's, on the disabled tab's index, and nothing else's.
        Assert.AreEqual(0, value);
        Assert.AreEqual(2, raised.Length, string.Join(Environment.NewLine, raised));
        StringAssert.StartsWith(raised[0], "MultiPage1.MouseDown(Long 1, ");
        StringAssert.StartsWith(raised[1], "MultiPage1.MouseUp(Long 1, ");
    }

    [TestMethod]
    public void AProgramsValue_SelectsThePage_AndRaisesChangeOnce()
    {
        var raised = UI.Run(() =>
        {
            var (form, multiPage) = TwoPages();
            var (window, sink) = Show(form);
            multiPage.Set(P.SelectedIndex, 1);
            multiPage.Set(P.SelectedIndex, 1);
            Dispatcher.UIThread.RunJobs();
            var result = NotTheMouse(sink);
            window.Close();
            return result;
        });

        Expect.Lines(["MultiPage1.Change"], raised);
    }

    [TestMethod]
    public void APageAProgramAdds_GetsItsTab_AndAMultiPageItAdds_ShowsItsTwoPages()
    {
        var (afterAdd, added) = UI.Run(() =>
        {
            var (form, multiPage) = TwoPages();
            var (window, _) = Show(form);
            Pages.Of(form, multiPage).Add(caption: "Third");
            Dispatcher.UIThread.RunJobs();
            var view = (RDFormsMultiPage)window.View.ControlOf(multiPage)!;
            var second = form.AddControl(ComponentClasses.MultiPage, "MultiPage2").Set(P.Visible, true);
            Dispatcher.UIThread.RunJobs();
            var result = (view.TabCaptions.ToArray(), ((RDFormsMultiPage)window.View.ControlOf(second)!).TabCaptions.ToArray());
            window.Close();
            return result;
        });

        CollectionAssert.AreEqual(new[] { "General", "More", "Third" }, afterAdd);
        CollectionAssert.AreEqual(new[] { "Page4", "Page5" }, added);
    }

    [TestMethod]
    public void AHiddenPage_HasNoTab()
    {
        var captions = UI.Run(() =>
        {
            var (form, multiPage) = TwoPages();
            var (window, _) = Show(form);
            multiPage.Contents[0].Set(P.Visible, false);
            Dispatcher.UIThread.RunJobs();
            var result = ((RDFormsMultiPage)window.View.ControlOf(multiPage)!).TabCaptions.ToArray();
            window.Close();
            return result;
        });

        CollectionAssert.AreEqual(new[] { "More" }, captions);
    }

    // TabStrip

    private static ComponentInstance TabStrip() => new(ComponentClasses.TabStrip, "TabStrip1");

    [TestMethod]
    public void ClickingATabStripsTab_SelectsIt_AndRaisesChangeThenClick()
    {
        var (value, raised) = UI.Run(() =>
        {
            var instance = TabStrip();
            var strip = new RDFormsTabStrip(instance);
            var (window, sink) = ControlHost.Show((strip, new Rect(10, 10, 200, 100)));
            ClickTab(window, strip, "Tab2");
            var result = (instance.Get(P.SelectedIndex), NotTheMouse(sink));
            window.Close();
            return result;
        });

        Assert.AreEqual(1, value);
        Expect.Lines(["TabStrip1.Change", "TabStrip1.Click(Long 1)"], raised);
    }

    [TestMethod]
    public void TheClientArea_IsUnderTheTabs_AndTheWholeStrip_WithStyleNone()
    {
        var (tabs, none) = UI.Run(() =>
        {
            var instance = TabStrip();
            var strip = new RDFormsTabStrip(instance);
            var (window, _) = ControlHost.Show((strip, new Rect(10, 10, 200, 100)));
            var withTabs = new Rect(strip.ClientLeft, strip.ClientTop, strip.ClientWidth, strip.ClientHeight);
            instance.Set(P.TabStyle, TabStyle.None);
            Dispatcher.UIThread.RunJobs();
            var withNone = new Rect(strip.ClientLeft, strip.ClientTop, strip.ClientWidth, strip.ClientHeight);
            window.Close();
            return (withTabs, withNone);
        });

        Assert.IsTrue(tabs.Top > 10, $"The client area is {tabs}.");
        Assert.AreEqual(new Rect(1, 1, 198, 98), none);
    }

    [TestMethod]
    public void TheArrowKeys_SelectTheNextTab_AndRaiseClick()
    {
        var (value, raised) = UI.Run(() =>
        {
            var instance = TabStrip();
            var strip = new RDFormsTabStrip(instance);
            var (window, sink) = ControlHost.Show((strip, new Rect(10, 10, 200, 100)));
            strip.Focus();
            Dispatcher.UIThread.RunJobs();
            sink.Raised.Clear();
            window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
            window.KeyRelease(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
            window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
            Dispatcher.UIThread.RunJobs();
            var result = (instance.Get(P.SelectedIndex), sink.Raised.Where(e => !e.Contains(".Key")).ToArray());
            window.Close();
            return result;
        });

        // The last tab is the last: a second arrow goes no further.
        Assert.AreEqual(1, value);
        Expect.Lines(["TabStrip1.Change", "TabStrip1.Click(Long 1)"], raised);
    }

    [TestMethod]
    public void ATabStripsTabs_FollowItsTabs()
    {
        var captions = UI.Run(() =>
        {
            var instance = TabStrip();
            var strip = new RDFormsTabStrip(instance);
            var (window, _) = ControlHost.Show((strip, new Rect(10, 10, 200, 100)));
            var tabs = Tabs.Of(instance);
            tabs.Add(caption: "&Third");
            tabs.Replace(0, tabs[0] with { Visible = false });
            Dispatcher.UIThread.RunJobs();
            var result = strip.TabCaptions.ToArray();
            window.Close();
            return result;
        });

        CollectionAssert.AreEqual(new[] { "Tab2", "Third" }, captions);
    }
}
