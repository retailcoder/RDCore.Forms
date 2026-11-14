// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using System.Text;
using System.Text.RegularExpressions;
using RDCore.Forms.Menus;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;

namespace RDCore.Forms.Import.VB6;

/// <summary>
/// Reads a VB6 designer file (<c>.frm</c>, <c>.ctl</c> or <c>.pag</c>) and its companion binary file into a <see cref="FormDefinition"/>.
/// </summary>
/// <remarks>
/// <para>
/// The designer half, from <c>VERSION</c> to the root block's <c>End</c>, becomes the form; the rest of the file is returned as
/// <see cref="VB6FormImport.ModuleText"/>. Property and class names are read without regard to case. A component's
/// <c>Index</c> becomes its <see cref="ComponentInstance.Index"/>, and a root's <c>Client*</c> rectangle its position and size.
/// A file's positions and sizes are twips whatever a container's <c>ScaleMode</c>, and are read as pixels, at 15 twips a pixel. A Line's
/// ends are in its container's scale, and are read as written.
/// </para>
/// <para>
/// A read does not throw on what a file holds. A property the model does not have, or whose value it cannot hold, is kept as an
/// <see cref="UnmodelledProperty"/>, and so is a property bag other than a Font its class has; a component of a class the model does
/// not have as a control or a menu, such as an ActiveX control, or with no name, or nested deeper than <see cref="MaxNestingDepth"/>,
/// is kept as an <see cref="UnmodelledComponent"/>; a header line that names no control library is kept in
/// <see cref="VB6FormImport.UnmodelledHeader"/>, and a root block that is not a form's in <see cref="VB6FormImport.UnmodelledRoot"/>.
/// Each of these is reported in <see cref="VB6FormImport.Diagnostics"/>, except a property or a property bag the model does not have.
/// A line that cannot be read and an <c>EndProperty</c> that closes nothing are reported and left out. The <c>VERSION</c> line is
/// left out, and so are the header lines of a file that holds no form. A property a block sets twice has the value of its last line.
/// </para>
/// <para>
/// A control that writes no <c>Font</c>, on a <c>VB.Form</c> that writes one, takes a copy of that form's font. A component placed
/// inside one that cannot hold it, such as a control inside a ListBox, is placed in the nearest enclosing component that can, at its
/// position as written. A companion record is read in the shape its property's type has: a string cited as
/// <c>$"Form1.frx":0000</c> has a 4-byte length, and one cited as <c>"Form1.frx":0000</c> a 1-byte length and ends at its first NUL;
/// a picture is a 4-byte length of the rest, the signature <c>lt\0\0</c>, then the image file's 4-byte length and its bytes, an image
/// of no bytes being no picture; a List is a 2-byte count, then, for a list that is not empty, a 2-byte word, then each item's 2-byte
/// length and bytes; an ItemData is a List of the decimal text of each Long.
/// </para>
/// </remarks>
public static partial class VB6FormReader
{
    /// <summary>
    /// How deep the blocks of a designer file are read, the root's included: 2,500. A block nested deeper is kept as text, its own
    /// contents included.
    /// </summary>
    public const int MaxNestingDepth = 2_500;

    /// <summary>
    /// Reads a designer file's text and its companion binary file.
    /// </summary>
    /// <param name="designerFile">The designer file's text.</param>
    /// <param name="companion">
    /// The companion binary file's bytes, read for every property that cites a companion record, or <see langword="null"/> when there
    /// is none. A property citing one is then kept as text.
    /// </param>
    /// <param name="options">How strings in the companion are decoded; <see langword="null"/> for <see cref="VB6ImportOptions.Default"/>.</param>
    /// <returns>The form, the module text, and what the import reports.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="designerFile"/> is <see langword="null"/>.</exception>
    public static VB6FormImport Read(string designerFile, byte[]? companion = null, VB6ImportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(designerFile);
        options ??= VB6ImportOptions.Default;

        var read = companion is null ? null : new Companion(companion, options.Encoding);
        return Read(designerFile, cited => (read, cited));
    }

    /// <summary>
    /// Reads a designer file, and the companion binary files its properties cite, such as <c>Form1.frx</c>: each one by the name the
    /// designer file cites, from the designer file's folder, its name compared without regard to case. A cited name that is a rooted
    /// path, such as a network path, is not read, and neither is one that holds a NUL: the property is reported as citing a missing
    /// companion.
    /// </summary>
    /// <param name="path">The designer file's path.</param>
    /// <param name="options">
    /// How the designer file and the strings in its companions are decoded; <see langword="null"/> for <see cref="VB6ImportOptions.Default"/>.
    /// </param>
    /// <returns>The form, the module text, and what the import reports.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty, or holds a NUL.</exception>
    /// <exception cref="IOException">The designer file, or a companion file that was found, cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">
    /// The designer file, or a companion file that was found, or the designer file's folder, cannot be accessed.
    /// </exception>
    public static VB6FormImport ReadFile(string path, VB6ImportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        options ??= VB6ImportOptions.Default;

        var fullPath = Path.GetFullPath(path);
        var folder = Path.GetDirectoryName(fullPath) ?? "";
        var text = options.Encoding.GetString(File.ReadAllBytes(fullPath));
        var companions = new Dictionary<string, (Companion?, string)>(StringComparer.OrdinalIgnoreCase);
        return Read(text, cited =>
        {
            if (!companions.TryGetValue(cited, out var found))
            {
                var (lookedFor, file) = FindCompanion(folder, cited);
                found = (file is null ? null : new Companion(File.ReadAllBytes(file), options.Encoding), lookedFor);
                companions[cited] = found;
            }

            return found;
        });
    }

    private static VB6FormImport Read(string designerFile, Func<string, (Companion? Companion, string LookedFor)> companions)
    {
        var diagnostics = new DiagnosticBag();
        var file = DesignerFile.Parse(designerFile, diagnostics);
        var importer = new Importer(diagnostics, companions);
        var form = file.Root is null ? null : importer.Import(file, file.Root);
        return new VB6FormImport(form, importer.UnmodelledRoot, importer.UnmodelledHeader, file.ModuleText, file.ModuleLine, diagnostics.Items);
    }

    // The path a cited companion is looked for at, and the file found there. A rooted name, a network path among them, is not in the
    // designer file's folder, and a name with a NUL is no file's: neither is looked for.
    private static (string LookedFor, string? File) FindCompanion(string folder, string cited)
    {
        if (cited.Contains('\0') || Path.IsPathRooted(cited))
        {
            return (cited, null);
        }

        var path = Path.GetFullPath(Path.Combine(folder, cited));
        return (path, FindIgnoringCase(path));
    }

    // The file at the path, or else the one of its folder whose name differs from it only in case.
    private static string? FindIgnoringCase(string path)
    {
        if (File.Exists(path))
        {
            return path;
        }

        var folder = Path.GetDirectoryName(path);
        var name = Path.GetFileName(path);
        return folder is not null && Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder).FirstOrDefault(f => string.Equals(Path.GetFileName(f), name, StringComparison.OrdinalIgnoreCase))
            : null;
    }

    [GeneratedRegex("""^"\{(?<id>[0-9A-Fa-f-]{36})\}#(?<version>[^#"]*)#[^"]*"\s*;\s*"(?<file>[^"]*)"$""")]
    private static partial Regex LibraryReference();

    private sealed class Importer(DiagnosticBag diagnostics, Func<string, (Companion? Companion, string LookedFor)> companions)
    {
        private static readonly Dictionary<string, PropertyDefinition> ClientRectangle = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ClientLeft"] = VBProperties.Left,
            ["ClientTop"] = VBProperties.Top,
            ["ClientWidth"] = VBProperties.Width,
            ["ClientHeight"] = VBProperties.Height,
        };

        public UnmodelledComponent? UnmodelledRoot { get; private set; }

        public List<UnmodelledProperty> UnmodelledHeader { get; } = [];

        public FormDefinition? Import(DesignerFile file, DesignerBlock root)
        {
            if (root.Name.Length == 0)
            {
                diagnostics.Add(VB6ImportDiagnosticKind.NamelessComponent, VB6ImportSeverity.Error, root.Line, root.TypeName);
                UnmodelledRoot = new UnmodelledComponent(root.TypeName, root.Name, 0, root.Text);
                return null;
            }

            var rootClass = VBClasses.ByTypeName(root.TypeName);
            if (rootClass is not { Kind: ComponentKind.Root })
            {
                diagnostics.Add(VB6ImportDiagnosticKind.NotAFormRoot, root.Line, root.TypeName);
                UnmodelledRoot = new UnmodelledComponent(root.TypeName, root.Name, 0, root.Text);
                return null;
            }

            var (index, invalidIndex) = ReadIndex(root);
            var rootInstance = new ComponentInstance(rootClass, root.Name, index);
            foreach (var property in root.Properties)
            {
                if (ClientRectangle.TryGetValue(property.Name, out var axis))
                {
                    SetFromText(rootInstance, axis, property, root.Name);
                }
                else if (rootClass.Property(property.Name) is { } definition
                    && definition != VBProperties.Left && definition != VBProperties.Top
                    && definition != VBProperties.Width && definition != VBProperties.Height)
                {
                    SetFromText(rootInstance, definition, property, root.Name);
                }
                else if (!property.Name.Equals("Index", StringComparison.OrdinalIgnoreCase) || property == invalidIndex)
                {
                    // the window's outer rectangle among them: the model holds the client rectangle.
                    rootInstance.UnmodelledProperties.Add(new UnmodelledProperty(property.Name, property.Value));
                }
            }

            ReadBags(rootInstance, root);
            var form = new FormDefinition(rootInstance);
            ReadHeader(file, form);

            var ambientFont = rootClass == VBClasses.Form && rootInstance.IsSet(VBProperties.Font) ? rootInstance.Get(VBProperties.Font) : (FormFont?)null;
            ReadContents(rootInstance, root, ambientFont);
            return form;
        }

        // Depth first in the file's order, with a stack of its own rather than the call stack, so a deep file costs no recursion.
        private void ReadContents(ComponentInstance root, DesignerBlock rootBlock, FormFont? ambientFont)
        {
            var pending = new Stack<(ComponentInstance Container, DesignerBlock Block, int Depth)>();
            Push(pending, root, rootBlock, 1);
            while (pending.TryPop(out var next))
            {
                var (container, child, depth) = next;
                if (depth > MaxNestingDepth)
                {
                    diagnostics.Add(VB6ImportDiagnosticKind.NestingTooDeep, child.Line, child.Name, MaxNestingDepth);
                    KeepAsText(container, child);
                    continue;
                }

                if (child.Name.Length == 0)
                {
                    diagnostics.Add(VB6ImportDiagnosticKind.NamelessComponent, child.Line, child.TypeName);
                    KeepAsText(container, child);
                    continue;
                }

                var componentClass = VBClasses.ByTypeName(child.TypeName);
                if (componentClass is null or { Kind: ComponentKind.Root })
                {
                    diagnostics.Add(VB6ImportDiagnosticKind.UnmodelledClass, child.Line, child.TypeName, child.Name);
                    KeepAsText(container, child);
                    continue;
                }

                var (index, invalidIndex) = ReadIndex(child);
                var instance = new ComponentInstance(componentClass, child.Name, index);
                Place(instance, container, child.Line);

                foreach (var property in child.Properties)
                {
                    if (componentClass.Property(property.Name) is { } definition)
                    {
                        SetFromText(instance, definition, property, instance.ToString());
                    }
                    else if (!property.Name.Equals("Index", StringComparison.OrdinalIgnoreCase) || property == invalidIndex)
                    {
                        instance.UnmodelledProperties.Add(new UnmodelledProperty(property.Name, property.Value));
                    }
                }

                // a control whose Font bag could not be read keeps that bag as text, and does not take the form's font as well.
                var hasFontBag = ReadBags(instance, child);
                if (ambientFont is { } font && componentClass.Has(VBProperties.Font) && !hasFontBag)
                {
                    instance.Set(VBProperties.Font, font);
                }

                OffsetByTimers(instance, container);
                Push(pending, instance, child, depth);
            }
        }

        // A block's children, one level deeper than the block, pushed last first so the first is read first.
        private static void Push(Stack<(ComponentInstance, DesignerBlock, int)> pending, ComponentInstance container, DesignerBlock block, int depth)
        {
            for (var at = block.Children.Count - 1; at >= 0; at--)
            {
                pending.Push((container, block.Children[at], depth + 1));
            }
        }

        // VB6 places a control written inside a Timer, which holds no control, at the Timer's Left and Top added to its own. A control
        // written inside another control that cannot hold it keeps the position the file gives it.
        private static void OffsetByTimers(ComponentInstance instance, ComponentInstance writtenIn)
        {
            if (!instance.Class.Has(VBProperties.Left))
            {
                return;
            }

            for (ComponentInstance? host = writtenIn; host is not null && host != instance.Container; host = host.Container)
            {
                if (ReferenceEquals(host.Class, VBClasses.Timer))
                {
                    instance.Set(VBProperties.Left, instance.Get(VBProperties.Left) + host.Get(VBProperties.Left))
                        .Set(VBProperties.Top, instance.Get(VBProperties.Top) + host.Get(VBProperties.Top));
                }
            }
        }

        // In the component the file places it in, or else in the nearest enclosing component that can hold it; the root holds any
        // control and any menu.
        private void Place(ComponentInstance instance, ComponentInstance writtenIn, int line)
        {
            for (ComponentInstance? container = writtenIn; container is not null; container = container.Container)
            {
                if (CanHold(container, instance.Class))
                {
                    if (container != writtenIn)
                    {
                        diagnostics.Add(VB6ImportDiagnosticKind.MisplacedComponent, line, writtenIn.ToString(), instance.ToString(), container.ToString());
                    }

                    instance.SetContainer(container);
                    return;
                }
            }
        }

        private static bool CanHold(ComponentInstance container, ComponentClass content) => content.Kind switch
        {
            ComponentKind.Menu => container.Class.Kind is ComponentKind.Root or ComponentKind.Menu,
            _ => container.Class.IsContainer,
        };

        private static void KeepAsText(ComponentInstance container, DesignerBlock block) =>
            container.UnmodelledContents.Add(new UnmodelledComponent(block.TypeName, block.Name, container.Contents.Count, block.Text));

        // The block's Index, and the Index line kept as text when its value is not one.
        private (int? Index, DesignerProperty? Invalid) ReadIndex(DesignerBlock block)
        {
            var index = block.Properties.LastOrDefault(p => p.Name.Equals("Index", StringComparison.OrdinalIgnoreCase));
            if (index is null)
            {
                return (null, null);
            }

            if (ValueLiteral.Parse(index.Value) is ValueLiteral.Number { IsIntegral: true, Value: >= 0 and <= short.MaxValue } number)
            {
                return ((int)number.Value, null);
            }

            diagnostics.Add(VB6ImportDiagnosticKind.InvalidValue, index.Line, index.Name, block.Name);
            return (null, index);
        }

        private void ReadHeader(DesignerFile file, FormDefinition form)
        {
            foreach (var line in file.Header)
            {
                if (!line.Name.Equals("Object", StringComparison.OrdinalIgnoreCase))
                {
                    diagnostics.Add(VB6ImportDiagnosticKind.UnmodelledHeaderLine, line.Line, line.Name);
                    UnmodelledHeader.Add(new UnmodelledProperty(line.Name, line.Value));
                    continue;
                }

                var match = LibraryReference().Match(line.Value);
                if (match.Success && Guid.TryParse(match.Groups["id"].Value, out var id))
                {
                    form.ControlLibraries.Add(new ControlLibraryReference(id, match.Groups["version"].Value, match.Groups["file"].Value));
                }
                else
                {
                    diagnostics.Add(VB6ImportDiagnosticKind.UnreadLibraryReference, line.Line, line.Value);
                    UnmodelledHeader.Add(new UnmodelledProperty(line.Name, line.Value));
                }
            }
        }

        // Whether the block has a Font bag the instance's class could take, read or kept as text.
        private bool ReadBags(ComponentInstance instance, DesignerBlock block)
        {
            var hasFontBag = false;
            foreach (var bag in block.Bags)
            {
                var isFont = bag.Name.Equals("Font", StringComparison.OrdinalIgnoreCase) && instance.Class.Has(VBProperties.Font);
                hasFontBag |= isFont;
                if (isFont && ReadFont(bag) is { } font)
                {
                    instance.Set(VBProperties.Font, font);
                    continue;
                }

                if (isFont)
                {
                    diagnostics.Add(VB6ImportDiagnosticKind.InvalidValue, bag.Line, bag.Name, instance.ToString());
                }

                instance.UnmodelledProperties.Add(new UnmodelledProperty(bag.Name, bag.Text));
            }

            return hasFontBag;
        }

        // A Font bag's fields, in any case and any order; a field the bag leaves out keeps the default font's.
        private static FormFont? ReadFont(DesignerBag bag)
        {
            var font = FormFont.Default;
            foreach (var field in bag.Properties)
            {
                var value = ValueLiteral.Parse(field.Value);
                switch (field.Name.ToLowerInvariant(), value)
                {
                    case ("name", ValueLiteral.Text text):
                        font = font with { Name = text.Value };
                        break;
                    case ("size", ValueLiteral.Number { Value: > 0 and <= float.MaxValue } size):
                        font = font with { Size = size.Value };
                        break;
                    case ("charset", ValueLiteral.Number { IsIntegral: true, Value: >= 0 and <= 255 } charset):
                        font = font with { Charset = (int)charset.Value };
                        break;
                    case ("weight", ValueLiteral.Number { IsIntegral: true, Value: >= 0 and <= 1000 } weight):
                        font = font with { Weight = (int)weight.Value };
                        break;
                    case ("underline", ValueLiteral.Number underline):
                        font = font with { Underline = underline.Value != 0 };
                        break;
                    case ("italic", ValueLiteral.Number italic):
                        font = font with { Italic = italic.Value != 0 };
                        break;
                    case ("strikethrough", ValueLiteral.Number strikethrough):
                        font = font with { Strikethrough = strikethrough.Value != 0 };
                        break;
                    default:
                        return null;
                }
            }

            return font;
        }

        private void SetFromText(ComponentInstance instance, PropertyDefinition definition, DesignerProperty property, string component)
        {
            var literal = ValueLiteral.Parse(property.Value);
            if (literal is ValueLiteral.Citation citation)
            {
                SetFromCompanion(instance, definition, property, component, citation);
                return;
            }

            if (TryConvert(definition.ValueType, literal, out var value)
                && instance.TrySet(definition, CodeUnits.IsExtent(definition) && value is double twips ? twips / ScaleModeExtensions.TwipsPerPixel : value))
            {
                return;
            }

            KeepInvalid(instance, property, component);
        }

        private void SetFromCompanion(ComponentInstance instance, PropertyDefinition definition, DesignerProperty property, string component,
            ValueLiteral.Citation citation)
        {
            if (!HasRecordShape(definition.ValueType, citation))
            {
                KeepInvalid(instance, property, component);
                return;
            }

            var (companion, lookedFor) = companions(citation.FileName);
            if (companion is null)
            {
                diagnostics.Add(VB6ImportDiagnosticKind.MissingCompanion, property.Line, property.Name, component, lookedFor);
                instance.UnmodelledProperties.Add(new UnmodelledProperty(property.Name, property.Value));
                return;
            }

            if (!TryReadRecord(companion, definition.ValueType, citation, out var record))
            {
                diagnostics.Add(VB6ImportDiagnosticKind.MissingRecord, property.Line, property.Name, component,
                    citation.Offset.ToString("X4", System.Globalization.CultureInfo.InvariantCulture), citation.FileName);
                instance.UnmodelledProperties.Add(new UnmodelledProperty(property.Name, property.Value));
                return;
            }

            if (!instance.TrySet(definition, record))
            {
                KeepInvalid(instance, property, component);
            }
        }

        private void KeepInvalid(ComponentInstance instance, DesignerProperty property, string component)
        {
            diagnostics.Add(VB6ImportDiagnosticKind.InvalidValue, property.Line, property.Name, component);
            instance.UnmodelledProperties.Add(new UnmodelledProperty(property.Name, property.Value));
        }

        // Whether a property of the type can be cited this way: a string by either citation, a picture or a list by the plain one.
        private static bool HasRecordShape(Type type, ValueLiteral.Citation citation) =>
            type == typeof(string)
            || !citation.IsDollar && (type == typeof(byte[]) || type == typeof(IReadOnlyList<string>) || type == typeof(IReadOnlyList<int>));

        private static bool TryReadRecord(Companion companion, Type type, ValueLiteral.Citation citation, out object? record)
        {
            bool read;
            if (type == typeof(string))
            {
                read = citation.IsDollar
                    ? companion.TryReadLongString(citation.Offset, out var text)
                    : companion.TryReadShortString(citation.Offset, out text);
                record = text;
            }
            else if (type == typeof(byte[]))
            {
                read = companion.TryReadPicture(citation.Offset, out var image);
                record = image;
            }
            else if (type == typeof(IReadOnlyList<string>))
            {
                read = companion.TryReadList(citation.Offset, out var items);
                record = items;
            }
            else
            {
                read = companion.TryReadItemData(citation.Offset, out var values);
                record = values;
            }

            return read;
        }

        // A number in the range of the property's VB type: a short is an Integer, an int a Long, a double a Single.
        private static bool TryConvert(Type type, ValueLiteral literal, out object? value)
        {
            value = null;
            switch (literal)
            {
                case ValueLiteral.Text text when type == typeof(string):
                    value = text.Value;
                    return true;

                case ValueLiteral.Number { Value: >= -float.MaxValue and <= float.MaxValue } number when type == typeof(double):
                    value = number.Value;
                    return true;

                case ValueLiteral.Number { IsIntegral: true, Value: >= short.MinValue and <= short.MaxValue } number when type == typeof(short):
                    value = (short)number.Value;
                    return true;

                case ValueLiteral.Number { IsIntegral: true, Value: >= int.MinValue and <= int.MaxValue } number when type == typeof(int):
                    value = (int)number.Value;
                    return true;

                case ValueLiteral.Number number when type == typeof(bool):
                    value = number.Value != 0;
                    return true;

                case ValueLiteral.Number { IsIntegral: true, Value: >= int.MinValue and <= int.MaxValue } number when type.IsEnum:
                    var member = Enum.ToObject(type, (int)number.Value);
                    value = member;
                    return Enum.IsDefined(type, member);

                case ValueLiteral.Number { IsIntegral: true, Value: >= int.MinValue and <= int.MaxValue } number when type == typeof(FormColor)
                    && FormColor.TryFromOle((int)number.Value, out var color):
                    value = color;
                    return true;

                case ValueLiteral.Word word when type == typeof(MenuShortcut?) && MenuShortcut.TryParse(word.Value.Trim(), out var shortcut):
                    value = shortcut;
                    return true;

                default:
                    return false;
            }
        }
    }
}

/// <summary>
/// How a <see cref="VB6FormReader"/> decodes the text it reads.
/// </summary>
public sealed class VB6ImportOptions
{
    /// <summary>
    /// The options a read takes when it is given none: the Windows-1252 code page, which VB6 writes on Western European systems.
    /// </summary>
    public static VB6ImportOptions Default { get; } = new();

    /// <summary>
    /// The code page a designer file read by <see cref="VB6FormReader.ReadFile"/> and the strings of a companion file are decoded
    /// with: the ANSI code page of the system that wrote them.
    /// </summary>
    public Encoding Encoding { get; init; } = CodePagesEncodingProvider.Instance.GetEncoding(1252)!;
}

/// <summary>
/// What a <see cref="VB6FormReader"/> read from a designer file.
/// </summary>
public sealed class VB6FormImport
{
    internal VB6FormImport(FormDefinition? form, UnmodelledComponent? unmodelledRoot, IReadOnlyList<UnmodelledProperty> unmodelledHeader,
        string moduleText, int moduleLine, IReadOnlyList<VB6ImportDiagnostic> diagnostics)
    {
        Form = form;
        UnmodelledRoot = unmodelledRoot;
        UnmodelledHeader = unmodelledHeader;
        ModuleText = moduleText;
        ModuleLine = moduleLine;
        Diagnostics = diagnostics;
    }

    /// <summary>
    /// The form, or <see langword="null"/> when the file holds none; <see cref="Diagnostics"/> then says why.
    /// </summary>
    public FormDefinition? Form { get; }

    /// <summary>
    /// The file's root block kept as text, when its class is not one of the roots of <see cref="VBClasses"/>, or it has no name;
    /// otherwise <see langword="null"/>. <see cref="Form"/> is then <see langword="null"/>.
    /// </summary>
    public UnmodelledComponent? UnmodelledRoot { get; }

    /// <summary>
    /// The <c>Name = Value</c> header lines before the root block that do not name a control library the form's
    /// <see cref="FormDefinition.ControlLibraries"/> lists, kept as they were read. Empty when the file holds no form.
    /// </summary>
    public IReadOnlyList<UnmodelledProperty> UnmodelledHeader { get; }

    /// <summary>
    /// The part of the file after the designer half: its <c>Attribute</c> lines and its code, with the file's own line endings. Empty
    /// when the file has no root block, or its root block has no <c>End</c>.
    /// </summary>
    public string ModuleText { get; }

    /// <summary>
    /// The 1-based line of the file that <see cref="ModuleText"/> starts on.
    /// </summary>
    public int ModuleLine { get; }

    /// <summary>
    /// What the import reports, in the order of the lines it is about; what it reports about one line, in the order it found it.
    /// </summary>
    public IReadOnlyList<VB6ImportDiagnostic> Diagnostics { get; }
}
