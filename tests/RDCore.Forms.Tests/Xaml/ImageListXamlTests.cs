using RDCore.Forms.Model;
using RDCore.Forms.Tests.Model;
using RDCore.Forms.Xaml;
using K = RDCore.Forms.Xaml.FormXamlDiagnosticKind;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Tests.Xaml;

[TestClass]
public sealed class ImageListXamlTests
{
    private const string Ns = FormXaml.Namespace;

    private static FormXamlRead Read(string body) => FormXaml.Read($"<Form xmlns=\"{Ns}\" FormatVersion=\"1\" Name=\"Form1\">{body}</Form>");

    [TestMethod]
    public void AnImageListsPictures_AreWrittenAndReadBack_WithTheirKeysAndTags()
    {
        var root = new ComponentInstance(ComponentClasses.Form, "Form1");
        var imageList = new ComponentInstance(ComponentClasses.ImageList, "ImageList1").Set(P.ImageWidth, (short)2).Set(P.ImageHeight, (short)2)
            .Set(P.ListImages, [new ListImage(TestPictures.Bitmap2x2) { Key = "red", Tag = "first" }, new ListImage(TestPictures.Gif1x1)]);
        imageList.SetContainer(root);
        var written = FormXaml.Write(new FormDefinition(root));

        var read = FormXaml.Read(written);

        Assert.IsEmpty(read.Diagnostics, string.Join(Environment.NewLine, read.Diagnostics));
        Assert.AreEqual(written, FormXaml.Write(read.Form!));
        CollectionAssert.AreEqual(imageList.Get(P.ListImages).ToArray(), read.Form!.Root.Contents.Single().Get(P.ListImages).ToArray());
        StringAssert.Contains(written, "<ImageList.ListImages>");
        StringAssert.Contains(written, $"<ListImage Picture=\"{Convert.ToBase64String(TestPictures.Bitmap2x2)}\" Key=\"red\" Tag=\"first\" />");
        StringAssert.Contains(written, $"<ListImage Picture=\"{Convert.ToBase64String(TestPictures.Gif1x1)}\" />");
    }

    [TestMethod]
    public void AnImageListWithNoListImagesWritten_HasNone_AndIsNotSet()
    {
        var imageList = Read("<ImageList Name=\"ImageList1\" />").Form!.Root.Contents.Single();

        Assert.IsEmpty(imageList.Get(P.ListImages));
        Assert.IsFalse(imageList.IsSet(P.ListImages));
    }

    [TestMethod]
    [DataRow("<ListImage Key=\"k\" />", DisplayName = "No Picture")]
    [DataRow("<ListImage Picture=\"not base64!\" />", DisplayName = "A Picture that is not base64")]
    [DataRow("<ListImage Picture=\"\" />", DisplayName = "A Picture of no bytes")]
    [DataRow("<Image Picture=\"R0lGODlhAQABAIAAAP///wAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==\" />", DisplayName = "Another element")]
    public void AListImageThatIsNotOne_IsKeptAsText_AndTheOthersAreRead(string item)
    {
        var read = Read($"<ImageList Name=\"ImageList1\"><ImageList.ListImages>{item}"
            + $"<ListImage Picture=\"{Convert.ToBase64String(TestPictures.Gif1x1)}\" Key=\"kept\" /></ImageList.ListImages></ImageList>");
        var imageList = read.Form!.Root.Contents.Single();

        Assert.AreEqual(K.UnmodelledElement, read.Diagnostics.Single().Kind);
        Assert.AreEqual("kept", imageList.Get(P.ListImages).Single().Key);
        Assert.HasCount(1, imageList.UnmodelledContents);
    }
}
