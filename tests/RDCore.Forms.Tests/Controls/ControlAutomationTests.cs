using System.Globalization;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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

// The controls as UI Automation sees and uses them, as it sees the Win32 controls VB6 draws.
[TestClass]
public sealed class ControlAutomationTests
{
    private static readonly string Root = OperatingSystem.IsWindows() ? @"C:\tree" : "/tree";

    private static string In(params string[] parts) => Path.Combine([Root, .. parts]);

    private static IFileSystem Tree()
    {
        var files = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [In("a.txt")] = new(""),
            [In("b.txt")] = new(""),
            [In("c.txt")] = new(""),
            [In("d.txt")] = new(""),
            [In("one")] = new MockDirectoryData(),
            [In("two")] = new MockDirectoryData(),
            [In("three")] = new MockDirectoryData(),
        }, Root);
        return OperatingSystem.IsWindows() ? files : new RootDrives(files);
    }

    private static ListBox List(string kind) => kind switch
    {
        "ListBox" => new RDFormsListBox(new ComponentInstance(ComponentClasses.ListBox, "List1").Set(P.List, ["a", "b", "c", "d"])),
        "DirListBox" => new RDFormsDirListBox(new ComponentInstance(ComponentClasses.DirListBox, "List1"), Tree()),
        _ => new RDFormsFileListBox(new ComponentInstance(ComponentClasses.FileListBox, "List1"), Tree()),
    };

    private static List<string> Clicks(RecordingSink sink)
    {
        var clicks = sink.Raised.Where(e => e == "List1.Click").ToList();
        sink.Raised.Clear();
        return clicks;
    }

    // A DirListBox's items 0 and 1 are the root and the folder shown, which is selected; 2 to 4 are the folders it holds.
    [TestMethod]
    [DataRow("ListBox")]
    [DataRow("DirListBox")]
    [DataRow("FileListBox")]
    public void UIAutomationSelectingAnItem_RaisesTheClickAMousePickRaises_OnceForAChange_AndNoneForTheItemSelected(string kind)
    {
        var (picked, selected, again, index) = UI.Run(() =>
        {
            var list = List(kind);
            var (window, sink) = ControlHost.Show((list, new Rect(10, 5, 200, 150)));
            var centre = list.ContainerFromIndex(2)!.TranslatePoint(new Point(20, 5), window)!.Value;
            window.MouseDown(centre, MouseButton.Left);
            window.MouseUp(centre, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            var picked = Clicks(sink);
            var item = ControlAutomationPeer.CreatePeerForElement(list.ContainerFromIndex(3)!).GetProvider<ISelectionItemProvider>()!;
            item.Select();
            Dispatcher.UIThread.RunJobs();
            var selected = Clicks(sink);
            item.Select();
            Dispatcher.UIThread.RunJobs();
            var again = Clicks(sink);
            var index = list.SelectedIndex;
            window.Close();
            return (picked, selected, again, index);
        });

        Expect.Lines(["List1.Click"], picked);
        Expect.Lines(picked, selected);
        Assert.IsEmpty(again, "the item selected again is no change");
        Assert.AreEqual(3, index);
    }

    [TestMethod]
    [DataRow("Label", AutomationControlType.Text)]
    [DataRow("Frame", AutomationControlType.Group)]
    [DataRow("PictureBox", AutomationControlType.Image)]
    [DataRow("Image", AutomationControlType.Image)]
    public void AControl_IsTheUIAutomationControlTypeOfItsWin32Control(string kind, AutomationControlType expected)
    {
        var type = UI.Run(() =>
        {
            Control control = kind switch
            {
                "Label" => new RDFormsLabel(new ComponentInstance(ComponentClasses.Label, "Label1")),
                "Frame" => new RDFormsFrame(new ComponentInstance(ComponentClasses.Frame, "Frame1")),
                "PictureBox" => new RDFormsPictureBox(new ComponentInstance(ComponentClasses.PictureBox, "Picture1")),
                _ => new RDFormsImage(new ComponentInstance(ComponentClasses.Image, "Image1")),
            };
            return ControlAutomationPeer.CreatePeerForElement(control).GetAutomationControlType();
        });

        Assert.AreEqual(expected, type);
    }

    [TestMethod]
    [DataRow("ComboBox", "en-CA", "Open")]
    [DataRow("ComboBox", "fr-CA", "Ouvrir")]
    [DataRow("DriveListBox", "en-CA", "Open")]
    [DataRow("DriveListBox", "fr-CA", "Ouvrir")]
    public void ADropDownsButton_IsNamedOpen_InTheUsersLanguage(string kind, string culture, string expected)
    {
        var name = UI.Run(() =>
        {
            var was = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentUICulture = new CultureInfo(culture);
            try
            {
                Control combo = kind == "ComboBox"
                    ? new RDFormsComboBox(new ComponentInstance(ComponentClasses.ComboBox, "Combo1"))
                    : new RDFormsDriveListBox(new ComponentInstance(ComponentClasses.DriveListBox, "Drive1"), Tree());
                var (window, _) = ControlHost.Show(combo);
                var button = combo.GetVisualDescendants().OfType<ToggleButton>().Single();
                var name = ControlAutomationPeer.CreatePeerForElement(button).GetName();
                window.Close();
                return name;
            }
            finally
            {
                CultureInfo.CurrentUICulture = was;
            }
        });

        Assert.AreEqual(expected, name);
    }

    // On a form, with the focus on a CommandButton, as a program sends a Win32 combo box CB_SHOWDROPDOWN: the list drops, and no item
    // and no control takes the focus. A ComboBox raises DropDown, as it does whenever its list drops.
    [TestMethod]
    [DataRow("DriveListBox", ComboBoxStyle.DropdownList, new string[0])]
    [DataRow("ComboBox", ComboBoxStyle.DropdownList, new[] { "Combo1.DropDown" })]
    [DataRow("ComboBox", ComboBoxStyle.DropdownCombo, new[] { "Combo1.DropDown" })]
    public void UIAutomationExpandingADropDown_AndCollapsingIt_LeavesTheFocusWhereItIs(string kind, ComboBoxStyle style, string[] expected)
    {
        var (expanded, collapsed, focused, raised) = UI.Run(() =>
        {
            var root = new ComponentInstance(ComponentClasses.Form, "Form1").Set(P.Width, 300d).Set(P.Height, 200d);
            var button = new ComponentInstance(ComponentClasses.CommandButton, "Command1")
                .Set(P.Left, 4d).Set(P.Top, 4d).Set(P.Width, 100d).Set(P.Height, 24d);
            button.SetContainer(root);
            var drop = (kind == "ComboBox"
                    ? new ComponentInstance(ComponentClasses.ComboBox, "Combo1").Set(P.ComboBoxStyle, style).Set(P.List, ["a", "b", "c"])
                    : new ComponentInstance(ComponentClasses.DriveListBox, "Drive1"))
                .Set(P.Left, 4d).Set(P.Top, 40d).Set(P.Width, 120d).Set(P.Height, 21d);
            drop.SetContainer(root);
            var sink = new RecordingSink();
            var window = new RDFormsFormWindow(new FormDefinition(root));
            RDFormsControl.SetEventSink(window, sink);
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var combo = (ComboBox)window.View.ControlOf(drop)!;
            // An item selected is the one the list would focus as it opens.
            combo.SelectedIndex = 0;
            window.View.ControlOf(button)!.Focus();
            Dispatcher.UIThread.RunJobs();
            sink.Raised.Clear();
            var peer = ControlAutomationPeer.CreatePeerForElement(combo).GetProvider<IExpandCollapseProvider>()!;
            peer.Expand();
            Dispatcher.UIThread.RunJobs();
            var expanded = peer.ExpandCollapseState;
            peer.Collapse();
            Dispatcher.UIThread.RunJobs();
            var collapsed = peer.ExpandCollapseState;
            var focused = window.View.ControlOf(button)!.IsFocused;
            var raised = sink.Raised.Where(e => !e.EndsWith(".Paint")).ToList();
            RDFormsControl.SetEventSink(window, null);
            window.Close();
            return (expanded, collapsed, focused, raised);
        });

        Assert.AreEqual(ExpandCollapseState.Expanded, expanded);
        Assert.AreEqual(ExpandCollapseState.Collapsed, collapsed);
        Assert.IsTrue(focused, "the CommandButton keeps the focus");
        Expect.Lines(expected, raised);
    }

    [TestMethod]
    public void UIAutomation_DoesNotExpandADisabledDropDown()
    {
        var (refused, open) = UI.Run(() =>
        {
            var list = new RDFormsDriveListBox(new ComponentInstance(ComponentClasses.DriveListBox, "Drive1").Set(P.Enabled, false), Tree());
            var (window, _) = ControlHost.Show(list);
            var peer = ControlAutomationPeer.CreatePeerForElement(list).GetProvider<IExpandCollapseProvider>()!;
            var refused = Assert.ThrowsExactly<ElementNotEnabledException>(peer.Expand) is not null;
            var open = list.IsDropDownOpen;
            window.Close();
            return (refused, open);
        });

        Assert.IsTrue(refused);
        Assert.IsFalse(open);
    }
}
