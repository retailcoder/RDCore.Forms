using System.Collections;
using System.Globalization;
using RDCore.Forms.Model;
using RDCore.Forms.Xaml;

namespace RDCore.Forms.Tests.Model;

// Whether two forms are the same document: the same component tree, each component of the same class, name and index, with the same
// value of every property its class has, the same text kept for what the model does not have, and its contents in the same order; and
// the same control libraries. A reader's golden tests compare what it imports with what the XAML format gives back: any reader, VB6's
// or MSForms', plugs its own samples in.
internal static class FormModelAssert
{
    // Writes the form as a document, reads it back, and asserts that the document is read with nothing reported and is the same form,
    // and that the form read back is written as the same document. Returns the form read back.
    public static FormDefinition RoundTripsThroughXaml(FormDefinition form)
    {
        var written = FormXaml.Write(form);

        var read = FormXaml.Read(written);

        Assert.IsNotNull(read.Form, string.Join(Environment.NewLine, read.Diagnostics));
        Assert.IsEmpty(read.Diagnostics, string.Join(Environment.NewLine, read.Diagnostics));
        SameDocument(form, read.Form);
        Assert.AreEqual(written, FormXaml.Write(read.Form), "The form read back is not written as the document it was read from.");
        return read.Form;
    }

    // Fails with every difference between the two forms, each naming the component and the property it is about.
    public static void SameDocument(FormDefinition expected, FormDefinition actual)
    {
        var differences = Differences(expected, actual);
        if (differences.Count > 0)
            Assert.Fail($"The forms differ in {differences.Count} place(s):{Environment.NewLine}{string.Join(Environment.NewLine, differences)}");
    }

    // Every difference between the two forms, one line each, as "Form1/Frame1/Text1.Caption: expected "a", was "b"".
    public static IReadOnlyList<string> Differences(FormDefinition expected, FormDefinition actual)
    {
        var found = new List<string>();
        Compare(expected.Root, actual.Root, expected.Root.ToString(), found);
        if (!expected.ControlLibraries.SequenceEqual(actual.ControlLibraries))
            found.Add($"{expected.Root}.ControlLibraries: expected [{string.Join(", ", expected.ControlLibraries)}], was [{string.Join(", ", actual.ControlLibraries)}]");
        return found;
    }

    // Whether two property values are the same: a picture by its bytes, a list item by item, anything else by its equality.
    public static bool Same(object? expected, object? actual) => (expected, actual) switch
    {
        (null, null) => true,
        (null, _) or (_, null) => false,
        (byte[] a, byte[] b) => a.AsSpan().SequenceEqual(b),
        (IEnumerable a, IEnumerable b) when a is not string && b is not string =>
            a.Cast<object?>().Count() == b.Cast<object?>().Count() && a.Cast<object?>().Zip(b.Cast<object?>()).All(p => Same(p.First, p.Second)),
        _ => expected.Equals(actual),
    };

    // A value as a difference shows it.
    public static string Show(object? value) => value switch
    {
        null => "Nothing",
        string text => $"\"{text}\"",
        byte[] bytes => $"{bytes.Length} bytes {Convert.ToHexString(bytes, 0, Math.Min(bytes.Length, 8))}",
        IEnumerable items => $"[{string.Join(", ", items.Cast<object?>().Select(Show))}]",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };

    private static void Compare(ComponentInstance expected, ComponentInstance actual, string path, List<string> found)
    {
        if (!ReferenceEquals(expected.Class, actual.Class))
        {
            found.Add($"{path}: expected a {expected.Class.TypeName}, was a {actual.Class.TypeName}");
            return;
        }

        if (!string.Equals(expected.Name, actual.Name, StringComparison.Ordinal))
            found.Add($"{path}.Name: expected \"{expected.Name}\", was \"{actual.Name}\"");
        if (expected.Index != actual.Index)
            found.Add($"{path}.Index: expected {Show(expected.Index)}, was {Show(actual.Index)}");
        foreach (var property in expected.Class.Properties)
        {
            // A property the component's other values leave it without, such as a dropdown list's SelStart, has no value to compare.
            var had = expected.TryGet(property, out var before);
            var has = actual.TryGet(property, out var after);
            if (had != has)
                found.Add($"{path}.{property.Name}: expected {(had ? Show(before) : "no value")}, was {(has ? Show(after) : "no value")}");
            else if (!Same(before, after))
                found.Add($"{path}.{property.Name}: expected {Show(before)}, was {Show(after)}");
        }

        if (!expected.UnmodelledProperties.SequenceEqual(actual.UnmodelledProperties))
            found.Add($"{path}.UnmodelledProperties: expected [{string.Join(", ", expected.UnmodelledProperties)}], was [{string.Join(", ", actual.UnmodelledProperties)}]");
        if (!expected.UnmodelledContents.SequenceEqual(actual.UnmodelledContents))
            found.Add($"{path}.UnmodelledContents: expected [{string.Join(", ", expected.UnmodelledContents.Select(c => c.Name))}], was [{string.Join(", ", actual.UnmodelledContents.Select(c => c.Name))}]");

        var children = expected.Contents.Select(c => c.ToString()).ToList();
        var others = actual.Contents.Select(c => c.ToString()).ToList();
        if (!children.SequenceEqual(others, StringComparer.Ordinal))
            found.Add($"{path}: expected the contents [{string.Join(", ", children)}], were [{string.Join(", ", others)}]");

        // Each component is compared with the one of its name and index, wherever it is, so that one out of order is not also reported
        // as different in every value.
        foreach (var child in expected.Contents)
        {
            if (actual.Contents.FirstOrDefault(c => c.Index == child.Index && string.Equals(c.Name, child.Name, StringComparison.Ordinal)) is { } other)
                Compare(child, other, $"{path}/{child}", found);
        }
    }
}
