using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using static RDCore.Forms.Tests.Model.TestPictures;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

[TestClass]
public sealed class ItemIconTests
{
    private static readonly Color Red = Color.FromRgb(255, 0, 0);

    // A 16x16 opaque red PNG.
    private static byte[] RedIcon() => PictureControlTests.Png(16, 16, [.. Enumerable.Repeat(0xFFFF0000u, 256)]);

    // A list's items, ItemData and ItemIcon, the icon given as its index in the pictures, or -1 for none.
    private static (string[] Items, int[] Data, int[] Icons) Read(ComponentInstance instance, byte[][] pictures) =>
        ([.. instance.Get(P.List)], [.. instance.Get(P.ItemData)],
         [.. instance.Get(P.ItemIcon).Select(icon => icon is null ? -1 : Array.FindIndex(pictures, p => ReferenceEquals(p, icon)))]);

    // The pixel at the centre of the icon square of an item's view, or of the first one in the control.
    private static Color IconPixel(Window window, Visual within)
    {
        var view = within.GetVisualDescendants().OfType<Control>().First(c => c.GetType().Name == "ItemView");
        var side = view.Bounds.Height;
        var centre = view.TranslatePoint(new Point(side / 2, side / 2), window)!.Value;
        return PictureControlTests.PixelsAt(window, new PixelPoint((int)centre.X, (int)centre.Y))[0];
    }

    [TestMethod]
    [DataRow("VB.ListBox")]
    [DataRow("VB.ComboBox")]
    public void AddItemRemoveItemAndClear_KeepItemDataAndItemIconInStepWithTheItems(string className)
    {
        byte[][] pictures = [Gif1x1, Bitmap2x2];
        var (added, removed, cleared, data, icon) = UI.Run(() =>
        {
            var isList = className == "VB.ListBox";
            var instance = new ComponentInstance(isList ? VBClasses.ListBox : VBClasses.ComboBox, "List1")
                .Set(P.List, ["b", "c"]).Set(P.ItemData, [7, 8]).Set(P.ItemIcon, [pictures[0], pictures[1]]);
            Control control = isList ? new VBListBox(instance) : new VBComboBox(instance);
            void AddItem(string item, int index) { if (control is VBListBox l) l.AddItem(item, index); else ((VBComboBox)control).AddItem(item, index); }
            void RemoveItem(int index) { if (control is VBListBox l) l.RemoveItem(index); else ((VBComboBox)control).RemoveItem(index); }
            void Clear() { if (control is VBListBox l) l.Clear(); else ((VBComboBox)control).Clear(); }
            var (window, _) = ControlHost.Show(control);
            AddItem("a", 0);
            var added = Read(instance, pictures);
            RemoveItem(1);
            var removed = Read(instance, pictures);
            var (data, icon) = control is VBListBox list ? (list.GetItemData(1), list.GetItemIcon(1)) : (((VBComboBox)control).GetItemData(1), ((VBComboBox)control).GetItemIcon(1));
            Clear();
            window.Close();
            return (added, removed, Read(instance, pictures), data, icon);
        });

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, added.Items);
        CollectionAssert.AreEqual(new[] { 0, 7, 8 }, added.Data);
        CollectionAssert.AreEqual(new[] { -1, 0, 1 }, added.Icons);
        CollectionAssert.AreEqual(new[] { "a", "c" }, removed.Items);
        CollectionAssert.AreEqual(new[] { 0, 8 }, removed.Data);
        CollectionAssert.AreEqual(new[] { -1, 1 }, removed.Icons);
        Assert.AreEqual(8, data);
        Assert.AreSame(pictures[1], icon);
        Assert.AreEqual(0, cleared.Items.Length + cleared.Data.Length + cleared.Icons.Length);
    }

    [TestMethod]
    public void AnItemsDataAndIcon_AreSetAndRead_WithinTheItems_AndAnIconIsAPictureTheInspectionAccepts()
    {
        var (data, icon, none, outside, invalid, unchanged) = UI.Run(() =>
        {
            // ItemData shorter than the items: the rest are 0.
            var instance = new ComponentInstance(VBClasses.ListBox, "List1").Set(P.List, ["a", "b"]).Set(P.ItemData, [5]);
            var list = new VBListBox(instance);
            list.SetItemData(1, -3);
            list.SetItemIcon(1, Gif1x1);
            var icon = list.GetItemIcon(1);
            list.SetItemIcon(1, []);
            return (instance.Get(P.ItemData).ToArray(), icon, list.GetItemIcon(1),
                new[]
                {
                    Assert.ThrowsExactly<FormErrorException>(() => list.GetItemData(2)).Error.Number,
                    Assert.ThrowsExactly<FormErrorException>(() => list.SetItemData(-1, 0)).Error.Number,
                    Assert.ThrowsExactly<FormErrorException>(() => list.GetItemIcon(2)).Error.Number,
                    Assert.ThrowsExactly<FormErrorException>(() => list.SetItemIcon(2, Gif1x1)).Error.Number,
                },
                Assert.ThrowsExactly<FormErrorException>(() => list.SetItemIcon(0, Hex("89504E47"))).Error.Number,
                instance.Get(P.ItemIcon).ToArray());
        });

        CollectionAssert.AreEqual(new[] { 5, -3 }, data);
        Assert.AreSame(Gif1x1, icon);
        Assert.IsNull(none);
        CollectionAssert.AreEqual(new[] { 381, 381, 381, 381 }, outside);
        Assert.AreEqual(481, invalid);
        CollectionAssert.AreEqual(new byte[]?[] { null, null }, unchanged);
    }

    [TestMethod]
    public void AListBoxItemsIcon_IsDrawnBeforeItsText_AndEveryItemsTextLinesUp_AndWithNoIconsTheItemsAreText()
    {
        var (pixel, contents, plain) = UI.Run(() =>
        {
            var list = new VBListBox(new ComponentInstance(VBClasses.ListBox, "List1").Set(P.List, ["red", "plain"]).Set(P.ItemIcon, [RedIcon()]));
            var bare = new VBListBox(new ComponentInstance(VBClasses.ListBox, "List2").Set(P.List, ["one"]));
            var (window, _) = ControlHost.Show((list, new Rect(10, 5, 120, 80)), (bare, new Rect(150, 5, 120, 80)));
            var pixel = IconPixel(window, list.ContainerFromIndex(0)!);
            var contents = new[] { list.ContainerFromIndex(0), list.ContainerFromIndex(1) }.Select(c => ((ContentControl)c!).Content?.GetType().Name).ToArray();
            var plain = ((ContentControl)bare.ContainerFromIndex(0)!).Content;
            window.Close();
            return (pixel, contents, plain);
        });

        Assert.AreEqual(Red, pixel);
        CollectionAssert.AreEqual(new[] { "ItemView", "ItemView" }, contents);
        Assert.AreEqual("one", plain);
    }

    [TestMethod]
    public void ADropdownListsBox_DrawsItsSelectedItemsIcon()
    {
        var pixel = UI.Run(() =>
        {
            var instance = new ComponentInstance(VBClasses.ComboBox, "Combo1").Set(P.List, ["plain", "red"])
                .Set(P.ComboBoxStyle, ComboBoxStyle.DropdownList).Set(P.ItemIcon, [null, RedIcon()]);
            var combo = new VBComboBox(instance);
            var (window, _) = ControlHost.Show(combo);
            combo.ListIndex = 1;
            Dispatcher.UIThread.RunJobs();
            var pixel = IconPixel(window, combo);
            window.Close();
            return pixel;
        });

        Assert.AreEqual(Red, pixel);
    }
}
