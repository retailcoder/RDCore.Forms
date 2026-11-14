using RDCore.Forms.Import.VB6;
using RDCore.Forms.Menus;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;

namespace RDCore.Forms.Tests.Import.VB6;

// Intrinsics.frm, UserScale.frm and Misplaced.frm, with Intrinsics.frx, are hand-written; VB6 compiled them and a program it built
// wrote what it read from each into vb6-read.txt. An expectation is a value of that file, except where a test says it is the
// designer file's. Where the import reads a value other than the one VB6 read, the test says so, and for the Timer's offset an
// ignored test holds VB6's.
[TestClass]
public sealed class VB6FixtureTests
{
    private static readonly Lazy<FormDefinition> intrinsics = new(() => Read("Intrinsics.frm").Form!);

    private static FormDefinition Intrinsics => intrinsics.Value;

    private static VB6FormImport Read(params string[] path) =>
        VB6FormReader.ReadFile(Path.Combine([AppContext.BaseDirectory, "Import", "VB6", "Fixtures", .. path]));

    private static ComponentInstance Component(string name, int? index = null) =>
        Intrinsics.Find(name, index) ?? throw new AssertFailedException($"{name} is not in the form.");

    private static (double, double, double, double) Bounds(ComponentInstance component) =>
        (component.Get(VBProperties.Left), component.Get(VBProperties.Top), component.Get(VBProperties.Width), component.Get(VBProperties.Height));

    [TestMethod]
    public void TheIntrinsicsForm_IsReadWithoutADiagnostic()
    {
        var import = Read("Intrinsics.frm");

        Assert.IsEmpty(import.Diagnostics, string.Join("; ", import.Diagnostics));
        Assert.AreEqual("Intrinsics", import.Form!.Name);
        Assert.StartsWith("Attribute VB_Name = \"Intrinsics\"", import.ModuleText);
    }

    [TestMethod]
    public void TheForm_HasEveryControlAndMenuVB6Counts()
    {
        Assert.HasCount(22, Intrinsics.Root.Descendants().ToArray());
    }

    [TestMethod]
    public void TheFormsOwnProperties_AreReadAsVB6ReadsThem()
    {
        var root = Intrinsics.Root;

        Assert.AreEqual("Intrinsic \"controls\"", root.Get(VBProperties.Caption));
        Assert.AreEqual("&H00C0FFC0&", root.Get(VBProperties.BackColor).ToString());
        Assert.AreEqual(new FormFont("Arial", 9.75, FormFont.BoldWeight, Italic: true), root.Get(VBProperties.Font));
        Assert.IsTrue(root.Get(VBProperties.KeyPreview));
        Assert.AreEqual("form tag", root.Get(VBProperties.Tag));
        Assert.AreEqual(StartUpPosition.CenterScreen, root.Get(VBProperties.StartUpPosition));
    }

    [TestMethod]
    public void TheFormsClientRectangle_IsItsPositionAndSize_AndItsOuterOneIsNotRead()
    {
        // the designer file's values: VB6 read a ScaleWidth of 7995, its window's client width at run time.
        // in pixels, from the file's twips.
        Assert.AreEqual((4d, 23d, 8000d / 15, 400d), Bounds(Intrinsics.Root));
        Assert.AreEqual(ScaleMode.Twip, Intrinsics.Root.Get(VBProperties.ScaleMode));
        Assert.AreEqual(8000d, Intrinsics.Root.Get(VBProperties.ScaleWidth));
    }

    [TestMethod]
    public void AControlArray_KeepsEachElementsIndex_InsideItsFrame()
    {
        var first = Component("optChoice", 0);
        var second = Component("optChoice", 1);

        Assert.AreSame(Component("fraOptions"), first.Container);
        Assert.AreEqual((8d, 24d, 2000d / 15, 17d), Bounds(first));
        Assert.IsTrue(first.Get(VBProperties.OptionButtonValue));
        Assert.IsFalse(second.Get(VBProperties.OptionButtonValue));
        Assert.AreEqual(1, first.Get(VBProperties.TabIndex));
        Assert.AreEqual("Second", second.Get(VBProperties.Caption));
    }

    [TestMethod]
    public void AControlThatWritesNoFont_TakesTheFormsFont()
    {
        Assert.AreEqual(Intrinsics.Root.Get(VBProperties.Font), Component("fraOptions").Get(VBProperties.Font));
        Assert.AreEqual(Intrinsics.Root.Get(VBProperties.Font), Component("chkFlag").Get(VBProperties.Font));
    }

    [TestMethod]
    public void AControlsOwnFont_IsReadFromItsBag()
    {
        var button = Component("cmdOK");

        Assert.AreEqual(new FormFont("Courier New", 12, Underline: true), button.Get(VBProperties.Font));
        Assert.AreEqual("&OK", button.Get(VBProperties.Caption));
        Assert.AreEqual("Accept", button.Get(VBProperties.ToolTipText));
    }

    [TestMethod]
    public void ACommandButtonsDefault_IsRead_AndItsCancelIsFalseWhereNotWritten()
    {
        var button = Component("cmdOK");

        Assert.IsTrue(button.Get(VBProperties.Default));
        Assert.IsFalse(button.Get(VBProperties.Cancel));
        Assert.IsEmpty(button.UnmodelledProperties);
    }

    [TestMethod]
    public void APropertyTheModelHasNot_IsKeptAsWritten()
    {
        Assert.Contains(new UnmodelledProperty("MultiLine", "-1  'True"), Component("txtNotes").UnmodelledProperties);
        Assert.Contains(new UnmodelledProperty("LinkTopic", "\"Intrinsics\""), Intrinsics.Root.UnmodelledProperties);
    }

    [TestMethod]
    public void AMenusShortcut_IsReadAsItsKeyAndModifiers()
    {
        Assert.AreEqual(new MenuShortcut(79, ShortcutModifiers.Ctrl), Component("mnuFileOpen").Get(VBProperties.Shortcut));
        Assert.IsEmpty(Component("mnuFileOpen").UnmodelledProperties);
    }

    [TestMethod]
    public void ACheckBox_ReadsItsGrayedValue_AndItsEnabledFlag()
    {
        Assert.AreEqual(CheckBoxValue.Grayed, Component("chkFlag").Get(VBProperties.CheckBoxValue));
        Assert.IsFalse(Component("chkFlag").Get(VBProperties.Enabled));
    }

    [TestMethod]
    public void AMultiLineText_IsReadFromItsLongStringRecord()
    {
        var text = Component("txtNotes");

        Assert.AreEqual("First line\r\nSecond line", text.Get(VBProperties.Text));
        Assert.AreEqual(SystemColor.InfoBackground, text.Get(VBProperties.BackColor).SystemColor);
        Assert.AreEqual("&H000000FF&", text.Get(VBProperties.ForeColor).ToString());
        Assert.IsTrue(text.Get(VBProperties.Locked));
    }

    [TestMethod]
    public void ACaption_IsReadFromItsShortStringRecord()
    {
        var label = Component("lblNote");

        Assert.AreEqual("Two\r\nlines", label.Get(VBProperties.Caption));
        Assert.AreEqual(TextAlignment.Center, label.Get(VBProperties.Alignment));
        Assert.AreEqual(BackStyle.Transparent, label.Get(VBProperties.BackStyle));
        Assert.AreEqual(BorderStyle.FixedSingle, label.Get(VBProperties.BorderStyle));
        Assert.IsFalse(label.Get(VBProperties.UseMnemonic));
    }

    [TestMethod]
    public void AListBoxsItems_AndTheirItemData_AreReadFromTheirRecords()
    {
        var list = Component("lstItems");

        CollectionAssert.AreEqual(new[] { "Alpha", "Beta", "Gamma" }, list.Get(VBProperties.List).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 20, -300 }, list.Get(VBProperties.ItemData).ToArray());
        Assert.AreEqual(ListBoxMultiSelect.Extended, list.Get(VBProperties.MultiSelect));
    }

    [TestMethod]
    public void AComboBoxsItems_AndTheirItemData_AreReadFromTheirRecords()
    {
        var combo = Component("cboStyle");

        CollectionAssert.AreEqual(new[] { "One", "Two" }, combo.Get(VBProperties.List).ToArray());
        CollectionAssert.AreEqual(new[] { 7, 8 }, combo.Get(VBProperties.ItemData).ToArray());
        Assert.AreEqual(ComboBoxStyle.DropdownList, combo.Get(VBProperties.ComboBoxStyle));
    }

    [TestMethod]
    public void APicture_IsTheImageFileItsRecordHolds()
    {
        var picture = Component("picLogo").Get(VBProperties.Picture);

        Assert.IsNotNull(picture);
        Assert.HasCount(70, picture);
        Assert.AreEqual((byte)'B', picture[0]);
        Assert.AreEqual((byte)'M', picture[1]);
        Assert.AreEqual(Appearance.Flat, Component("picLogo").Get(VBProperties.Appearance));
    }

    [TestMethod]
    public void AnEmptyPictureRecord_IsNoPicture()
    {
        var image = Component("imgEmpty");

        Assert.IsTrue(image.IsSet(VBProperties.Picture));
        Assert.IsNull(image.Get(VBProperties.Picture));
        Assert.IsTrue(image.Get(VBProperties.Stretch));
    }

    [TestMethod]
    public void APictureBox_HoldsTheControlsDrawnOnIt_AndHasAScaleOfItsOwn()
    {
        var inside = Component("cmdInside");

        Assert.AreSame(Component("picLogo"), inside.Container);
        Assert.AreEqual((8d, 8d, 41d, 25d), Bounds(inside));
        Assert.AreEqual(1425d, Component("picLogo").Get(VBProperties.ScaleWidth));
    }

    [TestMethod]
    public void ATimer_ReadsItsIntervalAndPlace()
    {
        var timer = Component("tmrTick");

        Assert.AreEqual(500, timer.Get(VBProperties.Interval));
        Assert.IsFalse(timer.Get(VBProperties.Enabled));
        Assert.AreEqual(496d, timer.Get(VBProperties.Left));
    }

    [TestMethod]
    public void TheScrollBars_ReadTheirRanges_AndKeepTheDefaultsTheFileLeavesOut()
    {
        var horizontal = Component("hsbValue");
        var vertical = Component("vsbValue");

        Assert.AreEqual((1, 10, 5, 3, 1), (horizontal.Get(VBProperties.Min), horizontal.Get(VBProperties.Max),
            horizontal.Get(VBProperties.ScrollBarValue), horizontal.Get(VBProperties.LargeChange), horizontal.Get(VBProperties.SmallChange)));
        Assert.AreEqual((0, 100, 0, 1, 5), (vertical.Get(VBProperties.Min), vertical.Get(VBProperties.Max),
            vertical.Get(VBProperties.ScrollBarValue), vertical.Get(VBProperties.LargeChange), vertical.Get(VBProperties.SmallChange)));
    }

    [TestMethod]
    public void AShapeAndALine_ReadTheirFiguresAndColours()
    {
        var shape = Component("shpBox");
        var line = Component("linDiagonal");

        Assert.AreEqual(ShapeType.RoundedRectangle, shape.Get(VBProperties.Shape));
        Assert.AreEqual("&H000000FF&", shape.Get(VBProperties.BorderColor).ToString());
        Assert.AreEqual(3, shape.Get(VBProperties.BorderWidth));
        Assert.AreEqual("&H0000FFFF&", shape.Get(VBProperties.FillColor).ToString());
        Assert.AreEqual(FillStyle.Solid, shape.Get(VBProperties.FillStyle));
        Assert.AreEqual((120d, 4200d, 3120d, 5400d),
            (line.Get(VBProperties.X1), line.Get(VBProperties.Y1), line.Get(VBProperties.X2), line.Get(VBProperties.Y2)));
        Assert.AreEqual("&H00FF0000&", line.Get(VBProperties.BorderColor).ToString());
        Assert.AreEqual(2, line.Get(VBProperties.BorderWidth));
    }

    [TestMethod]
    public void TheMenus_AreATreeUnderTheForm()
    {
        var file = Component("mnuFile");

        Assert.AreSame(Intrinsics.Root, file.Container);
        CollectionAssert.AreEqual(new[] { "mnuFileOpen", "mnuFileSep", "mnuFileExit" }, file.Contents.Select(c => c.Name).ToArray());
        Assert.AreEqual("-", Component("mnuFileSep").Get(VBProperties.Caption));
        Assert.IsTrue(Component("mnuFileExit").Get(VBProperties.Checked));
        Assert.IsFalse(Component("mnuWindow").Get(VBProperties.Enabled));
        Assert.IsTrue(Component("mnuWindow").Get(VBProperties.WindowList));
    }

    [TestMethod]
    public void AUserScale_IsTheForms_AndItsLinesEndsAreInIt()
    {
        var form = Read("UserScale.frm").Form!;
        var line = form.Find("linAcross")!;

        Assert.AreEqual((ScaleMode.User, -10d, -10d, 200d, 100d), (form.Root.Get(VBProperties.ScaleMode), form.Root.Get(VBProperties.ScaleLeft),
            form.Root.Get(VBProperties.ScaleTop), form.Root.Get(VBProperties.ScaleWidth), form.Root.Get(VBProperties.ScaleHeight)));
        // the designer file's ends, which the model keeps as written: VB6 read -0.0124842 0 100.0125 50.
        Assert.AreEqual((0d, 0d, 100d, 50d), (line.Get(VBProperties.X1), line.Get(VBProperties.Y1), line.Get(VBProperties.X2), line.Get(VBProperties.Y2)));
        Assert.AreEqual((4000d / 15, 200d), (form.Root.Get(VBProperties.Width), form.Root.Get(VBProperties.Height)));
    }

    [TestMethod]
    public void AControlInsideATimer_IsPlacedInTheForm_OffsetByTheTimersPosition()
    {
        var label = Read("Misplaced.frm").Form!.Find("lblInTimer")!;

        // VB6 read 720 4800: the label's 600 2400 and the Timer's 120 2400.
        Assert.AreEqual((48d, 320d, 81d, 17d), Bounds(label));
    }

    [TestMethod]
    public void AControlInsideAListBox_IsPlacedInTheNearestContainerThatHoldsIt_AtItsWrittenPosition()
    {
        var import = Read("Misplaced.frm");
        var form = import.Form!;

        Assert.AreSame(form.Root, form.Find("cmdInList")!.Container);
        Assert.AreEqual((8d, 8d, 81d, 25d), Bounds(form.Find("cmdInList")!));
        Assert.AreSame(form.Find("fraHost"), form.Find("cmdDeep")!.Container);
        Assert.AreEqual((4d, 4d, 60d, 25d), Bounds(form.Find("cmdDeep")!));
        Assert.AreSame(form.Root, form.Find("lblInTimer")!.Container);
        Assert.HasCount(3, import.Diagnostics);
        Assert.IsTrue(import.Diagnostics.All(d => d is { Kind: VB6ImportDiagnosticKind.MisplacedComponent, Severity: VB6ImportSeverity.Warning }));
        Assert.AreEqual(17, import.Diagnostics[0].Line);
    }
}
