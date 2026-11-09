using Avalonia.Controls;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Rendering;

/// <summary>
/// A window that shows a VB form: its <see cref="VBFormView"/>, titled with the form's caption.
/// </summary>
/// <remarks>
/// <para>
/// The window's client area is the form's: its size is the root's <c>Width</c> and <c>Height</c>, in twips at 15 twips a pixel,
/// and follows them. The window follows the root's <c>Caption</c>, <c>MaxButton</c>, <c>MinButton</c> and <c>ShowInTaskbar</c>.
/// Its <c>StartUpPosition</c> chooses where it starts: CenterOwner and CenterScreen centre it, and Manual and Windows Default
/// leave its place to the window system.
/// </para>
/// <para>
/// The controls raise their events to the sink set on the window with <see cref="VBControl.SetEventSink"/>. A window with no sink
/// raises none.
/// </para>
/// </remarks>
public sealed class VBFormWindow : Window
{
    /// <summary>
    /// Creates a window that shows a form.
    /// </summary>
    /// <param name="form">The form.</param>
    /// <exception cref="ArgumentNullException"><paramref name="form"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The root of <paramref name="form"/> is not a <see cref="VBClasses.Form"/>.</exception>
    public VBFormWindow(FormDefinition form)
    {
        ArgumentNullException.ThrowIfNull(form);
        if (!ReferenceEquals(form.Root.Class, VBClasses.Form))
            throw new ArgumentException($"The form's root is a {form.Root.Class.TypeName}, not a VB.Form.", nameof(form));
        View = new VBFormView(form);
        Content = View;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = form.Root.Get(P.StartUpPosition) switch
        {
            StartUpPosition.CenterOwner => WindowStartupLocation.CenterOwner,
            StartUpPosition.CenterScreen => WindowStartupLocation.CenterScreen,
            _ => WindowStartupLocation.Manual,
        };
        Apply();
    }

    /// <summary>
    /// The form's client area.
    /// </summary>
    public VBFormView View { get; }

    // It follows its root while it is open, so that a closed window holds no handler of its root's.
    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        View.Form.Root.PropertyChanged -= OnRootChanged;
        View.Form.Root.PropertyChanged += OnRootChanged;
        Apply();
    }

    /// <inheritdoc/>
    protected override void OnClosed(EventArgs e)
    {
        View.Form.Root.PropertyChanged -= OnRootChanged;
        base.OnClosed(e);
    }

    private void OnRootChanged(object? sender, ComponentPropertyChangedEventArgs e) => Apply();

    private void Apply()
    {
        var root = View.Form.Root;
        Title = root.Get(P.Caption);
        CanMaximize = root.Get(P.MaxButton);
        CanMinimize = root.Get(P.MinButton);
        ShowInTaskbar = root.Get(P.ShowInTaskbar);
    }
}
