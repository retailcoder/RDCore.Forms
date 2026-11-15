using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using K = RDCore.Forms.Xaml.FormXamlDiagnosticKind;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Xaml;

// Reads a loaded document into the model, reporting what it keeps as text.
internal sealed class FormXamlReader(FormXamlDiagnosticBag diagnostics)
{
    private static readonly XNamespace Ns = FormXaml.Namespace;

    private FormDefinition? form;

    // The document's FormatVersion, or 0 where it has none that can be read.
    public int Version { get; private set; }

    public FormDefinition? Read(XElement root)
    {
        var rootClass = root.Name.Namespace == Ns ? ClassOf(root.Name.LocalName) : null;
        if (rootClass is not { Kind: ComponentKind.Root })
        {
            diagnostics.Add(K.NotAForm, LineOf(root), root.Name.ToString());
            return null;
        }

        var name = (string?)root.Attribute("Name");
        if (string.IsNullOrEmpty(name))
        {
            diagnostics.Add(K.NamelessComponent, FormXamlSeverity.Error, LineOf(root), rootClass.ClassName);
            return null;
        }

        var instance = new ComponentInstance(rootClass, name);
        form = new FormDefinition(instance);
        ReadVersion(root, instance);
        ReadComponent(root, instance, depth: 1);
        return form;
    }

    private void ReadVersion(XElement root, ComponentInstance instance)
    {
        var attribute = root.Attribute("FormatVersion");
        if (attribute is null || !int.TryParse(attribute.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var version) || version < 1)
        {
            diagnostics.Add(K.InvalidValue, LineOf((XObject?)attribute ?? root), instance.Name, "FormatVersion", attribute?.Value ?? "");
            return;
        }

        Version = version;
        if (version > FormXaml.FormatVersion)
            diagnostics.Add(K.NewerFormatVersion, LineOf(attribute), version, FormXaml.FormatVersion);
    }

    // Depth first, in the document's order, on an explicit stack: a deep nesting costs no call stack.
    private void ReadComponent(XElement element, ComponentInstance instance, int depth)
    {
        var pending = new Stack<(IEnumerator<XElement> Children, XElement Element, ComponentInstance Instance, int Depth)>();
        Open(element, instance, depth);
        while (pending.TryPeek(out var top))
        {
            if (!top.Children.MoveNext())
            {
                top.Children.Dispose();
                pending.Pop();
                continue;
            }

            if (ReadChild(top.Children.Current, top.Element, top.Instance, top.Depth) is { } component)
                Open(top.Children.Current, component, top.Depth + 1);
        }

        void Open(XElement element, ComponentInstance instance, int depth)
        {
            foreach (var attribute in element.Attributes())
                ReadAttribute(attribute, instance);
            pending.Push((element.Elements().GetEnumerator(), element, instance, depth));
        }
    }

    private void ReadAttribute(XAttribute attribute, ComponentInstance instance)
    {
        // Namespace declarations, and the attributes of other namespaces, such as xml:space, are the XML's own.
        if (attribute.IsNamespaceDeclaration || attribute.Name.Namespace != XNamespace.None)
            return;
        var name = attribute.Name.LocalName;
        if (name == "Name" || name == "FormatVersion" && instance.Class.Kind == ComponentKind.Root)
            return;
        if (name == "Index")
        {
            if (int.TryParse(attribute.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var index) && index <= 32767)
                instance.Index = index;
            else
                Invalid(instance, name, attribute.Value, attribute);
            return;
        }

        ReadValue(instance, name, attribute.Value, attribute);
    }

    // A property's value written as text, in an attribute or a property element.
    private void ReadValue(ComponentInstance instance, string name, string text, XObject at)
    {
        // A property only a running program sets is not the form's.
        var property = instance.Class.Property(name);
        if (property is null or { RunTimeOnly: true })
        {
            diagnostics.Add(K.UnknownProperty, LineOf(at), instance.Name, name);
            instance.UnmodelledProperties.Add(new UnmodelledProperty(name, text));
        }
        else if (!FormXamlValues.TryParse(property.ValueType, text, out var value) || !instance.TryRead(property, value))
            Invalid(instance, property.Name, text, at);
    }

    private void Invalid(ComponentInstance instance, string property, string text, XObject at)
    {
        diagnostics.Add(K.InvalidValue, LineOf(at), instance.Name, property, text);
        instance.UnmodelledProperties.Add(new UnmodelledProperty(property, text));
    }

    // Reads a child element of a component, and returns the component it is, placed, whose own attributes and children are then read.
    private ComponentInstance? ReadChild(XElement child, XElement element, ComponentInstance instance, int depth)
    {
        var local = child.Name.LocalName;
        if (child.Name.Namespace != Ns)
        {
            Unmodelled(child, instance);
            return null;
        }

        var dot = local.IndexOf('.');
        if (dot >= 0)
        {
            if (local[..dot] == element.Name.LocalName)
                ReadPropertyElement(child, local[(dot + 1)..], instance);
            else
                Unmodelled(child, instance);
            return null;
        }

        if (local == "UnmodelledComponent")
        {
            instance.UnmodelledContents.Add(new UnmodelledComponent((string?)child.Attribute("TypeName") ?? "", (string?)child.Attribute("Name") ?? "",
                instance.Contents.Count, child.Value));
            return null;
        }

        var componentClass = ClassOf(local);
        if (componentClass is null || componentClass.Kind == ComponentKind.Root)
        {
            Unmodelled(child, instance);
            return null;
        }

        var name = (string?)child.Attribute("Name");
        if (string.IsNullOrEmpty(name))
        {
            diagnostics.Add(K.NamelessComponent, LineOf(child), componentClass.ClassName);
            Keep(child, componentClass.TypeName, "", instance);
            return null;
        }

        if (depth >= FormXaml.MaxNestingDepth)
        {
            diagnostics.Add(K.NestingTooDeep, LineOf(child), name, FormXaml.MaxNestingDepth);
            Keep(child, componentClass.TypeName, name, instance);
            return null;
        }

        var component = new ComponentInstance(componentClass, name);
        var container = instance;
        while (!CanHold(container, component))
            container = container.Container!;
        if (!ReferenceEquals(container, instance))
            diagnostics.Add(K.MisplacedComponent, LineOf(child), name, instance.Name, container.Name);
        component.SetContainer(container);
        return component;
    }

    // As ComponentInstance.SetContainer places: a menu item in a root or another menu item, a control in a container. A root holds
    // both, so the walk out of a component ends.
    private static bool CanHold(ComponentInstance container, ComponentInstance component) =>
        component.Class.Kind == ComponentKind.Menu
            ? container.Class.Kind is ComponentKind.Root or ComponentKind.Menu
            : container.Class.IsContainer;

    private void ReadPropertyElement(XElement element, string name, ComponentInstance instance)
    {
        switch (name)
        {
            case "Font" when instance.Class.Has(P.Font):
                ReadFont(element, instance);
                return;
            case "List" when instance.Class.Has(P.List):
                ReadList(element, instance);
                return;
            case "UnmodelledProperties":
                foreach (var property in element.Elements())
                {
                    if (property.Name == Ns + "UnmodelledProperty")
                        instance.UnmodelledProperties.Add(new UnmodelledProperty((string?)property.Attribute("Name") ?? "", (string?)property.Attribute("Text") ?? ""));
                    else
                        Unmodelled(property, instance);
                }

                return;
            case "ControlLibraries" when instance.Class.Kind == ComponentKind.Root:
                ReadControlLibraries(element, instance);
                return;
        }

        // Any other property written as an element holds its value as text, such as a caption of several lines.
        if (element.HasElements)
            Unmodelled(element, instance);
        else
            ReadValue(instance, name, element.Value, element);
    }

    private void ReadFont(XElement element, ComponentInstance instance)
    {
        var font = element.Elements().SingleOrDefaultIfMany();
        if (font?.Name == Ns + "Font" && ReadFont(font) is { } value)
            instance.Set(P.Font, value);
        else
            Invalid(instance, P.Font.Name, element.ToString(SaveOptions.DisableFormatting), element);
    }

    private static FormFont? ReadFont(XElement font)
    {
        var name = (string?)font.Attribute("Name");
        double size = 0;
        if (string.IsNullOrEmpty(name) || font.Attribute("Size") is null || !Optional(font, "Size", ref size) || size <= 0)
            return null;
        int weight = FormFont.NormalWeight, charset = 0;
        bool italic = false, underline = false, strikethrough = false;
        var read = Optional(font, "Weight", ref weight) && weight is >= 0 and <= 1000
            && Optional(font, "Charset", ref charset) && charset is >= 0 and <= 255
            && Optional(font, "Italic", ref italic) && Optional(font, "Underline", ref underline) && Optional(font, "Strikethrough", ref strikethrough);
        return read ? new FormFont(name, size, weight, italic, underline, strikethrough, charset) : null;
    }

    // An attribute that may be left out, keeping the value given; false where it is written and cannot be read.
    private static bool Optional<T>(XElement element, string name, ref T value)
    {
        if (element.Attribute(name) is not { } attribute)
            return true;
        if (!FormXamlValues.TryParse(typeof(T), attribute.Value, out var read))
            return false;
        value = (T)read!;
        return true;
    }

    // One Item for each item, with its Text, and its ItemData and ItemIcon where it has them.
    private void ReadList(XElement element, ComponentInstance instance)
    {
        var texts = new List<string>();
        var data = new List<int>();
        var icons = new List<byte[]?>();
        bool anyData = false, anyIcon = false;
        foreach (var item in element.Elements())
        {
            if (item.Name != Ns + "Item")
            {
                Unmodelled(item, instance);
                continue;
            }

            var index = texts.Count;
            texts.Add((string?)item.Attribute("Text") ?? "");
            data.Add(0);
            icons.Add(null);
            if (item.Attribute("ItemData") is { } itemData)
            {
                if (int.TryParse(itemData.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                    (data[index], anyData) = (value, true);
                else
                    Invalid(instance, $"ItemData({index})", itemData.Value, itemData);
            }

            if (item.Attribute("ItemIcon") is { } itemIcon && instance.Class.Has(P.ItemIcon))
            {
                if (FormXamlValues.ParsePicture(itemIcon.Value) is { } icon)
                    (icons[index], anyIcon) = (icon, true);
                else
                    Invalid(instance, $"ItemIcon({index})", itemIcon.Value, itemIcon);
            }
        }

        instance.Set(P.List, [.. texts]);
        if (anyData && instance.Class.Has(P.ItemData))
            instance.Set(P.ItemData, [.. data]);
        if (anyIcon)
            instance.Set(P.ItemIcon, [.. icons]);
    }

    private void ReadControlLibraries(XElement element, ComponentInstance instance)
    {
        foreach (var library in element.Elements())
        {
            if (library.Name != Ns + "ControlLibrary")
            {
                Unmodelled(library, instance);
                continue;
            }

            if (Guid.TryParse((string?)library.Attribute("Id"), out var id) && (string?)library.Attribute("Version") is { } version
                && (string?)library.Attribute("FileName") is { } fileName)
                form!.ControlLibraries.Add(new ControlLibraryReference(id, version, fileName));
            else
                Invalid(instance, "ControlLibraries", library.ToString(SaveOptions.DisableFormatting), library);
        }
    }

    // An element the format does not have in this place: reported, and kept as the component's text.
    private void Unmodelled(XElement element, ComponentInstance instance)
    {
        diagnostics.Add(K.UnmodelledElement, LineOf(element), element.Name.ToString());
        Keep(element, element.Name.ToString(), (string?)element.Attribute("Name") ?? "", instance);
    }

    private static void Keep(XElement element, string typeName, string name, ComponentInstance instance) =>
        instance.UnmodelledContents.Add(new UnmodelledComponent(typeName, name, instance.Contents.Count, element.ToString(SaveOptions.DisableFormatting)));

    // A class of the catalog, by its name without its library, such as CommandButton.
    private static ComponentClass? ClassOf(string name) =>
        name.Contains('.') ? null : VBClasses.ByTypeName("VB." + name) is { } found && found.ClassName == name ? found : null;

    private static int LineOf(XObject element) => element is IXmlLineInfo info && info.HasLineInfo() ? info.LineNumber : 0;
}

internal static class XElementSequence
{
    // The one element of a sequence, or null where it has none or several.
    public static XElement? SingleOrDefaultIfMany(this IEnumerable<XElement> elements)
    {
        XElement? found = null;
        foreach (var element in elements)
        {
            if (found is not null)
                return null;
            found = element;
        }

        return found;
    }
}
