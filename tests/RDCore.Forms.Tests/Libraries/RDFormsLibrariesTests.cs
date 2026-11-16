using RDCore.Forms.Libraries;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using RDCore.SDK.Model.Symbols;
using RDCore.SDK.Model.Symbols.Abstract;
using RDCore.SDK.Model.Symbols.VBProject;
using RDCore.SDK.Runtime.Libraries;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Tests.Libraries;

// The VB library RDForms describes: VB6.OLB's export, every member of it kept, with what the model adds marked as RD-VBA's.
[TestClass]
public sealed class RDFormsLibrariesTests
{
    // what RDForms adds to VB6.OLB. A member the model gains that VB6.OLB does not declare fails the step test until it is listed here.
    private static readonly string[] Additions =
    [
        "PictureBox.Opacity", "PictureBox.ContextMenu",
        "Label.Picture", "Label.PicturePosition", "Label.Opacity", "Label.ContextMenu",
        "TextBox.Opacity", "TextBox.ContextMenu",
        "Frame.Opacity", "Frame.ContextMenu",
        "CommandButton.Opacity", "CommandButton.ContextMenu",
        "CheckBox.Opacity", "CheckBox.ContextMenu",
        "OptionButton.Opacity", "OptionButton.ContextMenu",
        "ComboBox.ItemIcon", "ComboBox.Opacity",
        "ListBox.ItemIcon", "ListBox.Opacity", "ListBox.ContextMenu",
        "HScrollBar.Opacity",
        "VScrollBar.Opacity",
        "Form.ContextMenu",
        "DriveListBox.Opacity",
        "DirListBox.Opacity", "DirListBox.ContextMenu",
        "FileListBox.Opacity", "FileListBox.ContextMenu",
        "Menu.CheckOnClick", "Menu.Picture",
        "Shape.Opacity",
        "Line.Opacity",
        "Image.Opacity", "Image.ContextMenu",
        "Data.Opacity", "Data.ContextMenu",
        "VBControlExtender.Opacity",
    ];

    // the settings of VB6's designer the model holds on a root and VB6.OLB declares no member for: no member of the description.
    private static readonly string[] DesignerSettings =
    [
        "UserControl.Alignable", "UserControl.CanGetFocus", "UserControl.ControlContainer", "UserControl.DataBindingBehavior", "UserControl.DataSourceBehavior",
        "UserControl.DefaultCancel", "UserControl.EditAtDesignTime", "UserControl.ForwardFocus", "UserControl.InvisibleAtRuntime", "UserControl.Public",
        "UserControl.ToolboxBitmap", "UserControl.Windowless", "UserControl.Left", "UserControl.Top", "PropertyPage.Left", "PropertyPage.Top",
    ];

    private static LibraryDescription VB()
    {
        Assert.IsTrue(RDFormsLibraries.Instance.TryGet("vb", out var vb));
        return vb;
    }

    private static LibraryDescription Exported()
    {
        using var stream = typeof(RDFormsLibraries).Assembly.GetManifestResourceStream("RDCore.Forms.Libraries.VB.json")!;
        using var reader = new StreamReader(stream);
        return LibraryJson.Read(reader.ReadToEnd());
    }

    // a class's members and events as the file states them: a description's arrays compare by reference.
    private static string Json(IEnumerable<MemberDescription> members, IEnumerable<EventDescription> events) =>
        LibraryJson.Write(new LibraryDescription { Name = "L", Classes = [new ClassDescription { Name = "C", Members = [.. members], Events = [.. events] }] });

    [TestMethod]
    public void TheOnlyLibrary_IsVB_AndAProjectsReferenceToVB6Olb_FindsIt()
    {
        CollectionAssert.AreEqual(new[] { "VB" }, RDFormsLibraries.Instance.Names.ToArray());
        Assert.IsFalse(RDFormsLibraries.Instance.TryGet("VBRUN", out _));
        Assert.IsTrue(((ILibrarySource)RDFormsLibraries.Instance).TryGetByGuid(Guid.Parse("FCFB3D2E-A0FA-1068-A738-08002B3371B5"), out var vb));
        Assert.AreSame(VB(), vb);
        Assert.AreEqual("6.0", vb.Origin!.Version);
    }

    [TestMethod]
    public void EveryMemberVB6OlbDeclares_IsKept_AndIsNoAddition()
    {
        var vb = VB();
        foreach (var exported in Exported().Classes)
        {
            var described = vb.Classes.Single(declared => declared.Name == exported.Name);
            Assert.AreEqual(Json(exported.Members, exported.Events),
                Json(described.Members.Where(member => !member.IsAddition), described.Events.Where(raised => !raised.IsAddition)), exported.Name);
            Assert.IsFalse(described.IsAddition);
        }
        CollectionAssert.AreEqual(Exported().Classes.Select(declared => declared.Name).ToArray(), vb.Classes.Select(declared => declared.Name).ToArray());
        Assert.AreEqual(LibraryJson.Write(Exported() with { Classes = [] }), LibraryJson.Write(vb with { FormatVersion = Exported().FormatVersion, Classes = [] }));
    }

    [TestMethod]
    public void TheAdditions_AreRDFormsList()
        => CollectionAssert.AreEqual(Additions, RDFormsLibraries.Instance.Additions.ToArray());

    // the step test: a member a running program reaches on a modelled class is in the description, an addition where VB6.OLB lacks it.
    [TestMethod]
    public void EveryMemberAProgramReaches_IsDescribed()
    {
        var vb = VB();
        var exported = Exported();
        var extender = UserControlClasses.Of(new FormDefinition(new ComponentInstance(VBClasses.UserControl, "UserControl1")
            .Set(P.CanGetFocus, true).Set(P.DefaultCancel, true).Set(P.Alignable, true)), "Project1");
        foreach (var (name, modelled) in VBClasses.All.Select(type => (type.TypeName[3..], type)).Append(("VBControlExtender", extender)))
        {
            var described = vb.Classes.Single(declared => declared.Name == name);
            var declaredByVB6 = exported.Classes.Single(declared => declared.Name == name);
            var reached = modelled.Properties.Where(property => !modelled.IsNotAvailableAtRunTime(property)).Select(property => property.Name)
                .Concat(modelled.Methods.Select(method => method.Name));
            foreach (var member in reached.Where(member => !DesignerSettings.Contains($"{name}.{member}")))
            {
                var found = described.Members.Where(candidate => string.Equals(candidate.Name, member, StringComparison.OrdinalIgnoreCase)).ToList();
                Assert.IsNotEmpty(found, $"{name}.{member}");
                var inVB6 = declaredByVB6.Members.Any(candidate => string.Equals(candidate.Name, member, StringComparison.OrdinalIgnoreCase));
                Assert.IsTrue(found.All(candidate => candidate.IsAddition != inVB6), $"{name}.{member}");
            }
            foreach (var raised in modelled.Events)
                Assert.IsTrue(described.Events.Any(candidate => string.Equals(candidate.Name, raised.Name, StringComparison.OrdinalIgnoreCase)), $"{name}.{raised.Name}");
        }
    }

    [TestMethod]
    public void AnAddition_TakesTheTypeAndAccessorsOfVB6OlbsMemberOfItsName_OrTheModels()
    {
        var label = VB().Classes.Single(declared => declared.Name == "Label");
        var pictureBox = VB().Classes.Single(declared => declared.Name == "PictureBox");

        CollectionAssert.AreEqual(pictureBox.Members.Where(member => member.Name == "Picture").Select(member => (member.Kind, member.Type)).ToArray(),
            label.Members.Where(member => member.Name == "Picture").Select(member => (member.Kind, member.Type)).ToArray());
        var opacity = label.Members.Where(member => member.Name == "Opacity").ToArray();
        CollectionAssert.AreEqual(new[] { MemberKind.PropertyGet, MemberKind.PropertyLet }, opacity.Select(member => member.Kind).ToArray());
        Assert.AreEqual("Double", opacity[0].Type);
        Assert.AreEqual("Double", opacity[1].Parameters.Single().Type);
        Assert.IsTrue(opacity[1].Parameters.Single().IsByVal);
    }

    [TestMethod]
    public void WhatAProgramCannotReach_IsNoAddition()
    {
        var classes = VB().Classes;
        bool Has(string type, string member) => classes.Single(declared => declared.Name == type).Members.Any(candidate => candidate.Name == member);

        foreach (var setting in DesignerSettings)
            Assert.IsFalse(Has(setting.Split('.')[0], setting.Split('.')[1]), setting);
        Assert.IsFalse(Has("MDIForm", "Refresh"));
        Assert.IsFalse(Has("Menu", "Shortcut"));
    }

    [TestMethod]
    public void TheDescription_WritesItsAdditions_AndReadsBackTheSame()
    {
        var vb = VB();
        var written = LibraryJson.Write(vb);

        Assert.AreEqual(vb.Classes.Sum(declared => declared.Members.Count(member => member.IsAddition)), written.Split("\"isAddition\"").Length - 1);
        var label = vb.Classes.Single(declared => declared.Name == "Label");
        var read = LibraryJson.Read(written).Classes.Single(declared => declared.Name == "Label");
        Assert.AreEqual(Json(label.Members, label.Events), Json(read.Members, read.Events));
    }

    [TestMethod]
    public void AnAdditionsSymbol_IsALibraryAddition()
    {
        var symbols = new LibrarySymbolReader(new Uri("file:///c:/ws/")).Read([VB()]).ToArray();
        var label = symbols.OfType<VBClassModuleSymbol>().Single(declared => declared.Name == "Label");

        Assert.IsTrue(label.Members.Where(member => member.Name == "Opacity").All(member => member.GetProperty(SymbolProperties.LibraryAddition)));
        Assert.IsFalse(label.Members.Where(member => member.Name == "Caption").Any(member => member.GetProperty(SymbolProperties.LibraryAddition)));
    }
}
