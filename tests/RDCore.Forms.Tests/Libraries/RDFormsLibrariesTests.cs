using RDCore.Forms.Libraries;
using RDCore.Forms.Model;
using RDCore.Forms.Porting;
using RDCore.SDK.Model.Symbols;
using RDCore.SDK.Model.Symbols.Abstract;
using RDCore.SDK.Model.Symbols.VBProject;
using RDCore.SDK.Runtime.Libraries;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Tests.Libraries;

// The VB library RDForms describes: VB6.OLB's export, every member of it kept, with what the model adds marked as RD-VBA's.
[TestClass]
public sealed class RDFormsLibrariesTests
{
    // What RDForms adds to VB6.OLB. A member the model gains that VB6.OLB does not declare fails the step test until it is listed here.
    private static readonly string[] Additions =
    [
        "PictureBox.Opacity", "PictureBox.ContextMenu",
        "Label.Picture", "Label.PicturePosition", "Label.Opacity", "Label.ContextMenu",
        "Label.BeforeDragOver", "Label.BeforeDropOrPaste", "Label.Error",
        "TextBox.ControlSource", "TextBox.Opacity", "TextBox.ContextMenu",
        "TextBox.BeforeUpdate", "TextBox.AfterUpdate", "TextBox.BeforeDragOver", "TextBox.BeforeDropOrPaste", "TextBox.Error",
        "Frame.ScrollBars", "Frame.KeepScrollBarsVisible", "Frame.ScrollHeight", "Frame.ScrollWidth", "Frame.ScrollLeft", "Frame.ScrollTop",
        "Frame.Opacity", "Frame.ContextMenu", "Frame.Scroll",
        "Frame.BeforeDragOver", "Frame.BeforeDropOrPaste", "Frame.Error",
        "CommandButton.Opacity", "CommandButton.ContextMenu",
        "CommandButton.Validate", "CommandButton.BeforeDragOver", "CommandButton.BeforeDropOrPaste", "CommandButton.Error",
        "CheckBox.ControlSource", "CheckBox.Opacity", "CheckBox.ContextMenu",
        "CheckBox.BeforeUpdate", "CheckBox.AfterUpdate", "CheckBox.BeforeDragOver", "CheckBox.BeforeDropOrPaste", "CheckBox.Error",
        "OptionButton.ControlSource", "OptionButton.Opacity", "OptionButton.ContextMenu",
        "OptionButton.BeforeUpdate", "OptionButton.AfterUpdate", "OptionButton.BeforeDragOver", "OptionButton.BeforeDropOrPaste", "OptionButton.Error",
        "ComboBox.ItemIcon", "ComboBox.ControlSource", "ComboBox.RowSource", "ComboBox.Opacity",
        "ComboBox.BeforeUpdate", "ComboBox.AfterUpdate", "ComboBox.BeforeDragOver", "ComboBox.BeforeDropOrPaste", "ComboBox.Error",
        "ListBox.ItemIcon", "ListBox.ControlSource", "ListBox.RowSource", "ListBox.Opacity", "ListBox.ContextMenu",
        "ListBox.BeforeUpdate", "ListBox.AfterUpdate", "ListBox.BeforeDragOver", "ListBox.BeforeDropOrPaste", "ListBox.Error",
        "HScrollBar.ControlSource", "HScrollBar.Opacity",
        "HScrollBar.BeforeUpdate", "HScrollBar.AfterUpdate", "HScrollBar.BeforeDragOver", "HScrollBar.BeforeDropOrPaste", "HScrollBar.Error",
        "VScrollBar.ControlSource", "VScrollBar.Opacity",
        "VScrollBar.BeforeUpdate", "VScrollBar.AfterUpdate", "VScrollBar.BeforeDragOver", "VScrollBar.BeforeDropOrPaste", "VScrollBar.Error",
        "Form.ContextMenu", "Form.ScrollBars", "Form.KeepScrollBarsVisible", "Form.ScrollHeight", "Form.ScrollWidth", "Form.ScrollLeft",
        "Form.ScrollTop", "Form.Scroll",
        "Form.BeforeDragOver", "Form.BeforeDropOrPaste", "Form.Error",
        "DriveListBox.Opacity",
        "DirListBox.Opacity", "DirListBox.ContextMenu",
        "FileListBox.Opacity", "FileListBox.ContextMenu",
        "Menu.CheckOnClick", "Menu.Picture",
        "App.PlatformVersion", "App.ComponentVersion", "App.KeepSystemTheme",
        "Shape.Opacity",
        "Line.Opacity",
        "Image.Opacity", "Image.ContextMenu",
        "Image.BeforeDragOver", "Image.BeforeDropOrPaste", "Image.Error",
        "Data.Opacity", "Data.ContextMenu",
        "VBControlExtender.Opacity",
    ];

    // The settings of VB6's designer the model holds on a root and VB6.OLB declares no member for: no member of the description.
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

    // A class's members and events as the file states them: a description's arrays compare by reference.
    private static string Json(IEnumerable<MemberDescription> members, IEnumerable<EventDescription> events) =>
        LibraryJson.Write(new LibraryDescription { Name = "L", Classes = [new ClassDescription { Name = "C", Members = [.. members], Events = [.. events] }] });

    [TestMethod]
    public void TheLibraries_AreVBMSComctlLibMSFormsAndRDForms_AndAProjectsReferenceToVB6Olb_FindsVB()
    {
        CollectionAssert.AreEqual(new[] { "VB", "MSComctlLib", "MSForms", "RDForms" }, RDFormsLibraries.Instance.Names.ToArray());
        Assert.IsFalse(RDFormsLibraries.Instance.TryGet("VBRUN", out _));
        Assert.IsTrue(((ILibrarySource)RDFormsLibraries.Instance).TryGetByGuid(Guid.Parse("FCFB3D2E-A0FA-1068-A738-08002B3371B5"), out var vb));
        Assert.AreSame(VB(), vb);
        Assert.AreEqual("6.0", vb.Origin!.Version);
    }

    // MSComctlLib

    private static LibraryDescription CommonControls()
    {
        Assert.IsTrue(RDFormsLibraries.Instance.TryGet("mscomctllib", out var library));
        return library;
    }

    private static string EmbeddedText(string name)
    {
        using var stream = typeof(RDFormsLibraries).Assembly.GetManifestResourceStream($"RDCore.Forms.Libraries.{name}.json")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [TestMethod]
    public void AProjectsReferenceToMSComctlOcx_FindsMSComctlLib_TheSameDescriptionEachTime()
    {
        Assert.IsTrue(((ILibrarySource)RDFormsLibraries.Instance).TryGetByGuid(Guid.Parse("831FDD16-0C5C-11D2-A9FC-0000F8754DA1"), out var library));

        Assert.AreSame(CommonControls(), library);
        Assert.AreEqual(("MSComctlLib", "2.0", "MSCOMCTL.OCX"), (library.Name, library.Origin!.Version, library.Origin.File));
        Assert.AreEqual("Microsoft Windows Common Controls 6.0", library.Description);
    }

    [TestMethod]
    public void MSComctlLib_IsTheOcxsExportAsWritten_WithNoAddition()
    {
        var library = CommonControls();

        Assert.AreEqual(LibraryJson.Write(LibraryJson.Read(EmbeddedText("MSComctlLib"))), LibraryJson.Write(library));
        Assert.IsFalse(library.Classes.Any(declared => declared.IsAddition || declared.Members.Any(m => m.IsAddition) || declared.Events.Any(e => e.IsAddition)));
        Assert.AreEqual((32, 42), (library.Classes.Length, library.Enums.Length));
        Assert.AreEqual(new LibraryIdentity("stdole", Guid.Parse("00020430-0000-0000-C000-000000000046"), 2, 0), library.DependsOn.Single());
    }

    // The members the ImageList's run time implements, as MSCOMCTL.OCX declares them.
    [TestMethod]
    [DataRow("ImageList", "ImageHeight ImageWidth MaskColor UseMaskColor ListImages hImageList BackColor Overlay AboutBox")]
    [DataRow("ListImages", "Count ControlDefault Add Clear Item Remove _NewEnum")]
    [DataRow("ListImage", "Index Key Tag Picture Draw ExtractIcon")]
    public void TheImageListsClasses_HaveTheOcxsMembers(string className, string members)
    {
        var declared = CommonControls().Classes.Single(c => c.Name == className);

        Assert.AreEqual(members, string.Join(" ", declared.Members.Select(m => m.Name).Distinct()));
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
        // as text, so that a failure shows the list the library has
        => Assert.AreEqual(string.Join("\n", Additions), string.Join("\n", RDFormsLibraries.Instance.Additions));

    // The step test: a member a running program reaches on a modelled class is in the description, an addition where VB6.OLB lacks it.
    [TestMethod]
    public void EveryMemberAProgramReaches_IsDescribed()
    {
        var vb = VB();
        var exported = Exported();
        var extender = UserControlClasses.Of(new FormDefinition(new ComponentInstance(ComponentClasses.UserControl, "UserControl1")
            .Set(P.CanGetFocus, true).Set(P.DefaultCancel, true).Set(P.Alignable, true)), "Project1");
        // the model's classes that VB6's port map names a class of VB6.OLB for
        var ported = FormsPortMap.VB6.Types.Where(port => port.Kind == TypePortKind.Class)
            .Select(port => (port.LegacyName, Modelled: ComponentClasses.ByTypeName(port.RDFormsName)))
            .Where(pair => pair.Modelled is not null)
            .Select(pair => (pair.LegacyName, pair.Modelled!));
        foreach (var (name, modelled) in ported.Append(("VBControlExtender", extender)))
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
            // An added method or window property takes the arguments the window takes, and only a window property has accessors.
            foreach (var method in modelled.Methods)
            {
                var accessors = described.Members.Where(candidate => candidate.IsAddition && string.Equals(candidate.Name, method.Name, StringComparison.OrdinalIgnoreCase));
                foreach (var accessor in accessors)
                {
                    var arguments = accessor.Kind is MemberKind.PropertyLet or MemberKind.PropertySet ? accessor.Parameters.Length - 1 : accessor.Parameters.Length;
                    Assert.AreEqual(method.Parameters.Count, arguments, $"{name}.{method.Name} {accessor.Kind}");
                    Assert.AreEqual(method.Value is null, accessor.Kind == MemberKind.Method, $"{name}.{method.Name} {accessor.Kind}");
                }
            }
        }
    }

    [TestMethod]
    public void AppsVersions_AreReadOnlyStrings_TheComponentsByItsName()
    {
        var app = VB().Classes.Single(declared => declared.Name == "App");

        var platform = app.Members.Single(member => member.Name == "PlatformVersion");
        var component = app.Members.Single(member => member.Name == "ComponentVersion");

        Assert.AreEqual((MemberKind.PropertyGet, "String", 0, true), (platform.Kind, platform.Type, platform.Parameters.Length, platform.IsAddition));
        Assert.AreEqual((MemberKind.PropertyGet, "String", true), (component.Kind, component.Type, component.IsAddition));
        var name = component.Parameters.Single();
        Assert.AreEqual(("Name", "String", true, false), (name.Name, name.Type, name.IsByVal, name.IsOptional));
    }

    [TestMethod]
    public void AppsKeepSystemTheme_IsABooleanAProgramReadsAndWrites()
    {
        var app = VB().Classes.Single(declared => declared.Name == "App");

        var accessors = app.Members.Where(member => member.Name == "KeepSystemTheme").ToArray();

        CollectionAssert.AreEqual(new[] { MemberKind.PropertyGet, MemberKind.PropertyLet }, accessors.Select(member => member.Kind).ToArray());
        Assert.IsTrue(accessors.All(member => member.IsAddition));
        Assert.AreEqual("Boolean", accessors[0].Type);
        var value = accessors[1].Parameters.Single();
        Assert.AreEqual(("Boolean", true), (value.Type, value.IsByVal));
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

    // ItemIcon(Index), the window's, is a picture as VB6's Picture is: a stdole.StdPicture read, let and set, after the item's index, as ItemData's.
    [TestMethod]
    [DataRow("ListBox")]
    [DataRow("ComboBox")]
    public void ItemIcon_IsAnItemsPicture_ByItsIndex_WithTheAccessorsOfVB6sPicture(string type)
    {
        var described = VB().Classes.Single(declared => declared.Name == type);
        var picture = VB().Classes.Single(declared => declared.Name == "PictureBox").Members.Where(member => member.Name == "Picture").ToArray();

        var itemIcon = described.Members.Where(member => member.Name == "ItemIcon").ToArray();

        CollectionAssert.AreEqual(picture.Select(member => (member.Kind, member.Type)).ToArray(), itemIcon.Select(member => (member.Kind, member.Type)).ToArray());
        Assert.IsTrue(itemIcon.All(member => member.IsAddition));
        foreach (var accessor in itemIcon)
        {
            Assert.AreEqual("Index Integer", $"{accessor.Parameters[0].Name} {accessor.Parameters[0].Type}", $"{accessor.Kind}");
            var value = picture.Single(member => member.Kind == accessor.Kind).Parameters;
            CollectionAssert.AreEqual(value.Select(parameter => parameter.Type).ToArray(), accessor.Parameters.Skip(1).Select(parameter => parameter.Type).ToArray(), $"{accessor.Kind}");
        }
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

        Assert.AreEqual(vb.Classes.Sum(declared => declared.Members.Count(member => member.IsAddition) + declared.Events.Count(raised => raised.IsAddition)),
            written.Split("\"isAddition\"").Length - 1);
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

    // RDForms

    private static LibraryDescription RDForms()
    {
        Assert.IsTrue(RDFormsLibraries.Instance.TryGet("rdforms", out var library));
        return library;
    }

    private static readonly string[] Intrinsic =
        ["Boolean", "Byte", "Integer", "Long", "LongLong", "LongPtr", "Single", "Double", "Currency", "Date", "String", "Variant", "Object", "OLE_COLOR"];

    private static LibraryDescription MSForms()
    {
        Assert.IsTrue(RDFormsLibraries.Instance.TryGet("msforms", out var library));
        return library;
    }

    private static ClassDescription Own(string name) => RDForms().Classes.Single(declared => declared.Name == name);

    private static bool Has(ClassDescription declared, string member) => declared.Members.Any(candidate => candidate.Name == member);

    [TestMethod]
    public void AProjectsReferenceToFM20_FindsMSForms_TheExportAsWritten()
    {
        Assert.IsTrue(((ILibrarySource)RDFormsLibraries.Instance).TryGetByGuid(Guid.Parse("0D452EE1-E08F-101A-852E-02608C4D0BB4"), out var library));

        Assert.AreSame(MSForms(), library);
        Assert.AreEqual(("MSForms", "2.0", "FM20.DLL"), (library.Name, library.Origin!.Version, library.Origin.File));
        Assert.AreEqual(LibraryJson.Write(LibraryJson.Read(EmbeddedText("MSForms"))), LibraryJson.Write(library));
        Assert.HasCount(42, library.Enums);
    }

    [TestMethod]
    public void RDForms_HasAClassForEachOfTheModels_ThenForEachClassAPortMapPortsTo_AndNoTypeLibrary()
    {
        var library = RDForms();
        var names = library.Classes.Select(declared => declared.Name).ToArray();

        Assert.AreSame(library, RDForms());
        CollectionAssert.AreEqual(ComponentClasses.All.Select(modelled => modelled.ClassName).ToArray(), names.Take(ComponentClasses.All.Count).ToArray());
        CollectionAssert.AreEquivalent(new[] { "Control", "Controls", "Tab", "Tabs", "Pages", "DataObject", "ReturnEffect", "Font" },
            names.Skip(ComponentClasses.All.Count).ToArray());
        Assert.IsNull(library.Origin);
        Assert.IsTrue(library.Classes.All(declared => declared.DefaultInterface is null));
    }

    [TestMethod]
    public void RDForms_DependsOnVBAndMSForms_AndOnWhatTheyDependOn()
        => CollectionAssert.AreEqual(new[] { "VB", "DAO", "MSDATASRC", "StdFormat", "stdole", "VBRUN", "MSForms" },
            RDForms().DependsOn.Select(dependency => dependency.Name).ToArray());

    [TestMethod]
    public void RDForms_DeclaresMSFormsEnumerations_AsFM20DeclaresThem()
    {
        var written = LibraryJson.Write(new LibraryDescription { Name = "L", Enums = RDForms().Enums });

        Assert.AreEqual(LibraryJson.Write(new LibraryDescription { Name = "L", Enums = MSForms().Enums }), written);
        var scrollAction = RDForms().Enums.Single(declared => declared.Name == "fmScrollAction");
        Assert.AreEqual(0L, scrollAction.Members.Single(member => member.Name == "fmScrollActionNoChange").Value);
    }

    [TestMethod]
    [DataRow("Form")]
    [DataRow("TextBox")]
    [DataRow("Label")]
    [DataRow("Menu")]
    [DataRow("UserControl")]
    public void APortedClass_HasTheMembersAndEventsVBDescribes_First(string name)
    {
        var described = VB().Classes.Single(declared => declared.Name == name);
        var own = Own(name);
        var members = described.Members.Select(member => member.Name).Distinct().ToArray();

        CollectionAssert.AreEqual(members, own.Members.Select(member => member.Name).Distinct().Take(members.Length).ToArray());
        CollectionAssert.AreEqual(described.Events.Select(raised => raised.Name).ToArray(), own.Events.Take(described.Events.Length).Select(raised => raised.Name).ToArray());
        Assert.AreEqual((described.Description, described.IsCreatable), (own.Description, own.IsCreatable));
    }

    // Where VB's class and MSForms' have a member of one name, it is writable where either is: the Property Let only MSForms has, of VB's
    // type, with the dispatch identifier of VB's Property Get. VB's class keeps it read-only.
    [TestMethod]
    [DataRow("TextBox", "Name", "String")]
    [DataRow("Label", "Name", "String")]
    [DataRow("TextBox", "MultiLine", "Boolean")]
    [DataRow("TextBox", "HideSelection", "Boolean")]
    [DataRow("TextBox", "ScrollBars", "Integer")]
    [DataRow("Form", "ScrollBars", "Integer")]
    [DataRow("Frame", "ScrollBars", "Integer")]
    [DataRow("ListBox", "IntegralHeight", "Boolean")]
    [DataRow("ListBox", "MultiSelect", "Integer")]
    [DataRow("ComboBox", "Style", "Integer")]
    public void AMemberWritableInEitherLibrary_IsWritable_OfVBsType(string className, string member, string type)
    {
        var accessors = Own(className).Members.Where(candidate => candidate.Name == member).ToList();

        var let = accessors.Single(candidate => candidate.Kind == MemberKind.PropertyLet);
        Assert.AreEqual(type, let.Parameters.Single().Type);
        Assert.AreEqual(accessors.Single(candidate => candidate.Kind == MemberKind.PropertyGet).DispId, let.DispId);
        Assert.IsFalse(VB().Classes.Single(declared => declared.Name == className).Members
            .Any(candidate => candidate.Name == member && candidate.Kind == MemberKind.PropertyLet));
    }

    // A control's Name Let renames only a control a program added; the description says so, as Microsoft Forms 2.0 refuses it (382) on a control of
    // the form's file.
    [TestMethod]
    [DataRow("TextBox")]
    [DataRow("Label")]
    [DataRow("CommandButton")]
    public void AControlsNameLet_SaysItNamesOnlyAControlAProgramAdded(string className)
    {
        var let = Own(className).Members.Single(candidate => candidate.Name == "Name" && candidate.Kind == MemberKind.PropertyLet);

        StringAssert.Contains(let.Description, "Controls.Add");
        StringAssert.Contains(let.Description, "382");
    }

    // The wider parameter list wins: MSForms' List takes a row and a column, both optional, and is a Variant; its AddItem needs no item; its
    // Move takes five optional arguments. The dispatch identifiers are VB's.
    [TestMethod]
    [DataRow("ListBox", "List", MemberKind.PropertyGet, "0 2")]
    [DataRow("ListBox", "List", MemberKind.PropertyLet, "1 3")]
    [DataRow("ComboBox", "List", MemberKind.PropertyGet, "0 2")]
    [DataRow("ListBox", "AddItem", MemberKind.Method, "0 2")]
    [DataRow("ComboBox", "AddItem", MemberKind.Method, "0 2")]
    [DataRow("TextBox", "Move", MemberKind.Method, "0 5")]
    [DataRow("Image", "Move", MemberKind.Method, "0 5")]
    public void TheWiderParameterList_Wins_WithVBsDispatchIdentifier(string className, string member, MemberKind kind, string requiredAndTotal)
    {
        var own = Own(className).Members.Single(candidate => candidate.Name == member && candidate.Kind == kind);
        var vb = VB().Classes.Single(declared => declared.Name == className).Members.Single(candidate => candidate.Name == member && candidate.Kind == kind);

        var required = own.Parameters.Count(parameter => !parameter.IsOptional && !parameter.IsParamArray);
        Assert.AreEqual(requiredAndTotal, $"{required} {own.Parameters.Length}");
        Assert.AreEqual(vb.DispId, own.DispId);
        if (kind == MemberKind.PropertyGet)
            Assert.AreEqual("Variant", own.Type);
    }

    // Where neither parameter list is wider, VB's stands: Selected and RemoveItem take an Integer index as VB's do.
    [TestMethod]
    [DataRow("ListBox", "Selected", "Integer")]
    [DataRow("ListBox", "RemoveItem", "Integer")]
    public void WhereNeitherParameterListIsWider_VBsStands(string className, string member, string type)
        => Assert.IsTrue(Own(className).Members.Where(candidate => candidate.Name == member).All(candidate => candidate.Parameters[0].Type == type));

    // A double click's handler takes Cancel, as MSForms' does; VB's class raises it with none, for a Visual Basic 6.0 form's handler.
    [TestMethod]
    [DataRow("Form")]
    [DataRow("TextBox")]
    [DataRow("ListBox")]
    [DataRow("Image")]
    public void DblClick_TakesCancel_AsMSFormsDoes(string className)
    {
        var cancel = Own(className).Events.Single(raised => raised.Name == "DblClick").Parameters.Single();

        Assert.AreEqual(("Cancel", "Boolean", false), (cancel.Name, cancel.Type, cancel.IsByVal));
        Assert.IsEmpty(VB().Classes.Single(declared => declared.Name == className).Events.Single(raised => raised.Name == "DblClick").Parameters);
    }

    // A class both legacy libraries port to: what a ported UserForm's code reaches on it under MSForms' port.
    [TestMethod]
    public void AClassBothLibrariesPortTo_HasFM20sMembers_UnderTheNamesThePortGivesThem()
    {
        Assert.IsTrue(Has(Own("TextBox"), "LineCount"));
        Assert.IsTrue(Has(Own("TextBox"), "ToolTipText"));
        Assert.IsFalse(Has(Own("TextBox"), "ControlTipText"));
        Assert.IsFalse(Has(Own("Form"), "InsideWidth"));
    }

    [TestMethod]
    public void AClassVBPortsTo_KeepsVBsDefaultMember_AndNoClassHasTwo()
    {
        static string[] Defaults(ClassDescription declared) => [.. declared.Members.Where(member => member.DispId == 0).Select(member => member.Name)];
        var vb = VB().Classes.ToDictionary(declared => declared.Name);
        foreach (var port in FormsPortMap.VB6.Types.Where(port => port.Kind == TypePortKind.Class && vb.ContainsKey(port.LegacyName)))
        {
            var own = Own(port.RDFormsName["RDForms.".Length..]);
            if (string.Equals(own.Name, port.LegacyName, StringComparison.OrdinalIgnoreCase))
                CollectionAssert.AreEqual(Defaults(vb[port.LegacyName]), Defaults(own), own.Name);
        }
        foreach (var declared in RDForms().Classes)
            Assert.IsLessThanOrEqualTo(1, Defaults(declared).Distinct(StringComparer.OrdinalIgnoreCase).Count(), declared.Name);
    }

    [TestMethod]
    public void AControlOfMSForms_HasFM20sMembers_AndItsExtenders_UnderThePortsNames()
    {
        var toggle = Own("ToggleButton");

        Assert.IsTrue(Has(toggle, "TripleState"));
        Assert.IsTrue(Has(toggle, "BoundValue"));
        Assert.IsTrue(Has(toggle, "OldLeft"));
        Assert.IsTrue(Has(toggle, "ToolTipText"));
        Assert.IsFalse(Has(toggle, "ControlTipText"));
        Assert.IsTrue(toggle.Events.Any(raised => raised.Name == "Validate"));
        Assert.IsFalse(toggle.Events.Any(raised => raised.Name == "Exit"));
    }

    [TestMethod]
    [DataRow("DataObject", "Clear GetFormat GetText SetText PutInClipboard GetFromClipboard StartDrag")]
    [DataRow("ReturnEffect", "Value")]
    [DataRow("Tab", "Caption ToolTipText Enabled Index Name Tag Visible")]
    [DataRow("Font", "Name Size Bold Italic Underline Strikethrough Weight Charset")]
    public void AHelperClassOfMSForms_HasFM20sMembers(string className, string members)
        => Assert.AreEqual(members, string.Join(" ", Own(className).Members.Select(member => member.Name).Distinct()));

    [TestMethod]
    [DataRow("Form", "ActiveControl", "Control")]
    [DataRow("TextBox", "Parent", "Object")]
    [DataRow("Image", "Parent", "Object")]
    [DataRow("Form", "Controls", "Object")]
    [DataRow("TextBox", "Text", "String")]
    [DataRow("TextBox", "BorderStyle", "Integer")]
    [DataRow("ListBox", "ListIndex", "Integer")]
    [DataRow("CommandButton", "FontSize", "Single")]
    [DataRow("TextBox", "Font", "stdole.StdFont")]
    [DataRow("PictureBox", "Picture", "stdole.StdPicture")]
    [DataRow("Label", "Caption", "String")]
    [DataRow("ToggleButton", "Font", "Font")]
    [DataRow("MultiPage", "SelectedItem", "Page")]
    [DataRow("Frame", "SpecialEffect", "fmSpecialEffect")]
    [DataRow("SpinButton", "Orientation", "fmOrientation")]
    public void AMembersType_IsRDFormsOwn_OrQualifiedWithItsLibrary(string className, string member, string type)
        => Assert.AreEqual(type, Own(className).Members.First(candidate => candidate.Name == member).Type);

    [TestMethod]
    public void NothingInRDForms_IsAnAddition()
        => Assert.IsFalse(RDForms().Classes.Any(declared => declared.IsAddition
            || declared.Members.Any(member => member.IsAddition) || declared.Events.Any(raised => raised.IsAddition)));

    // The step test of the classes no class of VB ports to: what a running program reaches on the model's class is in the description.
    [TestMethod]
    public void AClassVBPortsNoClassTo_HasWhatAProgramReachesOnTheModelsClass()
    {
        var ported = FormsPortMap.VB6.Types.Where(port => port.Kind == TypePortKind.Class).Select(port => port.RDFormsName).ToHashSet();
        var unported = ComponentClasses.All.Where(modelled => !ported.Contains(modelled.TypeName)).ToList();
        Assert.IsNotEmpty(unported);
        foreach (var modelled in unported)
        {
            var own = Own(modelled.ClassName);
            var reached = modelled.Properties.Where(property => !modelled.IsNotAvailableAtRunTime(property)).Select(property => property.Name)
                .Concat(modelled.Methods.Select(method => method.Name));
            foreach (var member in reached)
                Assert.IsTrue(own.Members.Any(candidate => string.Equals(candidate.Name, member, StringComparison.OrdinalIgnoreCase)), $"{modelled.ClassName}.{member}");
            foreach (var raised in modelled.Events)
                Assert.IsTrue(own.Events.Any(candidate => string.Equals(candidate.Name, raised.Name, StringComparison.OrdinalIgnoreCase)), $"{modelled.ClassName}.{raised.Name}");
        }
    }

    [TestMethod]
    public void AToggleButtonsValue_IsItsDefault_AndAProgramReadsAndWritesIt()
    {
        var value = Own("ToggleButton").Members.Where(member => member.Name == "Value").ToList();

        CollectionAssert.AreEqual(new[] { MemberKind.PropertyGet, MemberKind.PropertyLet }, value.Select(member => member.Kind).ToArray());
        Assert.IsTrue(value.All(member => member.DispId == 0));
        Assert.AreEqual(1, Own("ToggleButton").Members.Where(member => member.DispId == 0).Select(member => member.Name).Distinct().Count());
    }

    [TestMethod]
    public void RDForms_WritesAndReadsBackTheSame()
    {
        var written = LibraryJson.Write(RDForms());

        Assert.AreEqual(written, LibraryJson.Write(LibraryJson.Read(written)));
        Assert.IsFalse(written.Contains("\"isAddition\"", StringComparison.Ordinal));
    }

    [TestMethod]
    public void APortedFormsSymbol_HasItsCaptionAndShow()
    {
        var symbols = new LibrarySymbolReader(new Uri("file:///c:/ws/")).Read([RDForms()]).ToArray();
        var form = symbols.OfType<VBClassModuleSymbol>().Single(declared => declared.Name == "Form");

        Assert.IsTrue(form.Members.Any(member => member.Name == "Caption"));
        Assert.IsTrue(form.Members.Any(member => member.Name == "Show"));
    }

    // Every type a member or an event of RDForms names: an intrinsic type, a type of RDForms, a type of VB or of MSForms qualified with its
    // library, or a type of another library they depend on, qualified with it.
    [TestMethod]
    public void EveryTypeRDFormsNames_IsDescribed()
    {
        var library = RDForms();
        var own = library.Classes.Select(declared => declared.Name).Concat(library.Enums.Select(declared => declared.Name)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var vb = VB().Classes.Select(declared => declared.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var msforms = MSForms().Classes.Select(declared => declared.Name).Concat(MSForms().Enums.Select(declared => declared.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var libraries = library.DependsOn.Select(dependency => dependency.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        bool Of(string library, string name) => string.Equals(library, name, StringComparison.OrdinalIgnoreCase);
        bool Described(string type) => Intrinsic.Contains(type, StringComparer.OrdinalIgnoreCase) || own.Contains(type)
            || type.Split('.') is [var qualifier, var name]
                && (Of(qualifier, RDFormsLibraries.RDForms) ? own.Contains(name)
                    : Of(qualifier, RDFormsLibraries.VB) ? vb.Contains(name)
                    : Of(qualifier, RDFormsLibraries.MSForms) ? msforms.Contains(name)
                    : libraries.Contains(qualifier));

        var types = library.Classes
            .SelectMany(declared => declared.Members.SelectMany(member => member.Parameters.Select(parameter => parameter.Type).Append(member.Type))
                .Concat(declared.Events.SelectMany(raised => raised.Parameters.Select(parameter => parameter.Type))))
            .OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase);
        Assert.AreEqual(string.Empty, string.Join(" ", types.Where(type => !Described(type)).Order()));
    }
}
