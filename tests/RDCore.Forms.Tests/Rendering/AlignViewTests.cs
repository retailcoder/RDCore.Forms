using Avalonia.Controls;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using M = RDCore.Forms.Model.VB.VBMethods;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Rendering;

// A shown form's aligned PictureBoxes, as measured with VB6: the last box of the form is the nearest its edge, and ZOrder changes no
// box's place among the docked ones.
[TestClass]
public sealed class AlignViewTests
{
    private static ComponentInstance Box(ComponentInstance root, string name, Align align, double size)
    {
        var box = new ComponentInstance(VBClasses.PictureBox, name).Set(P.Width, size).Set(P.Height, size);
        box.SetContainer(root);
        return box.Set(P.Align, align);
    }

    [TestMethod]
    public void ZOrder_LeavesTheDockingOrder_AndTheViewShowsEachBoxWhereItIsDocked()
    {
        var (shown, docked) = UI.Run(() =>
        {
            var root = new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, 400d).Set(P.Height, 300d);
            var first = Box(root, "First", Align.Top, 30);
            var last = Box(root, "Last", Align.Top, 20);
            var left = Box(root, "Side", Align.Left, 50);
            var window = new VBFormWindow(new FormDefinition(root));
            window.Show();
            window.Invoke(first, M.ZOrder, [FormValue.FromInteger(0)]);
            window.Invoke(last, M.ZOrder, [FormValue.FromInteger(1)]);
            root.Set(P.Height, 280d);
            Dispatcher.UIThread.RunJobs();
            var view = window.View;
            var shown = new[] { first, last, left }
                .Select(box => view.ControlOf(box)!)
                .Select(control => $"{Canvas.GetLeft(control)},{Canvas.GetTop(control)} {control.Width}x{control.Height}")
                .ToArray();
            var docked = (first.Get(P.Top), last.Get(P.Top), left.Get(P.Top), left.Get(P.Height));
            window.Close();
            return (shown, docked);
        });

        Assert.AreEqual((20d, 0d, 50d, 230d), docked);
        CollectionAssert.AreEqual(new[] { "0,20 400x30", "0,0 400x20", "0,50 50x230" }, shown);
    }
}
