using System.Globalization;
using RDCore.Forms.Import.VB6;
using RDCore.Forms.Model;
using RDCore.SDK.Runtime.Stdole;
using RDCore.SDK.Runtime.Vbrun;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Tests.Model;

// A placed UserControl's site: its copy, its Ambient and its PropertyBag, as VB6 gives them to the UserControl's code (measured with
// VB6, native and p-code agreeing).
[TestClass]
public sealed class UserControlSiteTests
{
    private static string Ctl(params string[] settings) =>
        string.Join("\r\n", [
            "VERSION 5.00",
            "Begin VB.UserControl Gauge ",
            "   ClientHeight    =   500",
            "   ClientWidth     =   1500",
            .. settings.Select(s => "   " + s),
            "End",
            "Attribute VB_Name = \"Gauge\"",
            ""]);

    // Gauge1 as the VB6 grid places it (2000 by 600 twips), with the values its ReadProperties read and an extender property the
    // reader keeps as text; Gauge2 plain. The form's colors and font are not its defaults, so that Ambient reads them from the form.
    private const string PlacingForm = """
        VERSION 5.00
        Begin VB.Form Form1
           BackColor       =   &H00FF0000&
           ForeColor       =   &H8000000F&
           ClientHeight    =   4500
           ClientWidth     =   6000
           BeginProperty Font {0BE35203-8F91-11CE-9DE3-00AA004BB851}
              Name            =   "Tahoma"
              Size            =   10
              Charset         =   0
              Weight          =   400
              Underline       =   0   'False
              Italic          =   -1  'True
              Strikethrough   =   0   'False
           EndProperty
           Begin Project1.Gauge Gauge1
              Height          =   600
              Left            =   100
              TabIndex        =   0
              Top             =   600
              Width           =   2000
              WhatsThisHelpID =   3000000000
              _ExtentX        =   3528
              _ExtentY        =   1058
              Caption         =   "hello"
              Num             =   42
              Big             =   100000
              Neg             =   -5
              Hex2            =   &H8000000F&
              Flag            =   -1  'True
              Dbl             =   1.5
              Hex             =   -2147483633
              Str             =   "007"
              Pic             =   "Form1.frx":0000
              BeginProperty Font {0BE35203-8F91-11CE-9DE3-00AA004BB851}
                 Name            =   "Arial"
                 Size            =   12
                 Charset         =   0
                 Weight          =   700
                 Underline       =   0   'False
                 Italic          =   0   'False
                 Strikethrough   =   0   'False
              EndProperty
           End
           Begin Project1.Gauge Gauge2
              Height          =   500
              Left            =   100
              TabIndex        =   1
              Top             =   1300
              Width           =   1500
              _ExtentX        =   2646
              _ExtentY        =   882
           End
        End
        Attribute VB_Name = "Form1"

        """;

    private static (FormDefinition Gauge, FormDefinition Form) Read(params string[] settings)
    {
        var gauge = VB6FormReader.Read(Ctl(settings)).Form!;
        var type = UserControlClasses.Of(gauge, "Project1");
        return (gauge, VB6FormReader.Read(PlacingForm, options: new VB6ImportOptions { Classes = [type] }).Form!);
    }

    private static UserControlSite Gauge1Site(params string[] settings) => UserControlSite.Of(Read(settings).Form.Find("Gauge1")!);

    [TestMethod]
    public void APlacedControl_HasOneSite_WithItsOwnCopyOfItsUserControl()
    {
        var (gauge, form) = Read();
        var gauge1 = form.Find("Gauge1")!;
        var gauge2 = form.Find("Gauge2")!;

        var site = UserControlSite.Of(gauge1);

        Assert.AreSame(site, UserControlSite.Of(gauge1));
        Assert.AreSame(gauge1, site.Extender);
        Assert.AreNotSame(gauge, site.UserControl);
        Assert.AreNotSame(site.UserControl, UserControlSite.Of(gauge2).UserControl);
        Assert.AreEqual("Gauge", site.UserControl.Root.Name);
    }

    [TestMethod]
    public void AControlInvisibleAtRunTime_HasASiteToo()
    {
        var (_, form) = Read("InvisibleAtRuntime=   -1  'True");

        var site = UserControlSite.Of(form.Find("Gauge2")!);

        Assert.AreEqual("Gauge", site.UserControl.Root.Name);
    }

    [TestMethod]
    public void AComponentOfAnotherClass_HasNoSite()
    {
        var form = new ComponentInstance(ComponentClasses.Form, "Form1");

        Assert.ThrowsExactly<ArgumentException>(() => UserControlSite.Of(form));
    }

    // The PropertyBag a UserControl's ReadProperties reads, as VB6 types the form file's text.
    [TestMethod]
    [DataRow("Caption", FormValueKind.String, "hello", DisplayName = "\"hello\": String")]
    [DataRow("caption", FormValueKind.String, "hello", DisplayName = "a name in another case")]
    [DataRow("Str", FormValueKind.String, "007", DisplayName = "\"007\": String")]
    [DataRow("Num", FormValueKind.Integer, "42", DisplayName = "42: Integer")]
    [DataRow("Neg", FormValueKind.Integer, "-5", DisplayName = "-5: Integer")]
    [DataRow("Flag", FormValueKind.Integer, "-1", DisplayName = "-1 'True: Integer")]
    [DataRow("Big", FormValueKind.Long, "100000", DisplayName = "100000: Long")]
    [DataRow("Hex", FormValueKind.Long, "-2147483633", DisplayName = "-2147483633: Long")]
    [DataRow("Hex2", FormValueKind.Long, "-2147483633", DisplayName = "&H8000000F&: Long")]
    [DataRow("Dbl", FormValueKind.Currency, "1.5", DisplayName = "1.5: Currency")]
    [DataRow("_ExtentX", FormValueKind.Long, "3519", DisplayName = "_ExtentX from 133 pixels, not the file's 3528")]
    [DataRow("_ExtentY", FormValueKind.Long, "1058", DisplayName = "_ExtentY from 40 pixels")]
    public void ReadProperty_TypesTheFilesText_AsVB6Does(string name, FormValueKind kind, string value)
    {
        var bag = Gauge1Site().PropertyBag;

        Assert.AreEqual(PropertyBagFailure.None, bag.TryReadProperty(name, out var read));
        Assert.AreEqual(kind, read.Kind);
        Assert.AreEqual(value, Convert.ToString(read.ClrValue, CultureInfo.InvariantCulture));
    }

    [TestMethod]
    [DataRow("Left", PropertyBagFailure.DataValueNotFound, DisplayName = "an extender property: not the UserControl's (327)")]
    [DataRow("WhatsThisHelpID", PropertyBagFailure.DataValueNotFound, DisplayName = "an extender property the file sets past its range (327)")]
    [DataRow("Missing", PropertyBagFailure.DataValueNotFound, DisplayName = "a name the file lacks, with no default: 327")]
    [DataRow("", PropertyBagFailure.InvalidProcedureCall, DisplayName = "an empty name: 5")]
    public void ReadProperty_OfAValueTheBagLacks_Fails(string name, PropertyBagFailure failure) =>
        Assert.AreEqual(failure, Gauge1Site().PropertyBag.TryReadProperty(name, out _));

    [TestMethod]
    public void ReadProperty_OfAFontBag_GivesANewStdFontOfItsFont_EachTime()
    {
        var bag = Gauge1Site().PropertyBag;

        Assert.AreEqual(PropertyBagFailure.None, bag.TryReadProperty("Font", out var first));
        Assert.AreEqual(PropertyBagFailure.None, bag.TryReadProperty("font", out var second));

        var font = (StdFontObject)first.AsObject()!;
        Assert.AreEqual("Arial", font.Name);
        Assert.AreEqual(12m, font.Size);
        Assert.IsTrue(font.Bold);
        Assert.AreNotSame(font, second.AsObject());
    }

    [TestMethod]
    public void ReadProperty_OfTheExtent_IsThePlacedControlsSizeWhenItIsRead()
    {
        var site = Gauge1Site();

        site.Extender.Set(P.Width, 100);

        Assert.AreEqual(PropertyBagFailure.None, site.PropertyBag.TryReadProperty("_ExtentX", out var extent));
        Assert.AreEqual(2646, extent.AsLong());
    }

    [TestMethod]
    public void Ambient_ReadsTheFormsColors_Unsigned()
    {
        var ambient = Gauge1Site().Ambient;

        Assert.AreEqual(0xFF0000u, ambient.BackColor);
        Assert.AreEqual(2147483663u, ambient.ForeColor);
    }

    [TestMethod]
    public void Ambient_ReadsACopyOfTheFormsFont_EachTime()
    {
        var ambient = Gauge1Site().Ambient;

        var font = ambient.Font;

        Assert.AreEqual("Tahoma", font.Name);
        Assert.IsTrue(font.Italic);
        Assert.AreNotSame(font, ambient.Font);
    }

    [TestMethod]
    public void Ambient_ScaleUnits_IsTheFormsScaleModeWhenItIsRead()
    {
        var site = Gauge1Site();
        Assert.AreEqual("Twip", site.Ambient.ScaleUnits);

        site.Extender.Container!.Set(P.ScaleMode, ScaleMode.Pixel);

        Assert.AreEqual("Pixel", site.Ambient.ScaleUnits);
    }

    [TestMethod]
    public void Ambient_DisplayName_IsThePlacedControlsName() =>
        Assert.AreEqual("Gauge1", Gauge1Site().Ambient.DisplayName);

    [TestMethod]
    public void Ambient_DisplayAsDefault_IsThePlacedControlsDefault()
    {
        var plain = Gauge1Site();
        var site = Gauge1Site("DefaultCancel   =   -1  'True");

        site.Extender.Set(P.Default, true);

        Assert.IsFalse(plain.Ambient.DisplayAsDefault);
        Assert.IsTrue(site.Ambient.DisplayAsDefault);
    }
}
