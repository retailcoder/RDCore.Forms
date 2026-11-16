using System.Runtime.CompilerServices;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Model.VB;

/// <summary>
/// The classes of the controls a project's UserControls place on its forms.
/// </summary>
/// <remarks>
/// A placed UserControl's members are those of the extender VB6 wraps it in, and the extender's surface depends on the UserControl's
/// settings, as measured with VB6:
/// <list type="table">
/// <listheader><term>Setting</term><description>Members of a placed control</description></listheader>
/// <item><term>Every placed control</term><description><c>Left</c> and <c>Top</c>.</description></item>
/// <item>
/// <term><c>InvisibleAtRuntime</c> False</term>
/// <description><c>Width</c>, <c>Height</c>, <c>Visible</c>, <c>DragMode</c>, <c>ToolTipText</c>, <c>WhatsThisHelpID</c> and <c>Tag</c>, and RD-VBA's
/// <c>Opacity</c>.</description>
/// </item>
/// <item>
/// <term><c>InvisibleAtRuntime</c> True</term>
/// <description>Nothing else: a program can read neither <c>Left</c> nor <c>Top</c>, and the control has no extent.</description>
/// </item>
/// <item>
/// <term><c>CanGetFocus</c> True or <c>ForwardFocus</c> True, and visible</term>
/// <description><c>TabIndex</c>.</description>
/// </item>
/// <item>
/// <term><c>CanGetFocus</c> True, <c>ForwardFocus</c> False, and visible</term>
/// <description><c>TabStop</c>, <c>CausesValidation</c>, <c>HelpContextID</c>, <c>SetFocus</c>, and the <c>GotFocus</c> and <c>LostFocus</c> events.</description>
/// </item>
/// <item><term><c>DefaultCancel</c> True</term><description><c>Default</c> and <c>Cancel</c>.</description></item>
/// <item><term><c>Alignable</c> True</term><description><c>Align</c> and <c>Negotiate</c>.</description></item>
/// </list>
/// A placed control has no <c>Enabled</c>, <c>hWnd</c> or data-binding member, whatever the settings.
/// </remarks>
public static class UserControlClasses
{
    /// <summary>
    /// The class of the controls <paramref name="userControl"/> places: <c>Project1.UserControl1</c> for <c>UserControl1</c> of
    /// <c>Project1</c>, with the extender members its settings give it. It is a container when the UserControl's
    /// <c>ControlContainer</c> is True, and windowless when its <c>Windowless</c> is.
    /// </summary>
    /// <param name="userControl">The UserControl, as its designer file defines it.</param>
    /// <param name="projectName">The name of the project the UserControl is in.</param>
    /// <param name="events">The events the UserControl's code declares, which a placed control raises; none when <see langword="null"/>.</param>
    /// <returns>The class, which has no default member and no method but <c>SetFocus</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="userControl"/> or <paramref name="projectName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="userControl"/>'s root is not a UserControl; <paramref name="projectName"/> is empty or white space; or two of
    /// <paramref name="events"/>, or one of them and <c>GotFocus</c> or <c>LostFocus</c> of a control that has them, share a name.
    /// </exception>
    public static ComponentClass Of(FormDefinition userControl, string projectName, IReadOnlyList<EventDefinition>? events = null)
    {
        ArgumentNullException.ThrowIfNull(userControl);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);
        var root = userControl.Root;
        if (root.Class != VBClasses.UserControl)
            throw new ArgumentException($"{root.Name} is a {root.Class.TypeName}, not a UserControl.", nameof(userControl));

        var invisible = root.Get(P.InvisibleAtRuntime);
        var canGetFocus = root.Get(P.CanGetFocus);
        var forwardFocus = root.Get(P.ForwardFocus);
        var takesFocus = canGetFocus && !forwardFocus && !invisible;

        List<PropertyDefinition> properties = [P.Left, P.Top];
        if (!invisible)
            properties.AddRange([P.Width, P.Height, P.Visible, P.Opacity, P.DragMode, P.ToolTipText, P.WhatsThisHelpID, P.Tag]);
        if (!invisible && (canGetFocus || forwardFocus))
            properties.Add(P.TabIndex);
        if (takesFocus)
            properties.AddRange([P.TabStop, P.CausesValidation, P.HelpContextID]);
        if (root.Get(P.DefaultCancel))
            properties.AddRange([P.Default, P.Cancel]);
        if (root.Get(P.Alignable))
            properties.AddRange([P.Align, P.Negotiate]);

        EventDefinition[] focusEvents = takesFocus ? [VBEvents.GotFocus, VBEvents.LostFocus] : [];
        var placed = new ComponentClass($"{projectName}.{root.Name}", root.Name, ComponentKind.Control, properties,
            [.. events ?? [], .. focusEvents],
            isContainer: root.Get(P.ControlContainer), isWindowless: root.Get(P.Windowless), hasExtent: !invisible,
            methods: takesFocus ? [VBMethods.SetFocus] : [], notAvailableAtRunTime: invisible ? [P.Left, P.Top] : null);
        definitions.Add(placed, userControl);
        return placed;
    }

    /// <summary>
    /// The UserControl a class of placed controls was made from by <see cref="Of"/>.
    /// </summary>
    /// <param name="placed">The class.</param>
    /// <returns>The UserControl's definition, or <see langword="null"/> for a class <see cref="Of"/> did not make.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="placed"/> is <see langword="null"/>.</exception>
    public static FormDefinition? DefinitionOf(ComponentClass placed)
    {
        ArgumentNullException.ThrowIfNull(placed);
        return definitions.TryGetValue(placed, out var definition) ? definition : null;
    }

    // Each class's UserControl, for as long as the class lives.
    private static readonly ConditionalWeakTable<ComponentClass, FormDefinition> definitions = [];
}
