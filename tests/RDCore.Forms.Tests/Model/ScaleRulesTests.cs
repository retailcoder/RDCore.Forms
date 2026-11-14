using RDCore.Forms.Drawing;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Tests.Model;

// A scale's five properties as VB6 sets them at run time: a borderless PictureBox of 6000 by 1500 twips, and a form of 7005 by 3000.
[TestClass]
public sealed class ScaleRulesTests
{
    private static ComponentInstance Picture()
    {
        var picture = new ComponentInstance(VBClasses.PictureBox, "Picture1")
            .Set(P.Width, 400d).Set(P.Height, 100d).Set(P.BorderStyle, BorderStyle.None);
        ScaleRules.Follow(picture);
        return picture;
    }

    private static ComponentInstance Form()
    {
        var form = new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, 467d).Set(P.Height, 200d);
        ScaleRules.Follow(form);
        return form;
    }

    private static (ScaleMode Mode, double Left, double Top, double Width, double Height) Five(ComponentInstance component) =>
        (component.Get(P.ScaleMode), component.Get(P.ScaleLeft), component.Get(P.ScaleTop), component.Get(P.ScaleWidth), component.Get(P.ScaleHeight));

    // Each step sets one property as a program's code does, and the five read what VB6 read after it.
    [TestMethod]
    public void EachSet_LeavesTheScaleAsVB6Does()
    {
        var picture = Picture();
        (string Name, object Value, ScaleMode Mode, double Left, double Top, double Width, double Height)[] steps =
        [
            ("ScaleWidth", 3000d, ScaleMode.User, 0, 0, 3000, 1500),
            ("ScaleMode", ScaleMode.Twip, ScaleMode.Twip, 0, 0, 6000, 1500),
            ("ScaleLeft", 100d, ScaleMode.User, 100, 0, 6000, 1500),
            ("ScaleMode", ScaleMode.Pixel, ScaleMode.Pixel, 0, 0, 400, 100),
            ("ScaleHeight", 50d, ScaleMode.User, 0, 0, 400, 50),
            ("ScaleTop", -10d, ScaleMode.User, 0, -10, 400, 50),
            ("ScaleMode", ScaleMode.Pixel, ScaleMode.Pixel, 0, 0, 400, 100),
            ("ScaleMode", ScaleMode.User, ScaleMode.User, 0, 0, 400, 100),
            ("ScaleMode", ScaleMode.User, ScaleMode.User, 0, 0, 400, 100),
            ("ScaleWidth", -3000d, ScaleMode.User, 0, 0, -3000, 100),
            ("ScaleMode", ScaleMode.Twip, ScaleMode.Twip, 0, 0, 6000, 1500),
            ("ScaleLeft", 50d, ScaleMode.User, 50, 0, 6000, 1500),
            ("ScaleMode", ScaleMode.Twip, ScaleMode.Twip, 0, 0, 6000, 1500),
            // The value it has already.
            ("ScaleWidth", 6000d, ScaleMode.User, 0, 0, 6000, 1500),
        ];

        Assert.AreEqual((ScaleMode.Twip, 0d, 0d, 6000d, 1500d), Five(picture), "initial");
        foreach (var (name, value, mode, left, top, width, height) in steps)
        {
            Assert.IsTrue(picture.TrySet(picture.Class.Property(name)!, value), $"{name} = {value}");
            Assert.AreEqual((mode, left, top, width, height), Five(picture), $"{name} = {value}");
        }
    }

    [TestMethod]
    [DataRow("ScaleWidth")]
    [DataRow("ScaleHeight")]
    public void AScaleWidthOrHeightOf0_IsRefused_AndChangesNothing(string name)
    {
        var picture = Picture();
        picture.TrySet(P.ScaleMode, ScaleMode.Pixel);

        Assert.IsFalse(picture.TrySet(picture.Class.Property(name)!, 0d));
        Assert.AreEqual((ScaleMode.Pixel, 0d, 0d, 400d, 100d), Five(picture));
    }

    // VB6 refuses 8 and -1 with error 380 too, but reads the refused value back; RDForms keeps the one it had.
    [TestMethod]
    [DataRow(8)]
    [DataRow(-1)]
    public void AScaleModeThatIsNone_IsRefused_AndChangesNothing(int mode)
    {
        var picture = Picture();
        picture.TrySet(P.ScaleMode, ScaleMode.Pixel);

        Assert.IsFalse(picture.TrySet(P.ScaleMode, (ScaleMode)mode));
        Assert.AreEqual((ScaleMode.Pixel, 0d, 0d, 400d, 100d), Five(picture));
    }

    [TestMethod]
    public void AFormsScaleMode_SetsItsClientArea_InTheUnit()
    {
        var form = Form();
        form.TrySet(P.ScaleMode, ScaleMode.Pixel);

        Assert.AreEqual((ScaleMode.Pixel, 0d, 0d, 467d, 200d), Five(form));
    }

    [TestMethod]
    public void AResize_InAUserScale_KeepsTheUnit_AndInTwips_SetsTheSize()
    {
        var picture = Picture();
        picture.TrySet(P.ScaleWidth, 3000d);
        picture.TrySet(P.Width, 300d);
        var user = Five(picture);
        picture.TrySet(P.ScaleMode, ScaleMode.Twip);
        picture.TrySet(P.Width, 400d);

        Assert.AreEqual((ScaleMode.User, 0d, 0d, 2250d, 1500d), user);
        Assert.AreEqual((ScaleMode.Twip, 0d, 0d, 6000d, 1500d), Five(picture));
    }

    [TestMethod]
    public void AFormsResize_InAUserScale_KeepsTheUnit_InSinglePrecision()
    {
        var form = Form();
        form.TrySet(P.ScaleWidth, 3000d);
        form.TrySet(P.ScaleLeft, 10d);
        // 7005 twips of client area to 5505.
        form.TrySet(P.Width, 367d);

        Assert.AreEqual((double)(float)(3000d * 5505 / 7005), form.Get(P.ScaleWidth));
        Assert.AreEqual(2357.602, form.Get(P.ScaleWidth), 0.0005);
        Assert.AreEqual((ScaleMode.User, 10d, 3000d), (form.Get(P.ScaleMode), form.Get(P.ScaleLeft), form.Get(P.ScaleHeight)));
    }

    [TestMethod]
    public void AUserScale_KeepsItsUnit_ThroughAnEmptyInside()
    {
        var picture = Picture();
        picture.TrySet(P.ScaleWidth, 3000d);
        picture.TrySet(P.Width, 300d);
        picture.TrySet(P.Width, 0d);
        picture.TrySet(P.Width, 200d);

        Assert.AreEqual(1500d, picture.Get(P.ScaleWidth));
    }

    [TestMethod]
    public void ABorder_InTwips_TakesItsWidthOffTheScale()
    {
        var picture = Picture();
        // A 3D border is two pixels on each side.
        picture.TrySet(P.BorderStyle, BorderStyle.FixedSingle);

        Assert.AreEqual((ScaleMode.Twip, 0d, 0d, 5940d, 1440d), Five(picture));
    }

    [TestMethod]
    public void Following_SetsAUnitScale_ToTheInside_AndKeepsAUserScale()
    {
        // A designer file can hold a ScaleWidth that is not its client width: 8000 over 7995 twips.
        var twips = new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, 533d).Set(P.Height, 200d)
            .Set(P.ScaleWidth, 8000d).Set(P.ScaleHeight, 3000d);
        var user = new ComponentInstance(VBClasses.Form, "Form2").Set(P.Width, 533d).Set(P.Height, 200d)
            .Set(P.ScaleMode, ScaleMode.User).Set(P.ScaleLeft, 5d).Set(P.ScaleWidth, 100d).Set(P.ScaleHeight, 50d);
        ScaleRules.Follow(twips);
        ScaleRules.Follow(user);

        Assert.AreEqual((ScaleMode.Twip, 0d, 0d, 7995d, 3000d), Five(twips));
        Assert.AreEqual((ScaleMode.User, 5d, 0d, 100d, 50d), Five(user));
    }

    [TestMethod]
    public void Following_Twice_AppliesEachRuleOnce()
    {
        var picture = Picture();
        ScaleRules.Follow(picture);
        picture.TrySet(P.ScaleWidth, 3000d);
        picture.TrySet(P.Width, 300d);

        Assert.AreEqual(2250d, picture.Get(P.ScaleWidth));
    }

    [TestMethod]
    public void AComponent_NotFollowed_KeepsWhatIsSet()
    {
        var picture = new ComponentInstance(VBClasses.PictureBox, "Picture1").Set(P.Width, 400d).Set(P.Height, 100d);
        picture.TrySet(P.ScaleWidth, 3000d);

        Assert.AreEqual(ScaleMode.Twip, picture.Get(P.ScaleMode));
    }
}
