using System.Runtime.CompilerServices;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Drawing;

/// <summary>
/// How a form's or a PictureBox's scale follows what is set on it while it is shown or edited: VB6's rules for <c>ScaleMode</c>,
/// <c>ScaleLeft</c>, <c>ScaleTop</c>, <c>ScaleWidth</c> and <c>ScaleHeight</c>.
/// </summary>
/// <remarks>
/// <para>
/// Setting <c>ScaleLeft</c>, <c>ScaleTop</c>, <c>ScaleWidth</c> or <c>ScaleHeight</c> makes the scale a <see cref="ScaleMode.User"/>
/// one, even to the value it has, and the other three keep theirs. Setting <c>ScaleMode</c> to a unit sets <c>ScaleLeft</c> and
/// <c>ScaleTop</c> to 0, and <c>ScaleWidth</c> and <c>ScaleHeight</c> to the inside in that unit; setting it to
/// <see cref="ScaleMode.User"/> keeps the four.
/// </para>
/// <para>
/// A change of size, <c>BorderStyle</c> or <c>Appearance</c> changes the inside. In a unit, <c>ScaleWidth</c> and <c>ScaleHeight</c>
/// follow it. In a user scale the unit keeps its size: <c>ScaleWidth</c> and <c>ScaleHeight</c> grow and shrink with the inside, and
/// <c>ScaleLeft</c> and <c>ScaleTop</c> stay.
/// </para>
/// <para>
/// A designer file's values are read as it holds them, so a form follows these rules only once <see cref="Follow"/> is called on it.
/// </para>
/// </remarks>
public static class ScaleRules
{
    private static readonly ConditionalWeakTable<ComponentInstance, Follower> Followed = [];

    /// <summary>
    /// Makes a component's scale follow these rules from now on. Where its <c>ScaleMode</c> is a unit, its <c>ScaleWidth</c> and
    /// <c>ScaleHeight</c> are set to its inside in that unit at once.
    /// </summary>
    /// <param name="component">The component. One with no <c>ScaleMode</c>, such as a Frame, has no scale to follow.</param>
    /// <exception cref="ArgumentNullException"><paramref name="component"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// A component follows them once however often it is called, for as long as it lives.
    /// </remarks>
    public static void Follow(ComponentInstance component)
    {
        ArgumentNullException.ThrowIfNull(component);
        if (component.Class.Has(P.ScaleMode))
            _ = Followed.GetValue(component, static followed => new Follower(followed));
    }

    private sealed class Follower
    {
        private readonly ComponentInstance component;
        // The last inside that was not empty, across and down, in twips: a user scale's unit is ScaleWidth over it.
        private (double Width, double Height) inside;
        private bool applying;

        public Follower(ComponentInstance component)
        {
            this.component = component;
            inside = DrawingScale.InsideOf(component);
            if (component.Get(P.ScaleMode) != ScaleMode.User)
            {
                var (width, height) = InUnits();
                if (component.Get(P.ScaleWidth) != width || component.Get(P.ScaleHeight) != height)
                    Apply(() => component.Set(P.ScaleWidth, width).Set(P.ScaleHeight, height));
            }
            component.PropertyChanged += OnChanged;
        }

        private void OnChanged(object? sender, ComponentPropertyChangedEventArgs e)
        {
            if (applying)
                return;
            var property = e.Property;
            var mode = component.Get(P.ScaleMode);
            if (ReferenceEquals(property, P.ScaleLeft) || ReferenceEquals(property, P.ScaleTop)
                || ReferenceEquals(property, P.ScaleWidth) || ReferenceEquals(property, P.ScaleHeight))
            {
                if (mode != ScaleMode.User)
                    Apply(() => component.Set(P.ScaleMode, ScaleMode.User));
            }
            else if (ReferenceEquals(property, P.ScaleMode))
            {
                if (mode != ScaleMode.User)
                {
                    var (width, height) = InUnits();
                    Apply(() => component.Set(P.ScaleLeft, 0d).Set(P.ScaleTop, 0d).Set(P.ScaleWidth, width).Set(P.ScaleHeight, height));
                }
            }
            else if (ReferenceEquals(property, P.Width) || ReferenceEquals(property, P.Height)
                || ReferenceEquals(property, P.BorderStyle) || ReferenceEquals(property, P.Appearance))
            {
                Resized(mode);
            }
        }

        private void Resized(ScaleMode mode)
        {
            var now = DrawingScale.InsideOf(component);
            if (mode != ScaleMode.User)
            {
                var (width, height) = InUnits();
                Apply(() => component.Set(P.ScaleWidth, width).Set(P.ScaleHeight, height));
            }
            else
            {
                // The unit is the scale over the inside before; an empty inside has none, and the next one is measured from the last.
                double scaleWidth = component.Get(P.ScaleWidth), scaleHeight = component.Get(P.ScaleHeight);
                if (now.Width > 0 && inside.Width > 0)
                    scaleWidth = Single(scaleWidth * now.Width / inside.Width);
                if (now.Height > 0 && inside.Height > 0)
                    scaleHeight = Single(scaleHeight * now.Height / inside.Height);
                Apply(() => component.Set(P.ScaleWidth, scaleWidth).Set(P.ScaleHeight, scaleHeight));
            }
            inside = (now.Width > 0 ? now.Width : inside.Width, now.Height > 0 ? now.Height : inside.Height);
        }

        // The inside in the scale's unit, in single precision as VB holds a scale.
        private (double Width, double Height) InUnits()
        {
            var (across, down) = component.Get(P.ScaleMode).TwipsPerUnit();
            var (width, height) = DrawingScale.InsideOf(component);
            return (Single(width / across), Single(height / down));
        }

        private void Apply(Action change)
        {
            applying = true;
            try
            {
                change();
            }
            finally
            {
                applying = false;
            }
        }

        private static double Single(double value) => (float)value;
    }
}
