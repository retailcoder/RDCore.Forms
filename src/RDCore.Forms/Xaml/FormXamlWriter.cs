using System.Globalization;
using System.Xml;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Xaml;

// Writes the model as a document: what FormXamlReader reads back.
internal static class FormXamlWriter
{
    private const string Ns = FormXaml.Namespace;

    public static void Write(XmlWriter writer, FormDefinition form)
    {
        var root = form.Root;
        writer.WriteStartElement(root.Class.ClassName, Ns);
        writer.WriteAttributeString("FormatVersion", FormXaml.FormatVersion.ToString(CultureInfo.InvariantCulture));
        WriteAttributes(writer, root);
        if (form.ControlLibraries.Count > 0)
        {
            writer.WriteStartElement($"{root.Class.ClassName}.ControlLibraries", Ns);
            foreach (var library in form.ControlLibraries)
            {
                writer.WriteStartElement("ControlLibrary", Ns);
                writer.WriteAttributeString("Id", library.Id.ToString("B").ToUpperInvariant());
                writer.WriteAttributeString("Version", library.Version);
                writer.WriteAttributeString("FileName", library.FileName);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        WriteContents(writer, root);
    }

    // A component whose start tag is written: the components it holds, and the kept ones among them, each at the place it was read at.
    private sealed class Open(ComponentInstance component)
    {
        public ComponentInstance Component { get; } = component;
        public List<UnmodelledComponent> Kept { get; } = [.. component.UnmodelledContents.OrderBy(c => c.Position)];
        public int NextContent { get; set; }
        public int NextKept { get; set; }
    }

    // The property elements and the contents of a component whose start tag is written, and its end tag: depth first, on an explicit
    // stack, so that a deep nesting costs no call stack.
    private static void WriteContents(XmlWriter writer, ComponentInstance top)
    {
        var pending = new Stack<Open>();
        WritePropertyElements(writer, top);
        pending.Push(new Open(top));
        while (pending.TryPeek(out var open))
        {
            var contents = open.Component.Contents;
            if (open.NextKept < open.Kept.Count && (open.NextContent >= contents.Count || open.Kept[open.NextKept].Position <= open.NextContent))
            {
                WriteKept(writer, open.Kept[open.NextKept++]);
            }
            else if (open.NextContent < contents.Count)
            {
                var component = contents[open.NextContent++];
                writer.WriteStartElement(component.Class.ClassName, Ns);
                WriteAttributes(writer, component);
                WritePropertyElements(writer, component);
                pending.Push(new Open(component));
            }
            else
            {
                writer.WriteEndElement();
                pending.Pop();
            }
        }
    }

    // The name, the index, and each property set that is written as text, in the order of the class's properties.
    private static void WriteAttributes(XmlWriter writer, ComponentInstance component)
    {
        writer.WriteAttributeString("Name", component.Name);
        if (component.Index is { } index)
            writer.WriteAttributeString("Index", index.ToString(CultureInfo.InvariantCulture));
        foreach (var property in component.Class.Properties)
        {
            if (component.IsSet(property) && FormXamlValues.Format(component.GetBoxed(property)) is { } text)
                writer.WriteAttributeString(property.Name, text);
        }
    }

    // The font, the list, and the properties the model does not have.
    private static void WritePropertyElements(XmlWriter writer, ComponentInstance component)
    {
        var owner = component.Class.ClassName;
        if (component.IsSet(P.Font))
        {
            var font = component.Get(P.Font);
            writer.WriteStartElement($"{owner}.Font", Ns);
            writer.WriteStartElement("Font", Ns);
            writer.WriteAttributeString("Name", font.Name);
            writer.WriteAttributeString("Size", FormXamlValues.Format(font.Size));
            if (font.Weight != FormFont.NormalWeight)
                writer.WriteAttributeString("Weight", FormXamlValues.Format(font.Weight));
            if (font.Italic)
                writer.WriteAttributeString("Italic", "True");
            if (font.Underline)
                writer.WriteAttributeString("Underline", "True");
            if (font.Strikethrough)
                writer.WriteAttributeString("Strikethrough", "True");
            if (font.Charset != 0)
                writer.WriteAttributeString("Charset", FormXamlValues.Format(font.Charset));
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        if (component.Class.Has(P.List) && (component.IsSet(P.List) || component.IsSet(P.ItemData) || component.IsSet(P.ItemIcon)))
            WriteList(writer, component, owner);

        if (component.UnmodelledProperties.Count > 0)
        {
            writer.WriteStartElement($"{owner}.UnmodelledProperties", Ns);
            foreach (var property in component.UnmodelledProperties)
            {
                writer.WriteStartElement("UnmodelledProperty", Ns);
                writer.WriteAttributeString("Name", property.Name);
                writer.WriteAttributeString("Text", property.Text);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }
    }

    private static void WriteList(XmlWriter writer, ComponentInstance component, string owner)
    {
        var items = component.Get(P.List);
        var data = component.Class.Has(P.ItemData) && component.IsSet(P.ItemData) ? component.Get(P.ItemData) : [];
        var icons = component.Class.Has(P.ItemIcon) && component.IsSet(P.ItemIcon) ? component.Get(P.ItemIcon) : [];
        writer.WriteStartElement($"{owner}.List", Ns);
        for (var i = 0; i < items.Count; i++)
        {
            writer.WriteStartElement("Item", Ns);
            writer.WriteAttributeString("Text", items[i]);
            if (i < data.Count)
                writer.WriteAttributeString("ItemData", data[i].ToString(CultureInfo.InvariantCulture));
            if (i < icons.Count && icons[i] is { } icon)
                writer.WriteAttributeString("ItemIcon", Convert.ToBase64String(icon));
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteKept(XmlWriter writer, UnmodelledComponent kept)
    {
        writer.WriteStartElement("UnmodelledComponent", Ns);
        writer.WriteAttributeString("TypeName", kept.TypeName);
        writer.WriteAttributeString("Name", kept.Name);
        writer.WriteString(kept.Text);
        writer.WriteEndElement();
    }
}
