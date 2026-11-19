using RDCore.Forms.Model;
using RDCore.SDK.Workspace;
using RDCore.SDK.Workspace.Forms;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Xaml;

/// <summary>
/// Reads an RDForms form's document for the platform: what the form declares, as a <see cref="FormDescriptor"/>, from the <c>.xaml</c> beside its
/// <c>.rdform</c> module.
/// </summary>
/// <remarks>
/// A program that loads RDForms forms registers it once, before it reads one: <c>FormDesigner.Register(FormXamlDocumentReader.Instance)</c>
/// (<see cref="FormDesigner.Register"/>).
/// </remarks>
public sealed class FormXamlDocumentReader : IFormDocumentReader
{
    private FormXamlDocumentReader()
    {
    }

    /// <summary>
    /// The one instance of the reader, which keeps no state.
    /// </summary>
    public static FormXamlDocumentReader Instance { get; } = new();

    /// <summary>
    /// The extension of the documents it reads: <see cref="FormDesigner.RDFormsDocumentExtension"/>.
    /// </summary>
    public string DocumentExtension => FormDesigner.RDFormsDocumentExtension;

    /// <summary>
    /// What the form whose document is <paramref name="document"/> declares, as <see cref="DescriptorOf"/> reads it from the document's text.
    /// </summary>
    /// <param name="document">The document's content, as its file holds it: UTF-8, unless a byte order mark says another Unicode encoding.</param>
    /// <returns>The form.</returns>
    /// <exception cref="FormDesignerFormatException">The document holds no form (<see cref="FormDesignerFormatProblem.InvalidDocument"/>).</exception>
    public FormDescriptor Read(ReadOnlyMemory<byte> document) => DescriptorOf(ProjectModuleKinds.ReadText(document.ToArray()));

    /// <summary>
    /// What the form whose document's text is <paramref name="document"/> declares, as <see cref="FormXaml.Read"/> reads it.
    /// </summary>
    /// <param name="document">The document's text.</param>
    /// <returns>
    /// The form, of the <see cref="FormDesignerKind.RDForms"/> kind:
    /// <list type="bullet">
    /// <item>its root's name, and its class in the <c>RDForms</c> library (<c>RDForms.Form</c>);</item>
    /// <item>its properties as the document writes them, by name, its lengths in device-independent pixels;</item>
    /// <item>its controls and menu items, in the document's order, each with its class, its <c>TabIndex</c> and its <c>Index</c> in a control array
    /// where it has them, and the controls it holds;</item>
    /// <item>the document's text, as its <see cref="FormDescriptor.DesignerText"/>.</item>
    /// </list>
    /// A component the model cannot hold, kept as text (<see cref="UnmodelledComponent"/>), is not one of its controls.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormDesignerFormatException">The document holds no form (<see cref="FormDesignerFormatProblem.InvalidDocument"/>).</exception>
    public static FormDescriptor DescriptorOf(string document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (FormXaml.Read(document).Form is not { } form)
            throw new FormDesignerFormatException(FormDesignerFormatProblem.InvalidDocument);

        var root = form.Root;
        return new FormDescriptor(root.Name, FormDesignerKind.RDForms, ClassOf(root.Class), PropertiesOf(root), [.. root.Contents.Select(ControlOf)])
        {
            DesignerText = document,
        };
    }

    // `RDForms.TextBox`: the library and the class.
    private static FormControlClass ClassOf(ComponentClass componentClass)
    {
        var dot = componentClass.TypeName.IndexOf('.');
        return dot < 0
            ? new FormControlClass(null, componentClass.TypeName)
            : new FormControlClass(componentClass.TypeName[..dot], componentClass.TypeName[(dot + 1)..]);
    }

    // The properties a component sets, as the writer writes them as attributes.
    private static Dictionary<string, string> PropertiesOf(ComponentInstance component)
    {
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in component.Class.Properties)
        {
            if (!property.RunTimeOnly && component.IsSet(property) && FormXamlValues.Format(property.ValueType, component.GetBoxed(property)) is { } text)
                properties[property.Name] = text;
        }

        return properties;
    }

    // Depth first on an explicit stack, as the reader and the writer go: a deep nesting costs no call stack.
    private static FormControlDescriptor ControlOf(ComponentInstance top)
    {
        var made = new Dictionary<ComponentInstance, FormControlDescriptor>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<(ComponentInstance Component, bool Visited)>([(top, false)]);
        while (pending.TryPop(out var next))
        {
            if (!next.Visited)
            {
                pending.Push((next.Component, true));
                foreach (var child in next.Component.Contents)
                    pending.Push((child, false));
                continue;
            }

            var component = next.Component;
            made[component] = new FormControlDescriptor(component.Name, ClassOf(component.Class),
                component.IsSet(P.TabIndex) ? component.Get(P.TabIndex) : null, component.Index, [.. component.Contents.Select(child => made[child])]);
        }

        return made[top];
    }
}
