using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Menus;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using RDCore.Forms.Tests.Controls;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Rendering;

[TestClass]
public sealed class PopupMenuTests
{
    private static ComponentInstance Menu(ComponentInstance container, string name, string caption)
    {
        var menu = new ComponentInstance(VBClasses.Menu, name).Set(P.Caption, caption);
        menu.SetContainer(container);
        return menu;
    }

    // A form that keeps a hidden top-level menu to pop up: mnuEdit holds Cut, Copy and More, which holds Paste. mnuEmpty holds none.
    private static ComponentInstance Probe()
    {
        var root = new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, 4500d).Set(P.Height, 3000d);
        var edit = Menu(root, "mnuEdit", "&Edit").Set(P.Visible, false);
        Menu(edit, "mnuCut", "Cu&t");
        Menu(edit, "mnuCopy", "&Copy");
        var more = Menu(edit, "mnuMore", "&More");
        Menu(more, "mnuPaste", "&Paste");
        Menu(root, "mnuEmpty", "E&mpty").Set(P.Visible, false);
        return root;
    }

    private static VBFormWindow Open(ComponentInstance root, RecordingSink sink)
    {
        var window = new VBFormWindow(new FormDefinition(root));
        VBControl.SetEventSink(window, sink);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        sink.Raised.Clear();
        return window;
    }

    private static FormMenuItem Model(VBFormWindow window, string name) =>
        window.MenuBar.Menu.AllItems.Single(item => item.Component.Name == name);

    private static ContextMenu? Popup(VBFormWindow window) => window.PoppedUpMenu is { IsOpen: true } menu ? menu : null;

    private static MenuItem Shown(ContextMenu popup, string name) =>
        popup.GetLogicalDescendants().OfType<MenuItem>().Single(m => VBControl.GetInstance(m)?.Name == name);

    private static List<string> Clicks(RecordingSink sink) => [.. sink.Raised.Where(e => e.EndsWith(".Click", StringComparison.Ordinal))];

    [TestMethod]
    public void AShownForm_IsItsRootsMenuHost_UntilItCloses()
    {
        var (shown, closed) = UI.Run(() =>
        {
            var root = Probe();
            var window = Open(root, new RecordingSink());
            var host = MenuHosts.Of(root);
            window.Close();
            return (ReferenceEquals(host, window), MenuHosts.Of(root));
        });

        Assert.IsTrue(shown);
        Assert.IsNull(closed);
    }

    [TestMethod]
    public void PopupMenu_ShowsAHiddenMenusItems_AndRaisesItsClickAsItOpens()
    {
        var (raised, items, pending) = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Probe(), sink);
            var task = window.PopupMenu(Model(window, "mnuEdit"));
            Dispatcher.UIThread.RunJobs();
            var popup = Popup(window)!;
            var result = (Clicks(sink), popup.Items.OfType<MenuItem>().Select(m => VBControl.GetInstance(m)!.Name).ToList(), !task.IsCompleted);
            popup.Close();
            window.Close();
            return result;
        });

        Expect.Lines(["mnuEdit.Click"], raised);
        CollectionAssert.AreEqual(new[] { "mnuCut", "mnuCopy", "mnuMore" }, items);
        Assert.IsTrue(pending);
    }

    [TestMethod]
    public void AnItemChosenFromThePopup_RaisesItsClick_ThenThePopupMenusTaskCompletes()
    {
        var (raised, completed, closed) = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Probe(), sink);
            var task = window.PopupMenu(Model(window, "mnuEdit"));
            Dispatcher.UIThread.RunJobs();
            var popup = Popup(window)!;
            sink.Raised.Clear();
            var completedInClick = true;
            sink.Handler = (_, _, _) => completedInClick = task.IsCompleted;
            // A click on the item raises its Click, then Avalonia's menu handler closes the menu.
            Shown(popup, "mnuCopy").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            popup.Close();
            Dispatcher.UIThread.RunJobs();
            var result = (Clicks(sink), !completedInClick && task.IsCompleted, Popup(window) is null);
            window.Close();
            return result;
        });

        Expect.Lines(["mnuCopy.Click"], raised);
        Assert.IsTrue(completed);
        Assert.IsTrue(closed);
    }

    [TestMethod]
    public void AMenuThatHoldsNoItems_PopsUpNothing_AndRaisesNothing()
    {
        var (raised, completed, popup) = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Probe(), sink);
            var task = window.PopupMenu(Model(window, "mnuEmpty"));
            Dispatcher.UIThread.RunJobs();
            var result = (sink.Raised.ToList(), task.IsCompleted, Popup(window));
            window.Close();
            return result;
        });

        Assert.IsEmpty(raised);
        Assert.IsTrue(completed);
        Assert.IsNull(popup);
    }

    [TestMethod]
    public void AMenuOfAnotherForm_IsRefused()
    {
        var thrown = UI.Run(() =>
        {
            var window = Open(Probe(), new RecordingSink());
            var other = FormMenuItem.Of(Probe().Contents[0]);
            try
            {
                _ = window.PopupMenu(other);
                return null;
            }
            catch (ArgumentException e)
            {
                return e.ParamName;
            }
            finally
            {
                window.Close();
            }
        });

        Assert.AreEqual("menu", thrown);
    }

    [TestMethod]
    [DataRow(PopupMenuAlignment.Left, PopupGravity.BottomRight)]
    [DataRow(PopupMenuAlignment.Center, PopupGravity.Bottom)]
    [DataRow(PopupMenuAlignment.Right, PopupGravity.BottomLeft)]
    public void ThePoint_IsInTheRootsScale_AndTheAlignmentSaysWhereTheMenuGoes(PopupMenuAlignment alignment, PopupGravity gravity)
    {
        var (at, placed) = UI.Run(() =>
        {
            var root = Probe().Set(P.ScaleMode, ScaleMode.Pixel);
            var window = Open(root, new RecordingSink());
            _ = window.PopupMenu(Model(window, "mnuEdit"), 40, 25, alignment);
            Dispatcher.UIThread.RunJobs();
            var popup = Popup(window)!;
            var result = (popup.PlacementRect!.Value.Position, popup.PlacementGravity);
            popup.Close();
            window.Close();
            return result;
        });

        Assert.AreEqual(new Point(40, 25), at);
        Assert.AreEqual(gravity, placed);
    }

    [TestMethod]
    public void ACoordinateNotGiven_IsThePointers()
    {
        var at = UI.Run(() =>
        {
            var window = Open(Probe(), new RecordingSink());
            window.MouseMove(new Point(120, 70));
            Dispatcher.UIThread.RunJobs();
            _ = window.PopupMenu(Model(window, "mnuEdit"), y: 600);
            Dispatcher.UIThread.RunJobs();
            var popup = Popup(window)!;
            var result = popup.PlacementRect!.Value.Position;
            popup.Close();
            window.Close();
            return result;
        });

        Assert.AreEqual(new Point(120, 40), at);
    }

    [TestMethod]
    public void TheBoldItem_IsShownInBold_AtAnyDepth()
    {
        var weights = UI.Run(() =>
        {
            var window = Open(Probe(), new RecordingSink());
            _ = window.PopupMenu(Model(window, "mnuEdit"), boldItem: Model(window, "mnuPaste"));
            Dispatcher.UIThread.RunJobs();
            var popup = Popup(window)!;
            var result = new[] { "mnuCut", "mnuPaste" }.Select(name => Shown(popup, name).FontWeight).ToList();
            popup.Close();
            window.Close();
            return result;
        });

        Assert.AreNotEqual(FontWeight.Bold, weights[0]);
        Assert.AreEqual(FontWeight.Bold, weights[1]);
    }

    [TestMethod]
    public void ARightReleaseOverTheForm_PopsUpItsContextMenu_AfterItsMouseUpAndClick()
    {
        var (raised, at) = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var root = Probe().Set(P.ContextMenu, "MNUEDIT");
            var window = Open(root, sink);
            window.MouseDown(new Point(100, 60), MouseButton.Right);
            window.MouseUp(new Point(100, 60), MouseButton.Right);
            Dispatcher.UIThread.RunJobs();
            var popup = Popup(window);
            var result = (sink.WithoutMoves.ToList(), popup?.PlacementRect?.Position);
            popup?.Close();
            window.Close();
            return result;
        });

        Expect.Lines(["Form1.MouseDown(Integer 2, Integer 0, Single 1500, Single 900)",
            "Form1.MouseUp(Integer 2, Integer 0, Single 1500, Single 900)", "Form1.Click", "mnuEdit.Click"], raised);
        Assert.AreEqual(new Point(100, 60), at);
    }

    [TestMethod]
    public void AControlsContextMenu_IsItsOwn_AndTheLeftButtonPopsUpNone()
    {
        var (right, left) = UI.Run(() =>
        {
            var root = Probe().Set(P.ContextMenu, "mnuEmpty");
            var picture = new ComponentInstance(VBClasses.PictureBox, "Picture1").Set(P.Width, 1500d).Set(P.Height, 1500d)
                .Set(P.ContextMenu, "mnuEdit");
            picture.SetContainer(root);
            var window = Open(root, new RecordingSink());
            window.MouseDown(new Point(50, 50), MouseButton.Left);
            window.MouseUp(new Point(50, 50), MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            var afterLeft = Popup(window) is not null;
            window.MouseDown(new Point(50, 50), MouseButton.Right);
            window.MouseUp(new Point(50, 50), MouseButton.Right);
            Dispatcher.UIThread.RunJobs();
            var popup = Popup(window);
            var result = (popup?.Items.OfType<MenuItem>().Select(m => VBControl.GetInstance(m)!.Name).FirstOrDefault(), afterLeft);
            popup?.Close();
            window.Close();
            return result;
        });

        Assert.AreEqual("mnuCut", right);
        Assert.IsFalse(left);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("mnuNone")]
    public void AContextMenuThatNamesNoMenu_PopsUpNone(string name)
    {
        var popup = UI.Run(() =>
        {
            var window = Open(Probe().Set(P.ContextMenu, name), new RecordingSink());
            window.MouseDown(new Point(100, 60), MouseButton.Right);
            window.MouseUp(new Point(100, 60), MouseButton.Right);
            Dispatcher.UIThread.RunJobs();
            var result = Popup(window) is not null;
            window.Close();
            return result;
        });

        Assert.IsFalse(popup);
    }

    [TestMethod]
    public void AnItemsPicture_IsShownBesideItsCaption_AndFollowsItsComponent()
    {
        var (shown, cleared, refused) = UI.Run(() =>
        {
            var root = Probe();
            FormMenuItem.Of(root.Contents[0]).Visible = true;
            var window = Open(root, new RecordingSink());
            var cut = window.MenuBar.GetLogicalDescendants().OfType<MenuItem>().Single(m => VBControl.GetInstance(m)?.Name == "mnuCut");
            Model(window, "mnuCut").Picture = PictureControlTests.Png(16, 16, [.. Enumerable.Repeat(0xFFFF0000u, 256)]);
            var picture = (cut.Icon as Image)?.Source?.Size;
            Model(window, "mnuCut").Picture = null;
            var none = cut.Icon;
            Model(window, "mnuCut").Picture = [1, 2, 3];
            var result = (picture, none, cut.Icon);
            window.Close();
            return result;
        });

        Assert.AreEqual(new Size(16, 16), shown);
        Assert.IsNull(cleared);
        Assert.IsNull(refused);
    }
}
