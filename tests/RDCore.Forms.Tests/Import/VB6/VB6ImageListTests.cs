using RDCore.Forms.Import.VB6;
using RDCore.Forms.Model;
using RDCore.Forms.Tests.Model;

namespace RDCore.Forms.Tests.Import.VB6;

// MSComctlLib's ImageList, as VB6 writes it: its pictures in an Images bag, each in a ListImage bag citing a picture record of the
// companion that starts with StdPicture's CLSID.
[TestClass]
public sealed class VB6ImageListTests
{
    private static readonly byte[] StdPicture = new Guid("0BE35204-8F91-11CE-9DE3-00AA004BB851").ToByteArray();

    private static string Form(params string[] contents) =>
        string.Join("\r\n", [
            "VERSION 5.00",
            "Object = \"{831FDD16-0C5C-11D2-A9FC-0000F8754DA1}#2.0#0\"; \"MSCOMCTL.OCX\"",
            "Begin VB.Form Form1 ",
            "   Caption         =   \"Form1\"",
            "   ClientHeight    =   3000",
            "   ClientWidth     =   4000",
            .. contents,
            "End",
            "Attribute VB_Name = \"Form1\"",
            ""]);

    // An ImageList named ImageList1 at 120, 240 twips, with the Images bag's lines given, or none.
    private static string[] ImageList(params string[] images) =>
    [
        "   Begin MSComctlLib.ImageList ImageList1 ",
        "      Left            =   120",
        "      Top             =   240",
        "      _ExtentX        =   1005",
        "      _ExtentY        =   1005",
        "      BackColor       =   -2147483643",
        "      ImageWidth      =   2",
        "      ImageHeight     =   2",
        "      MaskColor       =   65280",
        "      UseMaskColor    =   0   'False",
        "      _Version        =   393216",
        .. images.Length == 0 ? [] : images.Prepend("      BeginProperty Images {2C247F25-8591-11D1-B16A-00C0F0283628} ").Append("      EndProperty"),
        "   End",
    ];

    private static string[] Images(int count, params string[][] listImages) =>
        [$"         NumListImages   =   {count}", .. listImages.SelectMany(l => l)];

    private static string[] ListImage(int number, params string[] fields) =>
        [
            $"         BeginProperty ListImage{number} {{2C247F27-8591-11D1-B16A-00C0F0283628}} ",
            .. fields.Select(f => "            " + f),
            "         EndProperty",
        ];

    // A ListImage's picture record: the length of the rest, StdPicture's CLSID, "lt\0\0", the image's length, the image.
    private static byte[] ObjectPicture(byte[] image) =>
        [.. BitConverter.GetBytes(24 + image.Length), .. StdPicture, 0x6C, 0x74, 0, 0, .. BitConverter.GetBytes(image.Length), .. image];

    private static readonly byte[] Red = TestPictures.Bitmap2x2;
    private static readonly byte[] Gif = TestPictures.Gif1x1;

    // Red at 0000, then Gif.
    private static byte[] TwoPictures() => [.. ObjectPicture(Red), .. ObjectPicture(Gif)];

    private static string GifOffset => (28 + Red.Length).ToString("X4");

    [TestMethod]
    public void AnImageListsColoursSizeAndPictures_AreRead_WithEachPicturesKeyAndTag()
    {
        var import = VB6FormReader.Read(Form(ImageList(Images(2,
            ListImage(1, "Picture         =   \"Form1.frx\":0000", "Key             =   \"red\"", "Object.Tag             =   \"first\""),
            ListImage(2, $"Picture         =   \"Form1.frx\":{GifOffset}", "Key             =   \"\"")))), TwoPictures());
        var imageList = import.Form!.Find("ImageList1")!;

        Assert.IsEmpty(import.Diagnostics);
        Assert.AreSame(ComponentClasses.ImageList, imageList.Class);
        Assert.AreEqual((8d, 16d), (imageList.Get(Properties.Left), imageList.Get(Properties.Top)));
        Assert.AreEqual(FormColor.FromSystem(SystemColor.Window), imageList.Get(Properties.BackColor));
        Assert.AreEqual(FormColor.FromRgb(0, 0xFF, 0), imageList.Get(Properties.MaskColor));
        Assert.IsFalse(imageList.Get(Properties.UseMaskColor));
        Assert.AreEqual(((short)2, (short)2), (imageList.Get(Properties.ImageWidth), imageList.Get(Properties.ImageHeight)));
        CollectionAssert.AreEqual(new[] { new ListImage(Red) { Key = "red", Tag = "first" }, new ListImage(Gif) },
            imageList.Get(Properties.ListImages).ToArray());
        CollectionAssert.AreEqual(new[] { "_ExtentX", "_ExtentY", "_Version" }, imageList.UnmodelledProperties.Select(p => p.Name).ToArray());
    }

    [TestMethod]
    public void AnImageListWithNoImagesBag_HasNoPictures_AndItsClassesDefaults()
    {
        var import = VB6FormReader.Read(Form(
            "   Begin MSComctlLib.ImageList ImageList1 ", "      Left            =   120", "      Top             =   240", "   End"));
        var imageList = import.Form!.Find("ImageList1")!;

        Assert.IsEmpty(import.Diagnostics);
        Assert.IsEmpty(imageList.Get(Properties.ListImages));
        Assert.IsFalse(imageList.IsSet(Properties.ListImages));
        Assert.AreEqual(FormColor.FromSystem(SystemColor.Window), imageList.Get(Properties.BackColor));
        Assert.IsTrue(imageList.Get(Properties.UseMaskColor));
    }

    [TestMethod]
    public void TheClassName_IsReadWithoutRegardToCase_AndAnotherMSComctlLibClass_IsKeptAsText()
    {
        var import = VB6FormReader.Read(Form(
            "   Begin mscomctllib.imagelist ImageList1 ", "   End",
            "   Begin MSComctlLib.ListView ListView1 ", "   End"));

        Assert.AreSame(ComponentClasses.ImageList, import.Form!.Find("ImageList1")!.Class);
        Assert.IsNull(import.Form.Find("ListView1"));
        Assert.AreEqual(VB6ImportDiagnosticKind.UnmodelledClass, import.Diagnostics.Single().Kind);
    }

    [TestMethod]
    public void AKeyCitedAsALongString_IsReadFromTheCompanion()
    {
        byte[] key = [5, 0, 0, 0, .. "a key"u8];
        var import = VB6FormReader.Read(Form(ImageList(Images(1,
            ListImage(1, $"Picture         =   \"Form1.frx\":{key.Length:X4}", "Key             =   $\"Form1.frx\":0000")))),
            [.. key, .. ObjectPicture(Red)]);

        Assert.IsEmpty(import.Diagnostics);
        Assert.AreEqual("a key", import.Form!.Find("ImageList1")!.Get(Properties.ListImages).Single().Key);
    }

    // Each Images bag VB6 does not write: the diagnostic, and the whole bag kept as text, so nothing of it is lost.
    [TestMethod]
    [DataRow("count", VB6ImportDiagnosticKind.InvalidValue, DisplayName = "NumListImages is not the number of ListImage bags")]
    [DataRow("no count", VB6ImportDiagnosticKind.InvalidValue, DisplayName = "No NumListImages")]
    [DataRow("order", VB6ImportDiagnosticKind.InvalidValue, DisplayName = "ListImage2 before ListImage1")]
    [DataRow("field", VB6ImportDiagnosticKind.InvalidValue, DisplayName = "A field a ListImage does not have")]
    [DataRow("twice", VB6ImportDiagnosticKind.InvalidValue, DisplayName = "A Key written twice")]
    [DataRow("no picture", VB6ImportDiagnosticKind.InvalidValue, DisplayName = "A ListImage with no Picture")]
    [DataRow("text picture", VB6ImportDiagnosticKind.InvalidValue, DisplayName = "A Picture that cites nothing")]
    [DataRow("dollar", VB6ImportDiagnosticKind.InvalidValue, DisplayName = "A Picture cited as a long string")]
    [DataRow("plain record", VB6ImportDiagnosticKind.MissingRecord, DisplayName = "A Picture citing a picture record with no CLSID")]
    [DataRow("past the end", VB6ImportDiagnosticKind.MissingRecord, DisplayName = "A Picture citing past the companion's end")]
    public void AnImagesBagThatCannotBeRead_IsReportedAndKeptAsText(string fault, VB6ImportDiagnosticKind kind)
    {
        var picture = "Picture         =   \"Form1.frx\":0000";
        string[] images = fault switch
        {
            "count" => Images(2, ListImage(1, picture)),
            "no count" => ListImage(1, picture),
            "order" => Images(2, ListImage(2, picture), ListImage(1, picture)),
            "field" => Images(1, ListImage(1, picture, "Index           =   1")),
            "twice" => Images(1, ListImage(1, picture, "Key             =   \"a\"", "Key             =   \"b\"")),
            "no picture" => Images(1, ListImage(1, "Key             =   \"a\"")),
            "text picture" => Images(1, ListImage(1, "Picture         =   \"red.bmp\"")),
            "dollar" => Images(1, ListImage(1, "Picture         =   $\"Form1.frx\":0000")),
            "plain record" => Images(1, ListImage(1, "Picture         =   \"Form1.frx\":0000")),
            _ => Images(1, ListImage(1, "Picture         =   \"Form1.frx\":0100")),
        };
        byte[] companion = fault == "plain record"
            ? [.. BitConverter.GetBytes(8 + Red.Length), 0x6C, 0x74, 0, 0, .. BitConverter.GetBytes(Red.Length), .. Red]
            : ObjectPicture(Red);

        var import = VB6FormReader.Read(Form(ImageList(images)), companion);
        var imageList = import.Form!.Find("ImageList1")!;

        Assert.AreEqual(kind, import.Diagnostics.Single().Kind);
        Assert.IsEmpty(imageList.Get(Properties.ListImages));
        var kept = imageList.UnmodelledProperties.Single(p => p.Name == "Images");
        StringAssert.StartsWith(kept.Text, "      BeginProperty Images");
        StringAssert.Contains(kept.Text, "EndProperty");
    }

    [TestMethod]
    public void AnImagesBagWithNoCompanion_IsAMissingCompanion_AndKeptAsText()
    {
        var import = VB6FormReader.Read(Form(ImageList(Images(1, ListImage(1, "Picture         =   \"Form1.frx\":0000")))));

        Assert.AreEqual(VB6ImportDiagnosticKind.MissingCompanion, import.Diagnostics.Single().Kind);
        Assert.IsTrue(import.Form!.Find("ImageList1")!.UnmodelledProperties.Any(p => p.Name == "Images"));
    }

    [TestMethod]
    public void AnImportedImageList_ComesBackFromItsDocument_AsTheSameModel()
    {
        var import = VB6FormReader.Read(Form(ImageList(Images(2,
            ListImage(1, "Picture         =   \"Form1.frx\":0000", "Key             =   \"red\"", "Object.Tag             =   \"<first & é>\""),
            ListImage(2, $"Picture         =   \"Form1.frx\":{GifOffset}")))), TwoPictures());

        FormModelAssert.RoundTripsThroughXaml(import.Form!);
    }
}
