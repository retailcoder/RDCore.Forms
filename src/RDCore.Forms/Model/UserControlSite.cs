using System.Globalization;
using System.Runtime.CompilerServices;
using RDCore.Forms.Import.VB6;
using RDCore.SDK.Runtime.Stdole;
using RDCore.SDK.Runtime.Vbrun;

namespace RDCore.Forms.Model;

/// <summary>
/// A placed UserControl's site: the placed control, its own copy of its class's UserControl, and the <c>Ambient</c> and
/// <c>PropertyBag</c> its form gives the UserControl's code, as VB6 gives them on a running form.
/// </summary>
/// <remarks>
/// <para>
/// Every placed UserControl has one, an InvisibleAtRuntime one too, made the first time <see cref="Of"/> is asked for it and kept for
/// as long as the placed control lives. A placed control's view shows its site's <see cref="UserControl"/>.
/// </para>
/// <para>
/// No event is raised through the site: a placed UserControl raises none of its own events (Initialize, ReadProperties, Resize, Show,
/// Paint, Terminate) and none of its focus events.
/// </para>
/// </remarks>
public sealed class UserControlSite
{
    // Each placed control's site, for as long as the placed control lives.
    private static readonly ConditionalWeakTable<ComponentInstance, UserControlSite> sites = [];

    // HIMETRIC units, hundredths of a millimeter, to the pixel, at 96 pixels to the inch.
    private const double HimetricPerPixel = 2540.0 / 96;

    private UserControlSite(ComponentInstance placed, FormDefinition definition)
    {
        Extender = placed;
        UserControl = definition.Copy();
        PropertyBag = new PropertyBagObject<FormValue>(BagValues(placed));
        Ambient = new AmbientPropertiesObject(new FormAmbient(placed));
    }

    /// <summary>
    /// The site of a placed UserControl, made the first time it is asked for.
    /// </summary>
    /// <param name="placed">The placed UserControl: a component of a class <see cref="UserControlClasses.Of"/> made.</param>
    /// <returns>The same site for the same placed control, each time.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="placed"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="placed"/>'s class was not made by <see cref="UserControlClasses.Of"/>.</exception>
    public static UserControlSite Of(ComponentInstance placed)
    {
        ArgumentNullException.ThrowIfNull(placed);
        var definition = UserControlClasses.DefinitionOf(placed.Class)
                         ?? throw new ArgumentException($"{placed.Class.TypeName} is not a placed UserControl's class.", nameof(placed));
        return sites.GetValue(placed, p => new UserControlSite(p, definition));
    }

    /// <summary>
    /// The control the form places: what VB calls the UserControl's <c>Extender</c>, with the members its settings give it.
    /// </summary>
    public ComponentInstance Extender { get; }

    /// <summary>
    /// The placed control's own copy of its class's UserControl (<see cref="FormDefinition.Copy"/>): a change to it leaves the class's
    /// UserControl and the other placed controls as they are.
    /// </summary>
    public FormDefinition UserControl { get; }

    /// <summary>
    /// The <c>PropertyBag</c> the UserControl's <c>ReadProperties</c> reads: the values the form's file keeps for the placed control,
    /// typed as VB6 types the file's text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bag holds each setting of the placed control that is not the extender's: each of its
    /// <see cref="ComponentInstance.UnmodelledProperties"/> that its class does not have, other than <c>Index</c>. Where the file sets a
    /// name twice, the bag holds the last.
    /// </para>
    /// <list type="table">
    /// <listheader><term>The file's text</term><description>The value read</description></listheader>
    /// <item><term>A quoted string, such as <c>"007"</c></term><description>A String.</description></item>
    /// <item>
    /// <term>A whole number of -32768 to 32767 without <c>&amp;</c>, such as <c>42</c> or <c>-1 'True</c></term>
    /// <description>An Integer.</description>
    /// </item>
    /// <item>
    /// <term>Another whole number a Long holds, or one written with <c>&amp;</c>, such as <c>&amp;H8000000F&amp;</c></term>
    /// <description>A Long.</description>
    /// </item>
    /// <item><term>Another number a Currency holds, such as <c>1.5</c></term><description>A Currency.</description></item>
    /// <item><term>Any other number</term><description>A Double.</description></item>
    /// <item>
    /// <term>A <c>stdole.StdFont</c> bag</term>
    /// <description>A new <see cref="StdFontObject"/> of the bag's font, each time it is read.</description>
    /// </item>
    /// <item>
    /// <term><c>_ExtentX</c> and <c>_ExtentY</c> of a control whose UserControl's <c>InvisibleAtRuntime</c> is False</term>
    /// <description>
    /// A Long: the placed control's width or height in whole pixels, as HIMETRIC units (133 pixels read 3519), whatever the file writes.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// <b>Not implemented.</b> A record of the companion file (<c>"Form1.frx":0000</c>), a bag of another class and any other text are not
    /// in the bag.
    /// </para>
    /// </remarks>
    public PropertyBagObject<FormValue> PropertyBag { get; }

    /// <summary>
    /// The <c>Ambient</c> of the UserControl's code: the ambient properties of the placed control's form, read from the form each time
    /// they are read.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Member</term><description>Value</description></listheader>
    /// <item><term><c>BackColor</c>, <c>ForeColor</c></term><description>The form's, as an unsigned <c>OLE_COLOR</c>.</description></item>
    /// <item><term><c>Font</c></term><description>A copy of the form's <c>Font</c>.</description></item>
    /// <item><term><c>RightToLeft</c></term><description>The form's, where its class has one; otherwise False, as for a Form.</description></item>
    /// <item><term><c>ScaleUnits</c></term><description>The name of the form's <c>ScaleMode</c>, such as <c>Twip</c> or <c>Pixel</c>.</description></item>
    /// <item><term><c>DisplayName</c></term><description>The placed control's name.</description></item>
    /// <item>
    /// <term><c>DisplayAsDefault</c></term>
    /// <description>The placed control's <c>Default</c>, for a UserControl whose <c>DefaultCancel</c> is True; otherwise False.</description>
    /// </item>
    /// <item><term><c>LocaleID</c></term><description>The locale of the culture the program runs in.</description></item>
    /// </list>
    /// </remarks>
    public AmbientPropertiesObject Ambient { get; }

    private static List<KeyValuePair<string, Func<FormValue>>> BagValues(ComponentInstance placed)
    {
        var values = new Dictionary<string, Func<FormValue>>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in placed.UnmodelledProperties)
        {
            if (placed.Class.Property(property.Name) is null && !property.Name.Equals("Index", StringComparison.OrdinalIgnoreCase)
                && DesignerValue.TryType(property.Text, out var value))
            {
                // A StdFont is the UserControl's own object: each read gives a new one.
                var text = property.Text;
                values[property.Name] = value.Kind == FormValueKind.Object ? () => Typed(text) : () => value;
            }
        }

        if (placed.Class.Has(Properties.Width))
        {
            values["_ExtentX"] = () => Himetric(placed.Get(Properties.Width));
            values["_ExtentY"] = () => Himetric(placed.Get(Properties.Height));
        }

        return [.. values];
    }

    private static FormValue Typed(string text) =>
        DesignerValue.TryType(text, out var value) ? value : throw new InvalidOperationException($"{text} gives no value.");

    // A size in a form's pixels, as the whole pixels VB6 gives the control, in HIMETRIC units.
    private static FormValue Himetric(double pixels) =>
        FormValue.FromLong((int)Math.Round(Math.Round(pixels, MidpointRounding.AwayFromZero) * HimetricPerPixel, MidpointRounding.AwayFromZero));

    // The placed control's form, as the Ambient object reads it.
    private sealed class FormAmbient(ComponentInstance placed) : IAmbientContainer
    {
        public uint BackColor => Color(Properties.BackColor);

        public uint ForeColor => Color(Properties.ForeColor);

        public StdFontObject Font =>
            (Form.Class.Has(Properties.Font) ? Form.Get(Properties.Font) : FormFont.Default).TryToStdFont(out var font) == StdFontFailure.None
                ? font!
                : new StdFontObject();

        public bool RightToLeft => Form.Class.Has(Properties.RightToLeft) && Form.Get(Properties.RightToLeft);

        public string ScaleUnits => (Form.Class.Has(Properties.ScaleMode) ? Form.Get(Properties.ScaleMode) : ScaleMode.Twip).ToString();

        public string DisplayName => placed.Name;

        public bool DisplayAsDefault => placed.Class.Has(Properties.Default) && placed.Get(Properties.Default);

        public int LocaleID => CultureInfo.CurrentCulture.LCID;

        private ComponentInstance Form
        {
            get
            {
                var form = placed;
                while (form.Container is { } container)
                {
                    form = container;
                }

                return form;
            }
        }

        private uint Color(PropertyDefinition<FormColor> property) =>
            Form.Class.Has(property) ? unchecked((uint)Form.Get(property).ToOle()) : 0;
    }
}
