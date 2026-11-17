using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using RDCore.Forms.Model;
using RDCore.Forms.Porting;
using RDCore.SDK.Runtime.Libraries;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Libraries;

/// <summary>
/// The libraries RDForms describes: <c>VB</c>, the library of the VB6 forms and controls the RDForms model implements.
/// </summary>
/// <remarks>
/// <para>
/// <c>VB</c> is what <c>rdc export-library</c> reads from VB6.OLB, with each property, method and event of a modelled class that VB6.OLB
/// does not declare: Opacity, a Label's Picture, a control's ContextMenu, and the others <see cref="Additions"/> lists, and App's
/// PlatformVersion and ComponentVersion, which read the platform's versions. Each of these is
/// marked <see cref="MemberDescription.IsAddition"/>. The description keeps VB6.OLB's GUID and version, so a project's reference to VB6.OLB
/// finds it. Every member VB6.OLB declares stays in it, whether the model holds the member or not. A setting of VB6's designer that
/// VB6.OLB declares no member for, such as a UserControl's CanGetFocus, is not in it.
/// </para>
/// <para>
/// A member an addition shares its name with in VB6.OLB, such as a PictureBox's Picture for a Label's, gives the addition its type and its
/// accessors. Another addition takes its type from the model.
/// </para>
/// </remarks>
public sealed class RDFormsLibraries : ILibrarySource
{
    /// <summary>
    /// The name of the library of VB6's forms and controls.
    /// </summary>
    public const string VB = "VB";

    /// <summary>
    /// The libraries RDForms describes. A description is built once, the first time it is asked for.
    /// </summary>
    public static RDFormsLibraries Instance { get; } = new();

    private readonly Lazy<LibraryDescription> vb = new(DescribeVB);

    private RDFormsLibraries() { }

    /// <inheritdoc/>
    public IEnumerable<string> Names => [VB];

    /// <inheritdoc/>
    public bool TryGet(string name, [NotNullWhen(true)] out LibraryDescription? description)
    {
        description = string.Equals(name, VB, StringComparison.OrdinalIgnoreCase) ? vb.Value : null;
        return description is not null;
    }

    /// <summary>
    /// What RDForms adds to VB6.OLB's <c>VB</c>: each member and event of a modelled class that VB6.OLB does not declare, as
    /// <c>Class.Member</c>, in the order of VB6.OLB's classes and then of the model's members.
    /// </summary>
    public IEnumerable<string> Additions =>
        vb.Value.Classes.SelectMany(declared => declared.Members.Where(member => member.IsAddition).Select(member => member.Name)
            .Concat(declared.Events.Where(raised => raised.IsAddition).Select(raised => raised.Name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => $"{declared.Name}.{name}"));

    private static LibraryDescription DescribeVB()
    {
        using var stream = typeof(RDFormsLibraries).Assembly.GetManifestResourceStream($"{typeof(RDFormsLibraries).Namespace}.VB.json")
            ?? throw new InvalidOperationException("RDCore.Forms holds no VB.json.");
        using var reader = new StreamReader(stream);
        var exported = LibraryJson.Read(reader.ReadToEnd());
        return exported with
        {
            FormatVersion = LibraryJson.CurrentFormatVersion,
            Classes = [.. exported.Classes.Select(declared => WithAdditions(declared, exported))],
        };
    }

    // A class with what its modelled classes have and VB6.OLB does not declare, then what RD-VBA adds to it; any other class is as exported.
    private static ClassDescription WithAdditions(ClassDescription declared, LibraryDescription exported)
    {
        var modelled = ModelledAs(declared.Name);
        var platform = PlatformAdditions.GetValueOrDefault(declared.Name) ?? [];
        if (modelled.Count == 0 && platform.Length == 0)
            return declared;

        var members = declared.Members.ToList();
        var events = declared.Events.ToList();
        bool Declared(string name) => members.Any(member => string.Equals(member.Name, name, StringComparison.OrdinalIgnoreCase));
        // A method the window answers for comes first: a running program reaches it where a model property has its name.
        foreach (var modelledClass in modelled)
        {
            foreach (var method in modelledClass.Methods.Where(method => !Declared(method.Name)))
                members.AddRange(Added(MethodMembers(method, exported)));
            var settings = DesignerSettings.GetValueOrDefault(declared.Name) ?? [];
            foreach (var property in modelledClass.Properties.Where(property => !modelledClass.IsNotAvailableAtRunTime(property) && !Declared(property.Name)
                && !settings.Contains(property.Name, StringComparer.OrdinalIgnoreCase)))
                members.AddRange(Added(PropertyAccessors(property, modelledClass.IsReadOnlyAtRunTime(property), exported)));
            foreach (var raised in modelledClass.Events.Where(raised => !events.Any(known => string.Equals(known.Name, raised.Name, StringComparison.OrdinalIgnoreCase))))
                events.Add(new EventDescription { Name = raised.Name, IsAddition = true, Parameters = [.. raised.Parameters.Select(parameter => Parameter(parameter.Name, parameter.TypeName))] });
        }
        // Asked of the members before any is added, so that a property's Let follows its Get.
        members.AddRange(Added(platform.Where(member => !Declared(member.Name)).ToList()));
        return declared with { Members = [.. members], Events = [.. events] };
    }

    // What RD-VBA adds to a class of VB6.OLB that no model class implements: App's PlatformVersion and ComponentVersion(Name), read-only, which give the
    // platform's version and that of one of its components ("RDCore", "RDCore.SDK", "RDCore.Forms"), and its KeepSystemTheme, read and written, whether
    // the program's forms take the system's theme variant (True) or stay light (False).
    private static readonly Dictionary<string, MemberDescription[]> PlatformAdditions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["App"] =
        [
            new MemberDescription { Name = "PlatformVersion", Kind = MemberKind.PropertyGet, Type = "String" },
            new MemberDescription { Name = "ComponentVersion", Kind = MemberKind.PropertyGet, Type = "String", Parameters = [Parameter("Name", "String", byVal: true)] },
            new MemberDescription { Name = "KeepSystemTheme", Kind = MemberKind.PropertyGet, Type = "Boolean" },
            new MemberDescription { Name = "KeepSystemTheme", Kind = MemberKind.PropertyLet, Parameters = [Parameter("Value", "Boolean", byVal: true)] },
        ],
    };

    // What VB6's designer sets on a root that VB6.OLB declares no member for, and that RD-VBA adds none for: VB6's documentation has each of a
    // UserControl's "not available at run time", and a UserControl's or a PropertyPage's place is its extender's Left and Top.
    private static readonly Dictionary<string, string[]> DesignerSettings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["UserControl"] = ["Alignable", "CanGetFocus", "ControlContainer", "DataBindingBehavior", "DataSourceBehavior", "DefaultCancel", "EditAtDesignTime",
            "ForwardFocus", "InvisibleAtRuntime", "Public", "ToolboxBitmap", "Windowless", "Left", "Top"],
        ["PropertyPage"] = ["Left", "Top"],
    };

    // The mark, in the one place every added member takes it.
    private static IEnumerable<MemberDescription> Added(IEnumerable<MemberDescription> members) => members.Select(member => member with { IsAddition = true });

    // The model's classes a class of VB6.OLB describes, as VB6's port map names them: VBControlExtender is the extender of every placed UserControl.
    private static IReadOnlyList<ComponentClass> ModelledAs(string name) =>
        string.Equals(name, "VBControlExtender", StringComparison.OrdinalIgnoreCase)
            ? [FullestExtender.Value]
            : FormsPortMap.VB6.TypeOf(name) is { Kind: TypePortKind.Class } port && ComponentClasses.ByTypeName(port.RDFormsName) is { } modelled ? [modelled] : [];

    // The extender of a UserControl whose settings give a placed control every extender member there is.
    private static readonly Lazy<ComponentClass> FullestExtender = new(() => UserControlClasses.Of(
        new FormDefinition(new ComponentInstance(ComponentClasses.UserControl, "UserControl1")
            .Set(P.InvisibleAtRuntime, false).Set(P.CanGetFocus, true).Set(P.ForwardFocus, false).Set(P.DefaultCancel, true).Set(P.Alignable, true)),
        "Project1"));

    // A property's accessors: those of a member of its name on another of VB6.OLB's classes, or a Property Get with a Property Let (or Set) of the
    // model's type.
    private static IEnumerable<MemberDescription> PropertyAccessors(PropertyDefinition property, bool readOnly, LibraryDescription exported)
    {
        if (SharedAccessors(property.Name, exported) is { } shared)
            return shared.Where(accessor => !readOnly || accessor.Kind == MemberKind.PropertyGet).Select(accessor => accessor with { Name = property.Name, DispId = null });

        var type = TypeOf(property.ValueType);
        var get = new MemberDescription { Name = property.Name, Kind = MemberKind.PropertyGet, Type = type };
        return readOnly ? [get] : [get, new MemberDescription { Name = property.Name, Kind = MemberKind.PropertyLet, Parameters = [Parameter("Value", type, byVal: true)] }];
    }

    // The accessors of a property of the name on the first of VB6.OLB's classes that declares one, or null where none does.
    private static List<MemberDescription>? SharedAccessors(string name, LibraryDescription exported) =>
        exported.Classes.Select(declared => declared.Members.Where(member => member.Kind != MemberKind.Method
                && string.Equals(member.Name, name, StringComparison.OrdinalIgnoreCase)).ToList())
            .FirstOrDefault(accessors => accessors.Count > 0);

    // A method, or a property the window answers for, as code calls it. A property whose value the window types as a property of VB6.OLB
    // (ItemIcon's Picture) has that property's accessors and type, after its own parameters: VB6's Picture is a stdole.StdPicture that is
    // read, let and set.
    private static IEnumerable<MemberDescription> MethodMembers(MethodDefinition method, LibraryDescription exported)
    {
        var parameters = method.Parameters.Select(parameter => Parameter(parameter.Name, parameter.TypeName, optional: parameter.IsOptional)).ToImmutableArray();
        if (method.Value is not { } value)
            return [new MemberDescription { Name = method.Name, Kind = MemberKind.Method, Parameters = parameters }];

        if (SharedAccessors(value.TypeName, exported) is { } shared)
            return shared.Where(accessor => !method.IsReadOnly || accessor.Kind == MemberKind.PropertyGet).Select(accessor => accessor with
            {
                Name = method.Name,
                DispId = null,
                Description = null,
                Parameters = [.. parameters, .. accessor.Parameters],
            });

        var get = new MemberDescription { Name = method.Name, Kind = MemberKind.PropertyGet, Type = value.TypeName, Parameters = parameters };
        return method.IsReadOnly
            ? [get]
            : [get, new MemberDescription { Name = method.Name, Kind = MemberKind.PropertyLet, Parameters = [.. parameters, Parameter(value.Name, value.TypeName, byVal: true)] }];
    }

    private static ParameterDescription Parameter(string name, string type, bool byVal = false, bool optional = false) =>
        new() { Name = name, Type = type, IsByVal = byVal, IsOptional = optional };

    // The VB type of a model property's values, for an addition VB6.OLB has no member of the name of.
    private static string TypeOf(Type valueType) => (Nullable.GetUnderlyingType(valueType) ?? valueType) switch
    {
        var type when type == typeof(bool) => "Boolean",
        var type when type == typeof(byte) => "Byte",
        var type when type == typeof(short) => "Integer",
        var type when type == typeof(int) => "Long",
        var type when type == typeof(float) => "Single",
        var type when type == typeof(double) => "Double",
        var type when type == typeof(string) => "String",
        var type when type == typeof(FormColor) => "OLE_COLOR",
        var type when type.IsEnum => "Long",
        _ => "Variant",
    };
}
