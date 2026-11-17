using RDCore.Forms.Import.VB6;
using RDCore.Forms.Menus;
using RDCore.Forms.Model;
using RDCore.Forms.Tests.Model;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Tests.Xaml;

// The golden round trips: a form goes from its file to the model, to a document of the XAML format, and back to the same model.
[TestClass]
public sealed class GoldenRoundTripTests
{
    private static readonly string Fixtures = Path.Combine(AppContext.BaseDirectory, "Import", "VB6", "Fixtures");

    // Every form file the import tests read, by its path under their fixtures.
    public static IEnumerable<object[]> ImportFixtures =>
        Directory.EnumerateFiles(Fixtures, "*.*", SearchOption.AllDirectories)
            .Where(f => Path.GetExtension(f).ToLowerInvariant() is ".frm" or ".ctl" or ".pag" or ".dob")
            .Order(StringComparer.Ordinal)
            .Select(f => new object[] { Path.GetRelativePath(Fixtures, f) });

    public static IEnumerable<object[]> EveryClass => ComponentClasses.All.Select(c => new object[] { c.TypeName });

    [TestMethod]
    public void TheImportTests_HaveFormsToRoundTrip() => Assert.IsGreaterThanOrEqualTo(8, ImportFixtures.Count());

    [TestMethod]
    [DynamicData(nameof(ImportFixtures))]
    public void AnImportedForm_ComesBackFromItsDocument_AsTheSameModel(string fixture)
    {
        var import = VB6FormReader.ReadFile(Path.Combine(Fixtures, fixture));

        Assert.IsNotNull(import.Form, fixture);
        FormModelAssert.RoundTripsThroughXaml(import.Form);
    }

    [TestMethod]
    [DynamicData(nameof(EveryClass))]
    public void AComponentWithEveryPropertyOffItsDefault_ComesBackFromItsDocument_AsTheSameModel(string typeName)
    {
        var type = ComponentClasses.ByTypeName(typeName)!;
        var (form, component) = FormWith(type);

        var unvaried = SetEveryPropertyOffItsDefault(component);

        Assert.IsEmpty(unvaried, $"No value off its default was taken by {string.Join(", ", unvaried)}.");
        FormModelAssert.RoundTripsThroughXaml(form);
    }

    [TestMethod]
    public void AValueThatDiffers_IsReported_WithItsComponentAndProperty()
    {
        var (form, _) = FormWith(ComponentClasses.TextBox);
        var copy = form.Copy();
        copy.Find("Sample1")!.Set(P.Text, "changed");

        var differences = FormModelAssert.Differences(form, copy);

        CollectionAssert.AreEqual(new[] { "Form1/Sample1.Text: expected \"\", was \"changed\"" }, differences.ToArray());
        var failure = Assert.ThrowsExactly<AssertFailedException>(() => FormModelAssert.SameDocument(form, copy));
        StringAssert.Contains(failure.Message, "Form1/Sample1.Text");
    }

    [TestMethod]
    public void ContentsInAnotherOrder_AComponentOfAnotherIndex_AndTextKeptForTheModel_AreReported()
    {
        var root = new ComponentInstance(ComponentClasses.Form, "Form1");
        new ComponentInstance(ComponentClasses.Label, "Label1").SetContainer(root);
        new ComponentInstance(ComponentClasses.CommandButton, "Command1", 0).SetContainer(root);
        var form = new FormDefinition(root);
        var copy = form.Copy();
        copy.Root.Contents[1].SetContainer(copy.Root, 0);
        copy.Root.Contents[0].Index = 1;
        copy.Root.UnmodelledProperties.Add(new UnmodelledProperty("Odd", "x"));

        var differences = FormModelAssert.Differences(form, copy);

        Assert.HasCount(2, differences, string.Join(Environment.NewLine, differences));
        StringAssert.StartsWith(differences[0], "Form1.UnmodelledProperties: ");
        Assert.AreEqual("Form1: expected the contents [Label1, Command1(0)], were [Command1(1), Label1]", differences[1]);
    }

    [TestMethod]
    public void TheSameForm_HasNoDifference()
    {
        var form = VB6FormReader.ReadFile(Path.Combine(Fixtures, "Intrinsics.frm")).Form!;

        Assert.IsEmpty(FormModelAssert.Differences(form, form.Copy()));
    }

    // A form holding one component of the class, named Sample1, or a root of the class itself. A control goes in the form where the form
    // can hold it, and otherwise in the first container class that can.
    private static (FormDefinition Form, ComponentInstance Component) FormWith(ComponentClass type)
    {
        if (type.Kind == ComponentKind.Root)
        {
            var only = new ComponentInstance(type, "Form1");
            return (new FormDefinition(only), only);
        }

        var root = new ComponentInstance(ComponentClasses.Form, "Form1");
        var component = new ComponentInstance(type, "Sample1");
        if (!TryPlace(component, root))
        {
            var holder = ComponentClasses.All
                .Where(c => c.Kind == ComponentKind.Control && c.IsContainer)
                .Select(c => new ComponentInstance(c, "Holder1"))
                .First(h => TryPlace(h, root) && (TryPlace(component, h) || Unplace(h)));
            Assert.AreSame(holder, component.Container);
        }

        return (new FormDefinition(root), component);
    }

    private static bool TryPlace(ComponentInstance component, ComponentInstance container)
    {
        try
        {
            component.SetContainer(container);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool Unplace(ComponentInstance component)
    {
        component.SetContainer(null);
        return false;
    }

    // Sets each of the component's properties, in its class's order, to the first value off its default that it takes. Returns the
    // properties that took none. A run-time-only property is not part of a document, so it is left as it is. Align goes last: a docked
    // box keeps the place its layout gives it, in place of the one set.
    private static List<string> SetEveryPropertyOffItsDefault(ComponentInstance component)
    {
        var unvaried = new List<string>();
        foreach (var property in component.Class.Properties.Where(p => !p.RunTimeOnly).OrderBy(p => ReferenceEquals(p, P.Align)))
        {
            var varied = false;
            foreach (var candidate in Candidates(property, component.Class.DefaultOf(property)))
            {
                if (FormModelAssert.Same(candidate, component.Class.DefaultOf(property)) || !component.TrySet(property, candidate))
                    continue;
                if (!FormModelAssert.Same(component.GetBoxed(property), component.Class.DefaultOf(property)))
                {
                    varied = true;
                    break;
                }
            }

            if (!varied)
                unvaried.Add($"{property.Name} ({property.ValueType.Name})");
        }

        return unvaried;
    }

    // Values of the property's type, in the order to try them, from the default of the component's class.
    private static IEnumerable<object?> Candidates(PropertyDefinition property, object? origin)
    {
        var type = Nullable.GetUnderlyingType(property.ValueType) ?? property.ValueType;
        if (type == typeof(bool))
            return [!(bool)(origin ?? false)];
        if (type.IsEnum)
            return [.. Enum.GetValues(type).Cast<object>().Reverse()];
        if (type == typeof(string))
            return [$"{property.Name} <é & \"x\">", "x", "1", "C:\\"];
        if (type == typeof(double))
            return [.. new[] { 12.5, 1, 0.75, 100, 2 }.Select(d => (object)((origin is double o ? o : 0) + d)), 0.5];
        if (type == typeof(int))
            return [.. new[] { 3, 1, 2, 100 }.Select(d => (object)((origin is int o ? o : 0) + d)), -1];
        if (type == typeof(short))
            return [.. new[] { 3, 1, 2, 100 }.Select(d => (object)(short)((origin is short o ? o : 0) + d)), (short)-1];
        if (type == typeof(byte))
            return [.. new[] { 3, 1, 2 }.Select(d => (object)(byte)((origin is byte o ? o : 0) + d))];
        if (type == typeof(FormColor))
            return [FormColor.FromRgb(0x12, 0x34, 0x56), FormColor.FromSystem(SystemColor.Highlight)];
        if (type == typeof(FormFont))
            return [new FormFont("Arial", 11.25, FormFont.BoldWeight, Italic: true, Underline: true, Strikethrough: true, Charset: 1)];
        if (type == typeof(byte[]))
            return [TestPictures.Gif1x1, TestPictures.Bitmap2x2, TestPictures.Icon(1, TestPictures.PngHeader(16, 16))];
        if (type == typeof(MenuShortcut))
            return [new MenuShortcut(79, ShortcutModifiers.Ctrl)];
        if (type == typeof(IReadOnlyList<string>))
            return [(IReadOnlyList<string>)["a", "b <&>"]];
        if (type == typeof(IReadOnlyList<int>))
            return [(IReadOnlyList<int>)[7, -5]];
        if (type == typeof(IReadOnlyList<byte[]?>))
            return [(IReadOnlyList<byte[]?>)[null, TestPictures.Gif1x1]];
        if (type == typeof(IReadOnlyList<Tab>))
            return [(IReadOnlyList<Tab>)[new("First", "&One <é>") { ToolTipText = "tip \"x\"", Tag = "t" }, new("Second", "") { Enabled = false, Visible = false }]];
        return [];
    }
}
