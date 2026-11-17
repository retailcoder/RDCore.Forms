using System.Globalization;
using System.Text;
using RDCore.Forms.Import.VB6;
using RDCore.Forms.Model;
using RDCore.Forms.Porting;
using RDCore.SDK.Forms;
using RDCore.SDK.Workspace.Forms;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Import.MSForms;

/// <summary>
/// Reads a VBA UserForm, as the VBE exports it (its <c>.frm</c> text and its <c>.frx</c> companion), into a <see cref="FormDefinition"/>,
/// through <see cref="FormsPortMap.MSForms"/>.
/// </summary>
public static class MSFormsFormReader
{
    /// <summary>
    /// Reads a UserForm's text and its companion file.
    /// </summary>
    /// <param name="designerFile">The <c>.frm</c> file's text.</param>
    /// <param name="companion">The <c>.frx</c> file's bytes, or <see langword="null"/> when there is none.</param>
    /// <returns>The form, the module text, and what the import reports.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="designerFile"/> is <see langword="null"/>.</exception>
    public static MSFormsFormImport Read(string designerFile, byte[]? companion = null)
    {
        ArgumentNullException.ThrowIfNull(designerFile);
        return Import(designerFile, companion, FormDesignerReferenceOf(designerFile)?.FileName);
    }

    /// <summary>
    /// Reads a UserForm's text with its designer data, as <see cref="MSFormsReader.Read"/> read it from the form's companion file.
    /// </summary>
    /// <param name="designerFile">The <c>.frm</c> file's text.</param>
    /// <param name="design">The form's designer data.</param>
    /// <returns>The form, the module text, and what the import reports.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="designerFile"/> or <paramref name="design"/> is <see langword="null"/>.</exception>
    public static MSFormsFormImport Read(string designerFile, MSFormsDesign design)
    {
        ArgumentNullException.ThrowIfNull(designerFile);
        ArgumentNullException.ThrowIfNull(design);
        return Import(designerFile, (_, _) => design);
    }

    /// <summary>
    /// Reads a UserForm's <c>.frm</c> file, and the companion file its <c>OleObjectBlob</c> cites from the same folder, its name
    /// compared without regard to case. A cited name that is a rooted path, or that holds a NUL, is not read.
    /// </summary>
    /// <param name="path">The <c>.frm</c> file's path.</param>
    /// <param name="encoding">The code page the file is decoded with; Windows-1252 when <see langword="null"/>.</param>
    /// <returns>The form, the module text, and what the import reports.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The file, or a companion file that was found, cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The file, or a companion file that was found, cannot be accessed.</exception>
    public static MSFormsFormImport ReadFile(string path, Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        encoding ??= CodePagesEncodingProvider.Instance.GetEncoding(1252)!;

        var fullPath = Path.GetFullPath(path);
        var text = encoding.GetString(File.ReadAllBytes(fullPath));
        var cited = FormDesignerReferenceOf(text)?.FileName;
        if (cited is null)
        {
            return Import(text, null, null);
        }

        var (lookedFor, file) = VB6FormReader.FindCompanion(Path.GetDirectoryName(fullPath) ?? "", cited);
        return Import(text, file is null ? null : File.ReadAllBytes(file), lookedFor);
    }

    // The companion the text cites; none for a designer block that is not terminated, which the import reports.
    private static FormDesignerReference? FormDesignerReferenceOf(string text)
    {
        try
        {
            return FormDesigner.CompanionOf(text);
        }
        catch (FormDesignerFormatException)
        {
            return null;
        }
    }

    private static MSFormsFormImport Import(string text, byte[]? companion, string? companionName) =>
        Import(text, (formName, diagnostics) => ReadDesign(text, companion, companionName, formName, diagnostics));

    private static MSFormsDesign? ReadDesign(string text, byte[]? companion, string? companionName, string formName,
        MSFormsDiagnosticBag diagnostics)
    {
        if (companionName is null)
        {
            diagnostics.AddMessage(MSFormsImportDiagnosticKind.MissingCompanion, "MissingCompanionNotCited", formName, null, null, formName);
            return null;
        }

        if (companion is null)
        {
            diagnostics.Add(MSFormsImportDiagnosticKind.MissingCompanion, formName, null, formName, companionName);
            return null;
        }

        try
        {
            return MSFormsReader.Read(FormDesigner.StateOf(text, companion).Span);
        }
        catch (FormDesignerFormatException exception) when (exception.Problem is FormDesignerFormatProblem.MissingCompanion or FormDesignerFormatProblem.BlobOutOfRange)
        {
            diagnostics.Add(MSFormsImportDiagnosticKind.MissingCompanion, formName, null, formName, companionName);
        }
        catch (Exception exception) when (exception is FormDesignerFormatException or MSFormsFormatException)
        {
            diagnostics.Add(MSFormsImportDiagnosticKind.UnreadableDesignerData, formName, null, formName, exception.Message);
        }

        return null;
    }

    // The designer data is read once the designer block is known to be a UserForm's, so its diagnostics name the form.
    private static MSFormsFormImport Import(string text, Func<string, MSFormsDiagnosticBag, MSFormsDesign?> design)
    {
        var diagnostics = new MSFormsDiagnosticBag();
        var lines = new DiagnosticBag();
        var file = DesignerFile.Parse(text, lines);
        if (file.Root is not { } root)
        {
            // a file with no designer block is module code throughout: its lines are not a designer's, so none is reported.
            diagnostics.Add(MSFormsImportDiagnosticKind.NoDesignerBlock, "", null);
            return new MSFormsFormImport(null, file.ModuleText, file.ModuleLine, diagnostics.Items);
        }

        foreach (var line in lines.Items)
        {
            diagnostics.AddMessage(MSFormsImportDiagnosticKind.MalformedLine, nameof(MSFormsImportDiagnosticKind.MalformedLine), root.Name,
                null, null, line.Line, line.Message);
        }

        var form = new Importer(diagnostics).Import(root, name => design(name, diagnostics));
        return new MSFormsFormImport(form, file.ModuleText, file.ModuleLine, diagnostics.Items);
    }

    private sealed class Importer(MSFormsDiagnosticBag diagnostics)
    {
        private static FormsPortMap Map => FormsPortMap.MSForms;

        private static readonly string[] ClientRectangle = ["ClientLeft", "ClientTop", "ClientWidth", "ClientHeight"];

        public FormDefinition? Import(DesignerBlock rootBlock, Func<string, MSFormsDesign?> readDesign)
        {
            if (rootBlock.Name.Length == 0)
            {
                diagnostics.AddMessage(MSFormsImportDiagnosticKind.NotAUserForm, "NamelessForm", "", null, null);
                return null;
            }

            if (!Guid.TryParse(rootBlock.TypeName, out var designer) || designer != FormDesigner.MSFormsClassId)
            {
                diagnostics.Add(MSFormsImportDiagnosticKind.NotAUserForm, rootBlock.Name, null, rootBlock.TypeName);
                return null;
            }

            var rootClass = ComponentClasses.ByTypeName(Map.TypeOf("UserForm")!.RDFormsName)!;
            var root = new ComponentInstance(rootClass, rootBlock.Name);
            ReadDesignerBlock(root, rootBlock);
            var form = new FormDefinition(root);

            if (readDesign(rootBlock.Name) is { } design)
            {
                PortProperties(root, "UserForm", design.Form.Properties);
                if (!root.IsSet(P.Width) && !root.IsSet(P.Height))
                {
                    // a .frm with no client rectangle: the form's inside size is its client area's.
                    SetLength(root, P.Width, design.Form.Properties, "InsideWidth");
                    SetLength(root, P.Height, design.Form.Properties, "InsideHeight");
                }

                foreach (var child in design.Form.Children)
                {
                    ImportControl(child, root);
                }
            }

            return form;
        }

        // The designer block's own properties: the caption, the client rectangle in twips (the .frm writes it so), the start-up position.
        private void ReadDesignerBlock(ComponentInstance root, DesignerBlock block)
        {
            foreach (var property in block.Properties)
            {
                var literal = ValueLiteral.Parse(property.Value);
                var name = property.Name;
                var read = (name.ToLowerInvariant(), literal) switch
                {
                    ("caption", ValueLiteral.Text caption) => root.TryRead(P.Caption, caption.Value),
                    ("startupposition", ValueLiteral.Number { IsIntegral: true } position) =>
                        Enum.IsDefined((StartUpPosition)(int)position.Value) && root.TryRead(P.StartUpPosition, (StartUpPosition)(int)position.Value),
                    (_, ValueLiteral.Number twips) when Array.FindIndex(ClientRectangle, c => c.Equals(name, StringComparison.OrdinalIgnoreCase)) is >= 0 and var axis =>
                        root.TryRead(axis switch { 0 => P.Left, 1 => P.Top, 2 => P.Width, _ => P.Height }, Units.FromTwips(twips.Value)),
                    ("oleobjectblob", _) => true,
                    _ => false,
                };

                if (!read)
                {
                    // ShowModal and the VBE's TypeInfoVer among them.
                    root.UnmodelledProperties.Add(new UnmodelledProperty(name, property.Value));
                }
            }
        }

        private void ImportControl(MSFormsControl control, ComponentInstance container)
        {
            var legacyClass = control.Kind.ToString();
            if (ClassOf(control) is not { Kind: ComponentKind.Control } componentClass)
            {
                var typeName = control.Kind == MSFormsControlKind.Other
                    ? "{" + control.ClassId?.ToString().ToUpperInvariant() + "}"
                    : FormDesigner.MSFormsLibrary + "." + legacyClass;
                var held = control.DescendantsAndSelf().Count() - 1;
                diagnostics.AddMessage(MSFormsImportDiagnosticKind.UnmodelledClass, held == 0 ? "UnmodelledClassEmpty" : "UnmodelledClass",
                    control.Name, null, null, control.Name, typeName, held);
                container.UnmodelledContents.Add(new UnmodelledComponent(typeName, control.Name, container.Contents.Count, ""));
                return;
            }

            var instance = new ComponentInstance(componentClass, control.Name);
            instance.SetContainer(container);
            if (control.Site is { } site)
            {
                PortSite(instance, legacyClass, site, control.Properties);
            }

            PortProperties(instance, legacyClass, control.Properties);
            foreach (var child in control.Children)
            {
                if (componentClass.IsContainer)
                {
                    ImportControl(child, instance);
                }
            }
        }

        // The RDForms class the port map gives the control's kind; a ScrollBar's by its orientation and size.
        private ComponentClass? ClassOf(MSFormsControl control)
        {
            if (control.Kind == MSFormsControlKind.Other || Map.TypeOf(control.Kind.ToString()) is not { Kind: TypePortKind.Class } port)
            {
                return null;
            }

            var name = port.RDFormsName;
            if (port.ByOrientation is { } byOrientation)
            {
                var properties = control.Properties;
                var orientation = properties.TryGet<int>("Orientation", out var o) ? o : OrientationPort.Auto;
                var width = properties.TryGet<double>("Width", out var w) ? w : 0;
                var height = properties.TryGet<double>("Height", out var h) ? h : 0;
                if (byOrientation.RDFormsNameOf(orientation, width, height) is not { } oriented)
                {
                    diagnostics.Add(MSFormsImportDiagnosticKind.InvalidValue, control.Name, "Orientation", control.Name, "Orientation", Text(orientation));
                    oriented = byOrientation.RDFormsNameOf(OrientationPort.Auto, width, height)!;
                }

                name = oriented;
            }

            return ComponentClasses.ByTypeName(name);
        }

        // What a parent stores about its control: its place, its flags, its texts, its data sources.
        private void PortSite(ComponentInstance instance, string legacyClass, MSFormsSite site, MSFormsProperties properties)
        {
            var component = instance.Name;
            SetIfChanged(instance, P.Left, Units.FromPoints(site.Left));
            SetIfChanged(instance, P.Top, Units.FromPoints(site.Top));

            // a page's Visible is its tab's, among its properties.
            if (!properties.TryGet<bool>("Visible", out _))
            {
                SetIfChanged(instance, P.Visible, site.Visible);
            }

            SetIfChanged(instance, P.TabStop, site.TabStop);
            if (site.TabIndex is >= 0 and <= short.MaxValue)
            {
                SetIfChanged(instance, P.TabIndex, (short)site.TabIndex);
            }

            SetIfChanged(instance, P.Tag, site.Tag);
            var tooltip = Map.MemberOf(legacyClass, "ControlTipText")?.RDFormsName ?? "ControlTipText";
            SetText(instance, tooltip, "ControlTipText", site.ControlTipText);
            SetFlag(instance, P.Default, site.Default);
            SetFlag(instance, P.Cancel, site.Cancel);
            if (site.HelpContextId != 0 && !(instance.Class.Has(P.HelpContextID) && instance.TryRead(P.HelpContextID, site.HelpContextId)))
            {
                instance.UnmodelledProperties.Add(new UnmodelledProperty("HelpContextID", Text(site.HelpContextId)));
            }

            PortDataSource(instance, P.ControlSource, site.ControlSource, component);
            PortDataSource(instance, P.RowSource, site.RowSource, component);
        }

        // A data source is kept as text; nothing resolves it, so it is reported with the port's diagnostic.
        private void PortDataSource(ComponentInstance instance, PropertyDefinition<string> property, string source, string component)
        {
            if (source.Length == 0)
            {
                return;
            }

            if (!(instance.Class.Has(property) && instance.TryRead(property, source)))
            {
                instance.UnmodelledProperties.Add(new UnmodelledProperty(property.Name, source));
            }

            var port = PortDiagnostic.UnresolvedDataSource(source);
            diagnostics.AddMessage(MSFormsImportDiagnosticKind.UnresolvedDataSource, nameof(MSFormsImportDiagnosticKind.UnresolvedDataSource), component,
                property.Name, port, component, property.Name, port.MessageIn(CultureInfo.CurrentUICulture));
        }

        private static void SetFlag(ComponentInstance instance, PropertyDefinition<bool> property, bool value)
        {
            if (instance.Class.Has(property))
            {
                SetIfChanged(instance, property, value);
            }
            else if (value)
            {
                instance.UnmodelledProperties.Add(new UnmodelledProperty(property.Name, Text(value)));
            }
        }

        private static void SetText(ComponentInstance instance, string rdformsName, string legacyName, string value)
        {
            if (instance.Class.Property(rdformsName) is PropertyDefinition<string> property)
            {
                SetIfChanged(instance, property, value);
            }
            else if (value.Length > 0)
            {
                instance.UnmodelledProperties.Add(new UnmodelledProperty(legacyName, value));
            }
        }

        private static void SetIfChanged<T>(ComponentInstance instance, PropertyDefinition<T> property, T value)
        {
            if (instance.Class.Has(property) && !Equals(instance.Class.DefaultOf(property), value))
            {
                instance.TryRead(property, value);
            }
        }

        private static void SetLength(ComponentInstance instance, PropertyDefinition<double> property, MSFormsProperties properties, string name)
        {
            if (properties.TryGet<double>(name, out var points))
            {
                SetIfChanged(instance, property, Units.FromPoints(points));
            }
        }

        // Every property the control's structure has a value for, stored or the format's default, in the order of their names.
        private void PortProperties(ComponentInstance instance, string legacyClass, MSFormsProperties properties)
        {
            foreach (var name in properties.Names.Order(StringComparer.OrdinalIgnoreCase))
            {
                if (properties.TryGet(name, out var value))
                {
                    PortProperty(instance, legacyClass, name, value, properties.IsStored(name));
                }
            }

            PortCaption(instance, legacyClass, properties);
        }

        private void PortProperty(ComponentInstance instance, string legacyClass, string name, object value, bool stored)
        {
            var component = instance.Name;
            switch (name.ToLowerInvariant())
            {
                // the caption takes its accelerator; a UserForm's caption is its designer block's; a ScrollBar's class is its orientation.
                case "caption" or "accelerator":
                case "insidewidth" or "insideheight" when legacyClass == "UserForm":
                case "orientation" when legacyClass == "ScrollBar":
                    return;

                // the size of an off-screen buffer: RDForms draws with none, so there is nothing to port.
                case "drawbuffer":
                    return;

                // a page fills its MultiPage, so its size is the MultiPage's.
                case "width" or "height" when legacyClass == "Page":
                    return;

                case "tabs" when value is IReadOnlyList<MSFormsTab> tabs && instance.Class.Has(P.Tabs):
                    PortTabs(instance, tabs);
                    return;

                case "zoom" when !IsNumber(value, 100):
                case "imemode" when !IsNumber(value, 0):
                    diagnostics.Add(MSFormsImportDiagnosticKind.NotImplemented, component, name, component, name, Text(value));
                    Keep(instance, name, value);
                    return;

                case "specialeffect":
                    PortSpecialEffect(instance, legacyClass, value, stored);
                    return;

                case "picturesizemode":
                    PortPictureSizeMode(instance, value, stored);
                    return;

                case "groupname" when value is string { Length: > 0 }:
                    diagnostics.Add(MSFormsImportDiagnosticKind.ValueNotPorted, component, name, component, name, Text(value));
                    Keep(instance, name, value);
                    return;

                case "picture" or "mouseicon" when value is MSFormsPicture picture:
                    PortPicture(instance, name, picture);
                    return;
            }

            var member = Map.MemberOf(legacyClass, name);
            if (instance.Class.Property(member?.RDFormsName ?? name) is not { RunTimeOnly: false } definition)
            {
                if (stored)
                {
                    Keep(instance, name, value);
                }

                return;
            }

            var legacy = value;
            if (member?.Value is { } values)
            {
                var conversion = values.Convert(value);
                if (!conversion.IsConvertible)
                {
                    Invalid(instance, name, legacy);
                    return;
                }

                value = conversion.Value!;
            }

            if (!TryConvert(definition.ValueType, value, member?.Unit == PortUnit.Length, out var converted))
            {
                if (stored)
                {
                    Invalid(instance, name, legacy);
                }

                return;
            }

            if (!Equals(instance.Class.DefaultOf(definition), converted) && !instance.TryRead(definition, converted))
            {
                Invalid(instance, name, legacy);
            }
        }

        // MSForms' edge to RDForms': flat and sunken are Appearance's two; a Frame is drawn etched. Any other has no equivalent. A class with
        // no Appearance reports only an edge its form stores: a ToggleButton has the format's default sunken edge, though MSForms gives it no
        // SpecialEffect.
        private void PortSpecialEffect(ComponentInstance instance, string legacyClass, object value, bool stored)
        {
            const int Flat = 0, Sunken = 2, Etched = 3;
            if (instance.Class.Has(P.Appearance) && value is Flat or Sunken)
            {
                SetIfChanged(instance, P.Appearance, value is Flat ? Appearance.Flat : Appearance.ThreeD);
            }
            else if ((stored || instance.Class.Has(P.Appearance)) && (legacyClass == "Frame" ? value is not Etched : value is not Flat))
            {
                diagnostics.Add(MSFormsImportDiagnosticKind.ValueNotPorted, instance.Name, "SpecialEffect", instance.Name, "SpecialEffect", Text(value));
                Keep(instance, "SpecialEffect", value);
            }
        }

        // An Image clips or stretches its picture; RDForms' Image has no zoom.
        private void PortPictureSizeMode(ComponentInstance instance, object value, bool stored)
        {
            const int Clip = 0, Stretch = 1;
            if (instance.Class.Has(P.Stretch) && value is Clip or Stretch)
            {
                SetIfChanged(instance, P.Stretch, value is Stretch);
            }
            else if (instance.Class.Has(P.Stretch))
            {
                diagnostics.Add(MSFormsImportDiagnosticKind.ValueNotPorted, instance.Name, "PictureSizeMode", instance.Name, "PictureSizeMode", Text(value));
                Keep(instance, "PictureSizeMode", value);
            }
            else if (stored)
            {
                Keep(instance, "PictureSizeMode", value);
            }
        }

        private void PortPicture(ComponentInstance instance, string name, MSFormsPicture picture)
        {
            var component = instance.Name;
            var bytes = picture.Data.ToArray();
            if (instance.Class.Property(name) is not PropertyDefinition<byte[]?> definition)
            {
                Keep(instance, name, picture);
                return;
            }

            // a CheckBox or an OptionButton shows a picture as a graphical button only, which MSForms' are not.
            if (definition == P.Picture && instance.Class.Has(P.ButtonStyle) && !instance.Class.Has(P.CommandButtonValue))
            {
                diagnostics.Add(MSFormsImportDiagnosticKind.ValueNotPorted, component, name, component, name, Text(picture));
                Keep(instance, name, picture);
                return;
            }

            if (FormPictures.Inspect(bytes, out _) is not null || !instance.TryRead(definition, bytes))
            {
                Invalid(instance, name, picture);
                return;
            }

            if (picture.Format is MSFormsPictureFormat.Metafile or MSFormsPictureFormat.EnhancedMetafile)
            {
                diagnostics.Add(MSFormsImportDiagnosticKind.MetafilePicture, component, name, component, name);
            }

            // a CommandButton shows its picture when it is graphical; MSForms' always shows it.
            if (definition == P.Picture && instance.Class.Has(P.CommandButtonValue) && instance.Class.Has(P.ButtonStyle))
            {
                instance.TryRead(P.ButtonStyle, ButtonStyle.Graphical);
            }
        }

        // MSForms' caption is literal and its access key a property: RDForms' caption doubles a literal & and marks the key with one.
        private void PortCaption(ComponentInstance instance, string legacyClass, MSFormsProperties properties)
        {
            if (legacyClass == "UserForm" || !properties.TryGet<string>("Caption", out var caption))
            {
                return;
            }

            var key = properties.TryGet<char>("Accelerator", out var accelerator) ? accelerator : '\0';
            var withKey = Map.MemberOf(legacyClass, "Accelerator")?.RDFormsName == "Caption" ? key : '\0';
            var (ported, found) = WithAccessKey(caption, withKey);
            if (withKey != '\0' && !found)
            {
                diagnostics.Add(MSFormsImportDiagnosticKind.AcceleratorNotInCaption, instance.Name, "Accelerator", instance.Name, Text(key));
            }

            if (instance.Class.Has(P.Caption))
            {
                SetIfChanged(instance, P.Caption, ported);
            }
            else if (caption.Length > 0)
            {
                Keep(instance, "Caption", caption);
            }

            if (key != '\0' && (withKey == '\0' || !instance.Class.Has(P.Caption)))
            {
                Keep(instance, "Accelerator", key);
            }
        }

        // A TabStrip's tabs, in their order: each caption takes its accelerator, as a control's does.
        private void PortTabs(ComponentInstance instance, IReadOnlyList<MSFormsTab> tabs)
        {
            var ported = new List<Tab>(tabs.Count);
            foreach (var tab in tabs)
            {
                var (caption, found) = WithAccessKey(tab.Caption, tab.Accelerator);
                if (tab.Accelerator != '\0' && !found)
                {
                    diagnostics.Add(MSFormsImportDiagnosticKind.AcceleratorNotInCaption, instance.Name, "Tabs", tab.Name, Text(tab.Accelerator));
                }

                ported.Add(new Tab(tab.Name, caption) { ToolTipText = tab.ControlTipText, Enabled = tab.Enabled, Visible = tab.Visible, Tag = tab.Tag });
            }

            if (!ported.SequenceEqual(instance.Class.DefaultOf(P.Tabs)) && !instance.TryRead(P.Tabs, ported))
            {
                Invalid(instance, "Tabs", tabs);
            }
        }

        private static (string Caption, bool Found) WithAccessKey(string caption, char key)
        {
            var builder = new StringBuilder(caption.Length + 2);
            var found = false;
            foreach (var character in caption)
            {
                if (character == '&')
                {
                    builder.Append("&&");
                    continue;
                }

                if (!found && key != '\0' && char.ToUpperInvariant(character) == char.ToUpperInvariant(key))
                {
                    builder.Append('&');
                    found = true;
                }

                builder.Append(character);
            }

            return (builder.ToString(), found);
        }

        private static bool IsNumber(object value, long number) =>
            value is int or uint or short or ushort or byte or long && Convert.ToInt64(value, CultureInfo.InvariantCulture) == number;

        private void Invalid(ComponentInstance instance, string name, object value)
        {
            diagnostics.Add(MSFormsImportDiagnosticKind.InvalidValue, instance.Name, name, instance.Name, name, Text(value));
            Keep(instance, name, value);
        }

        private static void Keep(ComponentInstance instance, string name, object value) =>
            instance.UnmodelledProperties.Add(new UnmodelledProperty(name, Text(value)));

        // An MSForms value as the model's property type holds it; a length from points to DIP.
        private static bool TryConvert(Type type, object value, bool isLength, out object? converted)
        {
            converted = (type, value) switch
            {
                _ when type == typeof(double) && value is double length => isLength ? Units.FromPoints(length) : length,
                _ when type == typeof(bool) || type == typeof(bool?) => value switch
                {
                    bool flag => flag,
                    // a CheckBox's or a ToggleButton's Value, as the form stores it.
                    string text => text switch { "1" => true, "0" or "" => false, _ => null },
                    _ => null,
                },
                _ when type == typeof(string) && value is string text => text,
                _ when type == typeof(string) && value is char character => character == '\0' ? "" : character.ToString(),
                _ when type == typeof(short) && value is int number and >= short.MinValue and <= short.MaxValue => (short)number,
                _ when type == typeof(int) && value is int number => number,
                _ when type == typeof(CheckBoxValue) && value is string text => text switch
                {
                    "1" => CheckBoxValue.Checked,
                    "0" or "" => CheckBoxValue.Unchecked,
                    _ => null,
                },
                _ when type.IsEnum && value is int number && Enum.IsDefined(type, Enum.ToObject(type, number)) => Enum.ToObject(type, number),
                _ when type == typeof(FormColor) && value is uint ole && FormColor.TryFromOle(unchecked((int)ole), out var color) => color,
                _ when type == typeof(FormFont) && value is MSFormsFont font => FontOf(font),
                _ => null,
            };

            return converted is not null;
        }

        private static FormFont FontOf(MSFormsFont font) =>
            new(font.Name, font.Size, font.Weight is > 0 and <= 1000 ? font.Weight : font.Bold ? FormFont.BoldWeight : FormFont.NormalWeight,
                font.Italic, font.Underline, font.Strikeout, font.Charset is >= 0 and <= 255 ? font.Charset : 0);

        // A value as an UnmodelledProperty or a message writes it.
        private static string Text(object? value) => value switch
        {
            null => "",
            string text => text,
            char character => character == '\0' ? "" : character.ToString(),
            bool flag => flag ? "True" : "False",
            uint ole => string.Create(CultureInfo.InvariantCulture, $"&H{ole:X8}&"),
            MSFormsFont font => string.Create(CultureInfo.InvariantCulture, $"{font.Name} {font.Size}"),
            MSFormsPicture picture => string.Create(CultureInfo.InvariantCulture, $"{picture.Format} ({picture.Data.Length} bytes)"),
            System.Collections.ICollection items => string.Create(CultureInfo.InvariantCulture, $"({items.Count})"),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "",
        };
    }
}

/// <summary>
/// What an <see cref="MSFormsFormReader"/> read from a UserForm.
/// </summary>
public sealed class MSFormsFormImport
{
    internal MSFormsFormImport(FormDefinition? form, string moduleText, int moduleLine, IReadOnlyList<MSFormsImportDiagnostic> diagnostics)
    {
        Form = form;
        ModuleText = moduleText;
        ModuleLine = moduleLine;
        Diagnostics = diagnostics;
    }

    /// <summary>
    /// The form, an <c>RDForms.Form</c>, or <see langword="null"/> when the file holds no UserForm; <see cref="Diagnostics"/> then says why.
    /// </summary>
    public FormDefinition? Form { get; }

    /// <summary>
    /// The part of the file after the designer block: its <c>Attribute</c> lines and its code, with the file's own line endings.
    /// </summary>
    public string ModuleText { get; }

    /// <summary>
    /// The 1-based line of the file that <see cref="ModuleText"/> starts on.
    /// </summary>
    public int ModuleLine { get; }

    /// <summary>
    /// What the import reports: the form's own, then its controls' in the order the form stores them, depth first.
    /// </summary>
    public IReadOnlyList<MSFormsImportDiagnostic> Diagnostics { get; }
}
