using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using static RDCore.Forms.Tests.Model.TestPictures;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Tests.Model;

// A form's Icon, as measured with VB6, native and p-code agreeing: an icon or a cursor, or none; a bitmap is 380.
[TestClass]
public sealed class FormIconTests
{
    [TestMethod]
    public void AFormsIcon_HoldsAnIconOrACursorOrNone_AndRefusesAnyOtherPicture()
    {
        var root = new ComponentInstance(VBClasses.Form, "Form1");
        var icon = Icon(1, PngHeader(2, 2));

        var accepted = new[] { root.TrySet(P.Icon, icon), root.TrySet(P.Icon, Icon(2, PngHeader(2, 2))), root.TrySet(P.Icon, null) };
        root.Set(P.Icon, icon);
        var refused = new[] { root.TrySet(P.Icon, Bitmap2x2), root.TrySet(P.Icon, PngHeader(2, 2)), root.TrySet(P.Icon, Hex("00000100 0100")) };

        CollectionAssert.AreEqual(new[] { true, true, true }, accepted);
        CollectionAssert.AreEqual(new[] { false, false, false }, refused);
        Assert.AreSame(icon, root.Get(P.Icon));
    }

    [TestMethod]
    public void AFormsPicture_HoldsAnyPicture()
    {
        var root = new ComponentInstance(VBClasses.Form, "Form1");

        var set = new[] { root.TrySet(P.Picture, Bitmap2x2), root.TrySet(P.Picture, Icon(1, PngHeader(2, 2))), root.TrySet(P.Picture, null) };

        CollectionAssert.AreEqual(new[] { true, true, true }, set);
    }
}
