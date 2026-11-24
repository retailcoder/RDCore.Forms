using System.IO.Abstractions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A placed UserControl: a control that shows a component of a class <see cref="UserControlClasses.Of"/> made, with its own copy of
/// that class's UserControl (its <see cref="UserControlSite"/>'s), and holds the controls placed in it.
/// </summary>
/// <remarks>
/// <para>
/// The copy (<see cref="UserControl"/>) is shown by a <see cref="RDFormsFormView"/> that fills the control: the UserControl's own controls,
/// its <c>BackColor</c>, or what is behind it where its <c>BackStyle</c> is Transparent, its <c>Picture</c> and its drawing. The copy's
/// root takes the placed control's <c>Width</c> and <c>Height</c>, and a UserControl whose <c>Enabled</c> is False takes no input in
/// any of its controls.
/// </para>
/// <para>
/// The controls a form places in it, where its UserControl is a container, are shown over the UserControl's own, each placed from the
/// UserControl's top left corner.
/// </para>
/// <para>
/// A UserControl whose controls hold, at any depth, a control placed from its own UserControl, is shown as a placeholder where it
/// holds itself: a box of the control's size that names its class, such as <c>Project1.Gauge</c>.
/// </para>
/// <para>
/// It takes no focus, and Tab passes over it; a press on one of its UserControl's controls gives that control the focus. Its
/// UserControl's root and controls raise their events to the window's sink, each with its component of the copy as the source.
/// </para>
/// </remarks>
public sealed class RDFormsUserControl : Control, IRDFormsContainer
{
    // The UserControls whose copies are being made on this thread, outermost first: one met again holds itself.
    [ThreadStatic]
    private static HashSet<FormDefinition>? making;

    /// <summary>
    /// Creates a control that shows a placed UserControl.
    /// </summary>
    /// <param name="instance">The placed UserControl.</param>
    /// <param name="fileSystem">The file system the file lists of its UserControl show, from its current directory.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> or <paramref name="fileSystem"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="instance"/>'s class was not made by <see cref="UserControlClasses.Of"/>, or is invisible at run time.
    /// </exception>
    public RDFormsUserControl(ComponentInstance instance, IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(fileSystem);
        var definition = UserControlClasses.DefinitionOf(instance.Class)
                         ?? throw new ArgumentException($"{instance.Class.TypeName} is not a placed UserControl's class.", nameof(instance));
        if (!instance.Class.HasExtent)
            throw new ArgumentException($"{instance.Class.TypeName} is invisible at run time.", nameof(instance));

        UserControl = UserControlSite.Of(instance).UserControl;
        var panel = new Panel();
        making ??= new(ReferenceEqualityComparer.Instance);
        if (making.Add(definition))
        {
            try
            {
                // In its container's theme variant.
                View = new RDFormsFormView(UserControl, fileSystem) { KeepSystemTheme = true };
                panel.Children.Add(View);
            }
            finally
            {
                making.Remove(definition);
            }
        }
        else
        {
            panel.Children.Add(Placeholder(instance.Class.TypeName));
        }
        panel.Children.Add(ChildHost);
        VisualChildren.Add(panel);
        LogicalChildren.Add(panel);
        ComponentBinding.Bind(this, instance, instance.Class, Apply);
    }

    /// <summary>
    /// The placed control's own copy of its class's UserControl: its <see cref="UserControlSite.UserControl"/>.
    /// </summary>
    public FormDefinition UserControl { get; }

    /// <summary>
    /// The view that shows <see cref="UserControl"/>, or <see langword="null"/> where the control is shown as a placeholder.
    /// </summary>
    public RDFormsFormView? View { get; }

    /// <inheritdoc/>
    public Canvas ChildHost { get; } = new();

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        switch (e.Property.Name)
        {
            case nameof(P.Width):
                UserControl.Root.Set(P.Width, (double)e.After!);
                break;
            case nameof(P.Height):
                UserControl.Root.Set(P.Height, (double)e.After!);
                break;
        }
        ComponentBinding.ApplyCommon(this, e.Property, e.After);
    }

    // RDForms' own: a box that names the class, in the button face's colour, in its theme variant.
    private static Border Placeholder(string typeName)
    {
        var name = new TextBlock
        {
            Text = typeName,
            Margin = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        var box = new Border { BorderThickness = new Thickness(1), ClipToBounds = true, Child = name };
        ComponentBinding.FollowVariant(box, () =>
        {
            box.Background = ComponentBinding.Brush(box, SystemColor.ButtonFace);
            box.BorderBrush = ComponentBinding.Brush(box, SystemColor.ButtonShadow);
            name.Foreground = ComponentBinding.Brush(box, SystemColor.ButtonText);
        });
        return box;
    }
}
