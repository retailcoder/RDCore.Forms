using System.Globalization;
using Avalonia;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Import.MSForms;
using RDCore.Forms.Model;
using RDCore.SDK.Forms;
using static RDCore.Forms.Tests.Controls.PictureControlTests;
using static RDCore.Forms.Tests.Model.TestPictures;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Import.MSForms;

// The fixtures: AllControls.storage is a form's storage holding one control of each Microsoft Forms class, with no .frx
// header; Resources.frx and ExampleView.frx are two exported UserForms' companions, with their .frm files.
[TestClass]
public sealed class MSFormsFormReaderTests
{
    private const double Delta = 1e-9;

    private CultureInfo? culture;

    // the messages a test reads are English, unless it sets another culture.
    [TestInitialize]
    public void InEnglish()
    {
        culture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("en-CA");
    }

    [TestCleanup]
    public void RestoreCulture() => CultureInfo.CurrentUICulture = culture!;

    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Import", "MSForms", "Fixtures", name);

    private static string Text(string name) => File.ReadAllText(Fixture(name));

    private static byte[] Bytes(string name) => File.ReadAllBytes(Fixture(name));

    private static string UserForm(string name, params string[] properties) =>
        string.Join("\r\n", [
            "VERSION 5.00",
            "Begin {C62A69F0-16DC-11CE-9E98-00AA00574A4F} " + name + " ",
            .. properties.Select(p => "   " + p),
            "End",
            "Attribute VB_Name = \"" + name + "\"",
            "Option Explicit",
            ""]);

    private static MSFormsFormImport AllControls(Func<MSFormsControl, MSFormsControl>? change = null)
    {
        var design = MSFormsReader.Read(Bytes("AllControls.storage"));
        return MSFormsFormReader.Read(UserForm("AllControls", "Caption = \"All controls\"", "ClientHeight = 3600", "ClientWidth = 4800"),
            change is null ? design : new MSFormsDesign(Changed(design.Form, change)));
    }

    private static MSFormsControl Changed(MSFormsControl control, Func<MSFormsControl, MSFormsControl> change) =>
        change(control with { Children = [.. control.Children.Select(child => Changed(child, change))] });

    private static Func<MSFormsControl, MSFormsControl> Site(string name, Func<MSFormsSite, MSFormsSite> change) =>
        control => control.Name == name ? control with { Site = change(control.Site!) } : control;

    private static byte[] Patched(byte[] bytes, byte[] find, byte[] replacement)
    {
        var at = bytes.AsSpan().IndexOf(find);
        Assert.IsGreaterThanOrEqualTo(0, at);
        var patched = bytes.ToArray();
        replacement.CopyTo(patched, at);
        return patched;
    }

    private static ComponentInstance Find(MSFormsFormImport import, string name) => import.Form!.Find(name) ?? throw new AssertFailedException(name);

    [TestMethod]
    public void AUserForm_IsAnRDFormsForm_SizedByItsClientRectangle_InDip()
    {
        var import = MSFormsFormReader.ReadFile(Fixture("Resources.frm"));
        var root = import.Form!.Root;

        Assert.AreSame(ComponentClasses.Form, root.Class);
        Assert.AreEqual("Resources", root.Name);
        Assert.AreEqual("Resources", root.Get(P.Caption));
        Assert.AreEqual(6900 / 15d, root.Get(P.Width), Delta);
        Assert.AreEqual(4500 / 15d, root.Get(P.Height), Delta);
        Assert.AreEqual(120 / 15d, root.Get(P.Left), Delta);
        Assert.AreEqual(465 / 15d, root.Get(P.Top), Delta);
        Assert.AreEqual(StartUpPosition.CenterScreen, root.Get(P.StartUpPosition));
        Assert.AreEqual(new FormFont("Tahoma", 7.8, FormFont.NormalWeight, false, false, false, 0), root.Get(P.Font));
        Assert.StartsWith("Attribute VB_Name = \"Resources\"", import.ModuleText);
        Assert.AreEqual(11, import.ModuleLine);
        Assert.IsEmpty(import.Diagnostics, string.Join("\n", import.Diagnostics));
    }

    [TestMethod]
    public void AControlsPlace_IsInDip_AndItsSiteAndPropertiesArePorted()
    {
        var import = MSFormsFormReader.ReadFile(Fixture("Resources.frm"));
        var icon = Find(import, "ValidationErrorIcon");
        var name = Find(import, "ValidationErrorIconName");

        Assert.AreSame(ComponentClasses.Image, icon.Class);
        Assert.AreEqual(11.990551181102362 / 0.75, icon.Get(P.Left), Delta);
        Assert.AreEqual(72 / 0.75, icon.Get(P.Top), Delta);
        Assert.AreEqual(12.018897637795275 / 0.75, icon.Get(P.Width), Delta);
        Assert.IsFalse(icon.Get(P.Visible));
        Assert.AreEqual(822, icon.Get(P.Picture)!.Length);
        Assert.AreEqual((short)1, name.Get(P.TabIndex));
        Assert.AreEqual(700, name.Get(P.Font).Weight);
        Assert.AreEqual("ValidationErrorIcon", name.Get(P.Caption));
    }

    [TestMethod]
    public void EveryModelledClass_IsReadAsTheRDFormsClassItPortsTo_AndFramesHoldTheirControls()
    {
        var import = AllControls();

        Assert.AreEqual("All controls", import.Form!.Root.Get(P.Caption));
        foreach (var (name, typeName) in new[]
        {
            ("Caption1", "RDForms.Label"), ("Entry", "RDForms.TextBox"), ("Choices", "RDForms.ComboBox"), ("Items", "RDForms.ListBox"),
            ("Toggle1", "RDForms.CheckBox"), ("Option1", "RDForms.OptionButton"), ("Go", "RDForms.CommandButton"),
            ("Scroller", "RDForms.VScrollBar"), ("Picture1", "RDForms.Image"), ("Outer", "RDForms.Frame"), ("Inner", "RDForms.Frame"),
            ("Switch", "RDForms.ToggleButton"), ("Spinner", "RDForms.SpinButton"), ("Tabs", "RDForms.TabStrip"), ("Pages", "RDForms.MultiPage"),
            ("Page1", "RDForms.Page"), ("Page2", "RDForms.Page"),
        })
        {
            Assert.AreEqual(typeName, Find(import, name).Class.TypeName, name);
        }

        Assert.AreEqual("Outer", Find(import, "OuterEntry").Container!.Name);
        Assert.AreEqual("Outer", Find(import, "Inner").Container!.Name);
        Assert.AreEqual("Inner", Find(import, "InnerCheck").Container!.Name);
        Assert.AreSame(import.Form.Root, Find(import, "Outer").Container);
        Assert.AreEqual(216 / 0.75, Find(import, "Outer").Get(P.Width), Delta);
        Assert.AreEqual(144 / 0.75, Find(import, "Outer").Get(P.Height), Delta);
        Assert.AreEqual(72 / 0.75, Find(import, "Items").Get(P.Height), Delta);
        Assert.AreEqual(new FormFont("MS Sans Serif", 8, FormFont.NormalWeight, false, false, false, 0), Find(import, "Entry").Get(P.Font));
    }

    // every class of the form is modelled: nothing is kept as an unmodelled component, and nothing is reported.
    [TestMethod]
    public void AFormOfModelledClasses_KeepsNoUnmodelledComponent_AndReportsNothing()
    {
        var import = AllControls();

        Assert.IsEmpty(import.Form!.Root.UnmodelledContents);
        Assert.IsEmpty(import.Diagnostics, string.Join("\n", import.Diagnostics));
    }

    // a class the model lacks, such as an ActiveX control's, is kept with no text, and the controls it holds left out.
    [TestMethod]
    public void AClassTheModelLacks_IsKeptAsAnUnmodelledComponent_AndReported()
    {
        var classId = new Guid("12345678-1234-1234-1234-123456789ABC");
        var import = AllControls(control => control.Name == "Pages" ? control with { Kind = MSFormsControlKind.Other, ClassId = classId } : control);
        var unmodelled = import.Form!.Root.UnmodelledContents.Single();

        Assert.AreEqual("Pages", unmodelled.Name);
        Assert.AreEqual("{12345678-1234-1234-1234-123456789ABC}", unmodelled.TypeName);
        Assert.AreEqual(0, unmodelled.Text.Length);
        Assert.IsNull(import.Form.Find("Page1"));
        Assert.IsNull(import.Form.Find("FirstPageEntry"));
        Assert.IsNull(import.Form.Find("SecondPageGo"));

        var reported = import.Diagnostics.Single();
        Assert.AreEqual(MSFormsImportDiagnosticKind.UnmodelledClass, reported.Kind);
        Assert.AreEqual("Pages", reported.Component);
        Assert.AreEqual(MSFormsImportSeverity.Information, reported.Severity);
        StringAssert.Contains(reported.Message, "4");
    }

    // a MultiPage is an RDForms.MultiPage holding its pages in their tabs' order, each an RDForms.Page with its tab's caption and the
    // controls it holds; a page fills its MultiPage, so its size is not kept.
    [TestMethod]
    public void AMultiPage_IsAnRDFormsMultiPage_AndEachPageHasItsCaption_AndHoldsItsControls()
    {
        // the first page's caption, in the MultiPage's tab row.
        var storage = Patched(Bytes("AllControls.storage"), [0x05, 0, 0, 0x80, .. "Page1"u8], [0x05, 0, 0, 0x80, .. "First"u8]);
        var import = MSFormsFormReader.Read(UserForm("AllControls", "Caption = \"All controls\""), MSFormsReader.Read(storage));
        var pages = Find(import, "Pages");

        Assert.AreSame(ComponentClasses.MultiPage, pages.Class);
        CollectionAssert.AreEqual(new[] { "Page1", "Page2" }, pages.Contents.Select(page => page.Name).ToArray());
        Assert.IsTrue(pages.Contents.All(page => page.Class == ComponentClasses.Page));
        Assert.AreEqual("First", Find(import, "Page1").Get(P.Caption));
        Assert.AreEqual("Page2", Find(import, "Page2").Get(P.Caption));
        Assert.AreSame(Find(import, "Page1"), Find(import, "FirstPageEntry").Container);
        Assert.AreSame(Find(import, "Page2"), Find(import, "SecondPageGo").Container);
        Assert.IsFalse(Find(import, "Page1").UnmodelledProperties.Any(p => p.Name is "Width" or "Height"));
        Assert.IsEmpty(import.Diagnostics, string.Join("\n", import.Diagnostics));
    }

    // a TabStrip is an RDForms.TabStrip with its tabs, in their order, each with its name and its caption.
    [TestMethod]
    public void ATabStrip_IsAnRDFormsTabStrip_WithItsTabsCaptions()
    {
        // the second tab's caption; its name follows the captions.
        var storage = Patched(Bytes("AllControls.storage"), [0x04, 0, 0, 0x80, .. "Tab2"u8], [0x04, 0, 0, 0x80, .. "Last"u8]);
        var import = MSFormsFormReader.Read(UserForm("AllControls", "Caption = \"All controls\""), MSFormsReader.Read(storage));
        var tabs = Find(import, "Tabs");

        Assert.AreSame(ComponentClasses.TabStrip, tabs.Class);
        CollectionAssert.AreEqual(new[] { new Tab("Tab1", "Tab1"), new Tab("Tab2", "Last") }, tabs.Get(P.Tabs).ToArray());
        Assert.IsFalse(tabs.UnmodelledProperties.Any(p => p.Name == "Tabs"));
        Assert.IsEmpty(import.Diagnostics, string.Join("\n", import.Diagnostics));
    }

    // a ToggleButton is an RDForms.ToggleButton: its size in DIP, the Value "0" it stores as False, its tip as ToolTipText; the sunken edge
    // the format gives it by default is not reported.
    [TestMethod]
    public void AToggleButton_IsAnRDFormsToggleButton()
    {
        var import = AllControls(Site("Switch", s => s with { ControlTipText = "Flip" }));
        var toggle = Find(import, "Switch");

        Assert.AreSame(ComponentClasses.ToggleButton, toggle.Class);
        Assert.AreEqual(36 / 0.75, toggle.Get(P.Width), Delta);
        Assert.AreEqual("Flip", toggle.Get(P.ToolTipText));
        Assert.AreEqual<bool?>(false, toggle.Get(P.ToggleButtonValue));
        Assert.IsEmpty(import.Diagnostics.Where(d => d.Component == "Switch"), string.Join("\n", import.Diagnostics));
    }

    // a SpinButton is an RDForms.SpinButton: its size in DIP, its stored automatic Orientation, its tip as ToolTipText.
    [TestMethod]
    public void ASpinButton_IsAnRDFormsSpinButton()
    {
        var import = AllControls(Site("Spinner", s => s with { ControlTipText = "Step" }));
        var spinner = Find(import, "Spinner");

        Assert.AreSame(ComponentClasses.SpinButton, spinner.Class);
        Assert.AreEqual(25.511811023622048 / 0.75, spinner.Get(P.Height), Delta);
        Assert.AreEqual("Step", spinner.Get(P.ToolTipText));
        Assert.AreEqual(ControlOrientation.Auto, spinner.Get(P.Orientation));
        Assert.IsEmpty(import.Diagnostics.Where(d => d.Component == "Spinner"), string.Join("\n", import.Diagnostics));
    }

    [TestMethod]
    public void TheDesignerBlocksOwnSettings_AreKept_AndAFormsCaptionIsLiteral()
    {
        var import = MSFormsFormReader.Read(Text("ExampleView.frm"), Bytes("ExampleView.frx"));
        var root = import.Form!.Root;

        Assert.AreEqual("Example & view", root.Get(P.Caption));
        Assert.AreEqual(StartUpPosition.CenterOwner, root.Get(P.StartUpPosition));
        Assert.AreEqual(FormColor.FromRgb(255, 255, 255), root.Get(P.BackColor));
        CollectionAssert.IsSubsetOf(new[] { "ShowModal", "TypeInfoVer" }, root.UnmodelledProperties.Select(p => p.Name).ToArray());
        Assert.IsTrue(Find(import, "CancelButton").Get(P.Cancel));
        Assert.AreEqual("Cancel", Find(import, "CancelButton").Get(P.Caption));
        Assert.AreEqual(Appearance.Flat, Find(import, "AmountBox").Get(P.Appearance));
        Assert.AreEqual(6, Find(import, "OptionsFrame").Get(P.Font).Size, Delta);
        CollectionAssert.AreEqual(new[] { "OptionButton1", "OptionButton2" }, Find(import, "OptionsFrame").Contents.Select(c => c.Name).ToArray());
    }

    [TestMethod]
    public void ALiteralAmpersandInACaption_IsDoubled()
    {
        var companion = Patched(Bytes("Resources.frx"), "isn't"u8.ToArray(), "isn&t"u8.ToArray());
        var import = MSFormsFormReader.Read(Text("Resources.frm"), companion);

        Assert.StartsWith("This isn&&t really a form.", Find(import, "InstructionsLabel").Get(P.Caption));
    }

    [TestMethod]
    public void AMetafilePicture_IsKept_AndReportedAsNotDrawn()
    {
        // a placeable metafile's key in place of the bitmap's file header.
        var companion = Patched(Bytes("Resources.frx"), [0x42, 0x4D, 0x36, 0x03, 0x00, 0x00], [0xD7, 0xCD, 0xC6, 0x9A]);
        var import = MSFormsFormReader.Read(Text("Resources.frm"), companion);

        var reported = import.Diagnostics.Single();
        Assert.AreEqual(MSFormsImportDiagnosticKind.MetafilePicture, reported.Kind);
        Assert.AreEqual("ValidationErrorIcon", reported.Component);
        Assert.AreEqual("Picture", reported.Property);
        Assert.IsNotNull(Find(import, "ValidationErrorIcon").Get(P.Picture));
    }

    // a data source is kept as text where the class has the member, and reported with the port's diagnostic: nothing resolves it.
    [TestMethod]
    public void AControlSourceAndARowSource_AreKeptAsText_AndReportedAsUnresolved()
    {
        var import = AllControls(control => Site("Entry", s => s with { ControlSource = "Sheet1!A1" })(
            Site("Items", s => s with { ControlSource = "Sheet1!B1", RowSource = "Sheet1!C1:C9" })(
                Site("Go", s => s with { ControlSource = "Sheet1!D1" })(control))));

        Assert.AreEqual("Sheet1!A1", Find(import, "Entry").Get(P.ControlSource));
        Assert.AreEqual("Sheet1!B1", Find(import, "Items").Get(P.ControlSource));
        Assert.AreEqual("Sheet1!C1:C9", Find(import, "Items").Get(P.RowSource));
        Assert.AreEqual("Sheet1!D1", Find(import, "Go").UnmodelledProperties.Single(p => p.Name == "ControlSource").Text);
        Assert.AreEqual("", Find(import, "Choices").Get(P.RowSource));

        var reported = import.Diagnostics.Where(d => d.Kind == MSFormsImportDiagnosticKind.UnresolvedDataSource).ToList();
        CollectionAssert.AreEqual(new[] { "Entry.ControlSource", "Items.ControlSource", "Items.RowSource", "Go.ControlSource" },
            reported.Select(d => d.Component + "." + d.Property).ToArray());
        Assert.IsTrue(reported.All(d => d.Severity == MSFormsImportSeverity.Warning && d.Port!.Code == "RDC40007"));
        StringAssert.StartsWith(reported[0].Message, "The ControlSource of Entry is kept as text. ");
        StringAssert.EndsWith(reported[0].Message, reported[0].Port!.MessageIn(CultureInfo.CurrentUICulture));
    }

    // each class with a ControlSource keeps it as text, and reports it with the port's diagnostic.
    [TestMethod]
    [DataRow("Entry", DisplayName = "TextBox")]
    [DataRow("Choices", DisplayName = "ComboBox")]
    [DataRow("Items", DisplayName = "ListBox")]
    [DataRow("Toggle1", DisplayName = "CheckBox")]
    [DataRow("Option1", DisplayName = "OptionButton")]
    [DataRow("Switch", DisplayName = "ToggleButton")]
    [DataRow("Spinner", DisplayName = "SpinButton")]
    [DataRow("Scroller", DisplayName = "ScrollBar")]
    public void AControlSource_IsKeptAsText_AndReportedAsUnresolved(string name)
    {
        var import = AllControls(Site(name, s => s with { ControlSource = "Sheet1!A1" }));

        Assert.AreEqual("Sheet1!A1", Find(import, name).Get(P.ControlSource));
        var reported = import.Diagnostics.Single(d => d.Kind == MSFormsImportDiagnosticKind.UnresolvedDataSource);
        Assert.AreEqual(name + ".ControlSource", reported.Component + "." + reported.Property);
        Assert.AreEqual(MSFormsImportSeverity.Warning, reported.Severity);
        Assert.AreEqual("RDC40007", reported.Port!.Code);
    }

    // a ListBox's and a ComboBox's RowSource is kept as text, and reported with the port's diagnostic.
    [TestMethod]
    [DataRow("Items", DisplayName = "ListBox")]
    [DataRow("Choices", DisplayName = "ComboBox")]
    public void ARowSource_IsKeptAsText_AndReportedAsUnresolved(string name)
    {
        var import = AllControls(Site(name, s => s with { RowSource = "Sheet1!C1:C9" }));

        Assert.AreEqual("Sheet1!C1:C9", Find(import, name).Get(P.RowSource));
        var reported = import.Diagnostics.Single(d => d.Kind == MSFormsImportDiagnosticKind.UnresolvedDataSource);
        Assert.AreEqual(name + ".RowSource", reported.Component + "." + reported.Property);
        Assert.AreEqual(MSFormsImportSeverity.Warning, reported.Severity);
        Assert.AreEqual("RDC40007", reported.Port!.Code);
    }

    // a TextBox and a ComboBox keep their text as their Value, which the port map gives to Text. No fixture's TextBox or ComboBox stores
    // one, so each is given the properties of the CheckBox, which shares their structure ([MS-OFORMS] 2.2.5) and stores its Value.
    [TestMethod]
    [DataRow("Entry", DisplayName = "TextBox")]
    [DataRow("Choices", DisplayName = "ComboBox")]
    public void AValue_IsTheText(string name)
    {
        var checkBox = MSFormsReader.Read(Bytes("AllControls.storage")).Form.DescendantsAndSelf().Single(control => control.Name == "Toggle1");
        Assert.AreEqual("0", checkBox.Properties.Get<string>("Value"));

        var import = AllControls(control => control.Name == name ? control with { Properties = checkBox.Properties } : control);

        var instance = Find(import, name);
        Assert.AreEqual("0", instance.Get(P.Text));
        Assert.IsFalse(instance.UnmodelledProperties.Any(p => p.Name == "Value"));
    }

    // MSForms' CommandButton lays its picture out by its PicturePosition, 7 by default. A file that stores none reads as 7, set, so the
    // ported button, given a picture, draws as MSForms' does where a VB6 Graphical button keeps VB6's layout.
    [TestMethod]
    public void ACommandButtonWithNoPicturePosition_ReadsAsAboveCenter_Set_AndDrawsWithMSFormsLayout()
    {
        var go = MSFormsReader.Read(Bytes("AllControls.storage")).Form.DescendantsAndSelf().Single(control => control.Name == "Go");
        Assert.IsFalse(go.Properties.IsStored("PicturePosition"));

        var button = Find(AllControls(), "Go");
        var picture = UI.Run(() =>
        {
            var red = Png(16, 12, [.. Enumerable.Repeat(0xFFFF0000u, 192)]);
            var control = new RDFormsCommandButton(button.Set(P.ButtonStyle, ButtonStyle.Graphical).Set(P.Picture, red));
            var (window, _) = Controls.ControlHost.Show((control, new Rect(0, 0, 200, 93)));
            Dispatcher.UIThread.RunJobs();
            var bounds = control.PictureBounds;
            window.Close();
            return bounds;
        });

        Assert.IsTrue(button.IsSet(P.PicturePosition));
        Assert.AreEqual(PicturePosition.AboveCenter, button.Get(P.PicturePosition));
        Assert.AreEqual(new Size(16, 12), picture?.Size);
    }

    [TestMethod]
    public void AMessage_IsInTheCurrentUICulture()
    {
        var culture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-CA");
            var french = AllControls(Site("Entry", s => s with { ControlSource = "Sheet1!A1" }))
                .Diagnostics.Single(d => d.Kind == MSFormsImportDiagnosticKind.UnresolvedDataSource);

            StringAssert.StartsWith(french.Message, "La propriété ControlSource de Entry est conservée sous forme de texte. ");
            StringAssert.EndsWith(french.Message, french.Port!.MessageIn(new CultureInfo("fr-CA")));
        }
        finally
        {
            CultureInfo.CurrentUICulture = culture;
        }
    }

    [TestMethod]
    public void ACitedCompanionThatIsMissing_IsReported_AndTheFormHasNoControls()
    {
        var import = MSFormsFormReader.Read(Text("Resources.frm"));

        Assert.AreEqual("Resources", import.Form!.Root.Get(P.Caption));
        Assert.IsEmpty(import.Form.Root.Contents);
        var reported = import.Diagnostics.Single();
        Assert.AreEqual(MSFormsImportDiagnosticKind.MissingCompanion, reported.Kind);
        StringAssert.Contains(reported.Message, "Resources.frx");
    }

    [TestMethod]
    public void AUserFormThatCitesNoCompanion_IsReported()
    {
        var import = MSFormsFormReader.Read(UserForm("UserForm1", "Caption = \"UserForm1\""), [1, 2, 3]);

        Assert.IsNotNull(import.Form);
        Assert.AreEqual(MSFormsImportDiagnosticKind.MissingCompanion, import.Diagnostics.Single().Kind);
    }

    [TestMethod]
    public void DesignerDataThatIsNoStorage_IsReportedAsUnreadable()
    {
        var companion = Patched(Bytes("Resources.frx"), [0xD0, 0xCF, 0x11, 0xE0], [0, 0, 0, 0]);
        var import = MSFormsFormReader.Read(Text("Resources.frm"), companion);

        Assert.IsEmpty(import.Form!.Root.Contents);
        Assert.AreEqual(MSFormsImportDiagnosticKind.UnreadableDesignerData, import.Diagnostics.Single().Kind);
    }

    [TestMethod]
    [DataRow("VERSION 5.00\r\nBegin VB.Form Form1 \r\nEnd\r\n")]
    [DataRow("VERSION 5.00\r\nBegin {C62A69F0-16DC-11CE-9E98-00AA00574A4F} \r\nEnd\r\n")]
    public void ADesignerBlockThatIsNoUserForm_HoldsNoForm(string text)
    {
        var import = MSFormsFormReader.Read(text);

        Assert.IsNull(import.Form);
        Assert.AreEqual(MSFormsImportDiagnosticKind.NotAUserForm, import.Diagnostics.Single().Kind);
        Assert.AreEqual(MSFormsImportSeverity.Error, import.Diagnostics.Single().Severity);
    }

    [TestMethod]
    public void AFileWithNoDesignerBlock_HoldsNoForm()
    {
        var import = MSFormsFormReader.Read("Attribute VB_Name = \"Module1\"\r\nOption Explicit\r\n");

        Assert.IsNull(import.Form);
        Assert.AreEqual(MSFormsImportDiagnosticKind.NoDesignerBlock, import.Diagnostics.Single().Kind);
    }

    [TestMethod]
    public void AMalformedLine_IsReportedWithItsNumber_AndLeftOut()
    {
        var import = MSFormsFormReader.Read(UserForm("UserForm1", "Caption = \"UserForm1\"", "this is not a property"));

        var reported = import.Diagnostics.Single(d => d.Kind == MSFormsImportDiagnosticKind.MalformedLine);
        Assert.AreEqual(MSFormsImportSeverity.Warning, reported.Severity);
        StringAssert.StartsWith(reported.Message, "Line 4: ");
        Assert.AreEqual("UserForm1", import.Form!.Root.Get(P.Caption));
    }

    // a ported UserForm is an RDForms.Form, so it takes an Icon, which its window shows.
    [TestMethod]
    public void APortedUserFormsIcon_IsShown()
    {
        var shown = UI.Run(() =>
        {
            var form = MSFormsFormReader.ReadFile(Fixture("Resources.frm")).Form!;
            Assert.AreSame(ComponentClasses.Form, form.Root.Class);
            form.Root.Set(P.Icon, Icon(1, Png(2, 2)));
            var window = new RDFormsFormWindow(form);
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var icon = window.Icon;
            window.Close();
            return icon;
        });

        Assert.IsNotNull(shown);
    }
}
