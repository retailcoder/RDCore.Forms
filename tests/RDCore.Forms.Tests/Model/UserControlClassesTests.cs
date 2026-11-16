using RDCore.Forms.Import.VB6;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Tests.Model;

[TestClass]
public sealed class UserControlClassesTests
{
    // VB6's extender members, and RD-VBA's Opacity.
    private const string Visible = "Left Top Width Height Visible Opacity DragMode ToolTipText WhatsThisHelpID Tag";
    private const string Focus = "TabIndex TabStop CausesValidation HelpContextID";

    private static string Ctl(params string[] settings) =>
        string.Join("\r\n", [
            "VERSION 5.00",
            "Begin VB.UserControl Gauge ",
            "   ClientHeight    =   1200",
            "   ClientWidth     =   2400",
            .. settings.Select(s => "   " + s),
            "End",
            "Attribute VB_Name = \"Gauge\"",
            ""]);

    private static FormDefinition Read(params string[] settings)
    {
        var import = VB6FormReader.Read(Ctl(settings));
        Assert.IsEmpty(import.Diagnostics);
        return import.Form!;
    }

    private static string[] Names(IEnumerable<string> names) => [.. names.Order(StringComparer.Ordinal)];

    [TestMethod]
    public void AUserControlRoot_HasTheDefaultsOfVB6sPropertiesWindow()
    {
        var root = VBClasses.UserControl;

        Assert.AreEqual(BackStyle.Opaque, root.DefaultOf(P.BackStyle));
        Assert.AreEqual(FormColor.FromSystem(SystemColor.ButtonFace), root.DefaultOf(P.MaskColor));
        Assert.AreEqual(PaletteMode.Container, root.DefaultOf(P.PaletteMode));
        Assert.IsTrue(root.DefaultOf(P.CanGetFocus));
        Assert.IsTrue(root.DefaultOf(P.HasDC));
        Assert.IsTrue(root.DefaultOf(P.ClipControls));
        Assert.AreEqual(ClipBehavior.UseRegion, root.DefaultOf(P.ClipBehavior));
        Assert.AreEqual(HitBehavior.UseRegion, root.DefaultOf(P.HitBehavior));
        Assert.AreEqual(FillStyle.Transparent, root.DefaultOf(P.FillStyle));
        Assert.AreEqual((short)13, root.DefaultOf(P.DrawMode));
        Assert.AreEqual("", root.DefaultOf(P.AccessKeys));
        Assert.IsTrue(root.DefaultOf(P.FontTransparent));
        foreach (var off in new[] { P.Alignable, P.ControlContainer, P.DefaultCancel, P.EditAtDesignTime, P.ForwardFocus, P.InvisibleAtRuntime,
                     P.Public, P.Windowless, P.AutoRedraw, P.KeyPreview, P.RightToLeft })
        {
            Assert.IsFalse(root.DefaultOf(off), off.Name);
        }
    }

    [TestMethod]
    public void AUserControlsSettings_AreReadTyped()
    {
        var root = Read("AccessKeys      =   \"q\"", "Alignable       =   -1  'True", "BackStyle       =   0  'Transparent",
            "CanGetFocus     =   0   'False", "ControlContainer=   -1  'True", "DataBindingBehavior=   1  'vbSimpleBound",
            "DefaultCancel   =   -1  'True", "ForwardFocus    =   -1  'True", "InvisibleAtRuntime=   -1  'True",
            "Windowless      =   -1  'True", "HitBehavior     =   2  'Use Paint", "PaletteMode     =   4  'None",
            "FontTransparent =   0   'False").Root;

        Assert.AreEqual("q", root.Get(P.AccessKeys));
        Assert.IsTrue(root.Get(P.Alignable));
        Assert.AreEqual(BackStyle.Transparent, root.Get(P.BackStyle));
        Assert.IsFalse(root.Get(P.CanGetFocus));
        Assert.IsTrue(root.Get(P.ControlContainer));
        Assert.AreEqual(DataBindingBehavior.SimpleBound, root.Get(P.DataBindingBehavior));
        Assert.IsTrue(root.Get(P.DefaultCancel));
        Assert.IsTrue(root.Get(P.ForwardFocus));
        Assert.IsTrue(root.Get(P.InvisibleAtRuntime));
        Assert.IsTrue(root.Get(P.Windowless));
        Assert.AreEqual(HitBehavior.UsePaint, root.Get(P.HitBehavior));
        Assert.AreEqual(PaletteMode.None, root.Get(P.PaletteMode));
        Assert.IsFalse(root.Get(P.FontTransparent));
        Assert.IsEmpty(root.UnmodelledProperties);
    }

    [TestMethod]
    public void APlacedUserControl_IsNamedAfterItsProjectAndItself()
    {
        var placed = UserControlClasses.Of(Read(), "Project1");

        Assert.AreEqual("Project1.Gauge", placed.TypeName);
        Assert.AreEqual("Gauge", placed.DefaultName);
        Assert.AreEqual(ComponentKind.Control, placed.Kind);
        Assert.IsNull(placed.DefaultMember);
    }

    [TestMethod]
    [DataRow(null, Visible + " " + Focus, true, DisplayName = "plain")]
    [DataRow("CanGetFocus     =   0   'False", Visible, false, DisplayName = "CanGetFocus False")]
    [DataRow("ForwardFocus    =   -1  'True", Visible + " TabIndex", false, DisplayName = "ForwardFocus True")]
    [DataRow("InvisibleAtRuntime=   -1  'True", "Left Top", false, DisplayName = "InvisibleAtRuntime True")]
    [DataRow("DefaultCancel   =   -1  'True", Visible + " " + Focus + " Default Cancel", true, DisplayName = "DefaultCancel True")]
    [DataRow("Alignable       =   -1  'True", Visible + " " + Focus + " Align Negotiate", true, DisplayName = "Alignable True")]
    public void APlacedUserControlsExtender_HasTheMembersItsSettingsGiveIt(string? setting, string properties, bool takesFocus)
    {
        var placed = UserControlClasses.Of(setting is null ? Read() : Read(setting), "Project1");

        CollectionAssert.AreEqual(Names(properties.Split(' ')), Names(placed.Properties.Select(p => p.Name)));
        CollectionAssert.AreEqual(takesFocus ? new[] { "SetFocus" } : [], placed.Methods.Select(m => m.Name).ToArray());
        CollectionAssert.AreEqual(takesFocus ? new[] { "GotFocus", "LostFocus" } : [], placed.Events.Select(e => e.Name).ToArray());
        Assert.IsNull(placed.Property("Enabled"));
    }

    // As measured with VB6: an Alignable control's Negotiate is False in a form's file, and reading it at run time is error 393.
    [TestMethod]
    public void AnAlignableUserControlsNegotiate_IsFalse_AndNotAvailableAtRunTime()
    {
        var placed = UserControlClasses.Of(Read("Alignable       =   -1  'True"), "Project1");

        Assert.IsFalse(placed.DefaultOf(P.Negotiate));
        Assert.IsTrue(placed.IsNotAvailableAtRunTime(P.Negotiate));
    }

    [TestMethod]
    public void AnInvisibleUserControl_HasNoExtent_AndItsPlaceIsNotAvailableAtRunTime()
    {
        var placed = UserControlClasses.Of(Read("InvisibleAtRuntime=   -1  'True"), "Project1");

        Assert.IsFalse(placed.HasExtent);
        Assert.IsTrue(placed.IsNotAvailableAtRunTime(P.Left));
        Assert.IsTrue(placed.IsNotAvailableAtRunTime(P.Top));
    }

    [TestMethod]
    public void AVisibleUserControl_HasAnExtent_AndItsPlaceIsAvailableAtRunTime()
    {
        var placed = UserControlClasses.Of(Read(), "Project1");

        Assert.IsTrue(placed.HasExtent);
        Assert.IsFalse(placed.IsNotAvailableAtRunTime(P.Left));
    }

    [TestMethod]
    public void AUserControlsContainerAndWindowlessSettings_AreItsPlacedControlsOwn()
    {
        var plain = UserControlClasses.Of(Read(), "Project1");
        var set = UserControlClasses.Of(Read("ControlContainer=   -1  'True", "Windowless      =   -1  'True"), "Project1");

        Assert.IsFalse(plain.IsContainer);
        Assert.IsFalse(plain.IsWindowless);
        Assert.IsTrue(set.IsContainer);
        Assert.IsTrue(set.IsWindowless);
    }

    [TestMethod]
    public void APlacedUserControl_RaisesTheEventsItsCodeDeclares_ThenItsFocusEvents()
    {
        var placed = UserControlClasses.Of(Read(), "Project1", [new EventDefinition("Changed")]);

        CollectionAssert.AreEqual(new[] { "Changed", "GotFocus", "LostFocus" }, placed.Events.Select(e => e.Name).ToArray());
    }

    [TestMethod]
    public void AFormRoot_HasNoPlacedClass()
    {
        var form = VB6FormReader.Read("VERSION 5.00\r\nBegin VB.Form Form1 \r\nEnd\r\n").Form!;

        Assert.ThrowsExactly<ArgumentException>(() => UserControlClasses.Of(form, "Project1"));
    }

    private const string PlacingForm = """
        VERSION 5.00
        Begin VB.Form Form1
           ClientHeight    =   3000
           ClientWidth     =   4000
           Begin Project1.Gauge Gauge1
              Height          =   600
              Left            =   120
              TabIndex        =   0
              Top             =   240
              Width           =   1200
              _ExtentX        =   2117
              _ExtentY        =   1058
              Level           =   42
           End
        End
        Attribute VB_Name = "Form1"

        """;

    [TestMethod]
    public void AFormPlacingAProjectsUserControl_ReadsItAsAnInstanceOfItsClass()
    {
        var gauge = UserControlClasses.Of(Read(), "Project1");

        var import = VB6FormReader.Read(PlacingForm, options: new VB6ImportOptions { Classes = [gauge] });

        Assert.IsEmpty(import.Diagnostics);
        var placed = import.Form!.Find("Gauge1")!;
        Assert.AreSame(gauge, placed.Class);
        // In pixels, at 15 twips a pixel.
        Assert.AreEqual(8, placed.Get(P.Left));
        Assert.AreEqual(80, placed.Get(P.Width));
        Assert.AreEqual((short)0, placed.Get(P.TabIndex));
        CollectionAssert.AreEqual(new[] { "_ExtentX", "_ExtentY", "Level" }, placed.UnmodelledProperties.Select(p => p.Name).ToArray());
    }

    [TestMethod]
    public void AFormPlacingAUserControlItsOptionsDoNotHave_KeepsItAsText()
    {
        var import = VB6FormReader.Read(PlacingForm);

        Assert.AreEqual(VB6ImportDiagnosticKind.UnmodelledClass, import.Diagnostics.Single().Kind);
        Assert.IsNull(import.Form!.Find("Gauge1"));
        Assert.AreEqual("Project1.Gauge", import.Form.Root.UnmodelledContents.Single().TypeName);
    }
}
