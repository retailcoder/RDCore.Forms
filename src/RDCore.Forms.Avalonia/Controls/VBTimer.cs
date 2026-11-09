using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A VB Timer: a control that shows nothing and raises its <see cref="VBClasses.Timer"/> component's <c>Timer</c> event every
/// <c>Interval</c> milliseconds.
/// </summary>
/// <remarks>
/// <para>
/// It runs while it is on a shown form, its own <c>Enabled</c> is True and its <c>Interval</c> is more than 0. Setting
/// <c>Enabled</c> or <c>Interval</c> to a new value starts the interval again, and stopping it drops a tick not yet raised.
/// </para>
/// <para>
/// The interval is counted on the clock <see cref="VBControl.GetTimeProvider"/> gives, the system's timer unless one is set, so a
/// tick is as precise as that timer: on Windows it lands on the system clock's tick, about every 15.6 milliseconds, as a VB6 Timer's
/// does. A tick is raised on the UI thread after its input and rendering. A tick that comes while the one before it is still waiting
/// to be raised is dropped, so a Timer whose event takes longer than its interval raises it again once that event returns, never
/// several times in a row.
/// </para>
/// </remarks>
public sealed class VBTimer : Control
{
    private int interval;
    private bool enabled;
    private bool attached;
    private ITimer? timer;
    // The running timer's token: a tick posted by a timer since stopped is not raised.
    private object? running;
    // 1 while a tick waits to be raised. The clock's callback sets it on a thread of its own.
    private int pending;

    /// <summary>
    /// Creates a control that shows a Timer.
    /// </summary>
    /// <param name="instance">The Timer.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a Timer.</exception>
    public VBTimer(ComponentInstance instance)
    {
        IsVisible = false;
        IsHitTestVisible = false;
        Focusable = false;
        ComponentBinding.Bind(this, instance, VBClasses.Timer, Apply);
    }

    /// <summary>
    /// Whether it is counting its interval.
    /// </summary>
    public bool IsRunning => timer is not null;

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        attached = true;
        Restart();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        attached = false;
        Restart();
        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VBControl.TimeProviderProperty && attached)
            Restart();
    }

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        // Its Enabled is its own: it is not the control's IsEnabled, which a disabled container would take away.
        switch (e.Property.Name)
        {
            case nameof(P.Enabled):
                enabled = (bool)e.After!;
                break;
            case nameof(P.Interval):
                interval = (int)e.After!;
                break;
            default:
                return;
        }
        // The values are applied again, unchanged, as the control joins a form; only a change starts the interval again.
        if (!Equals(e.Before, e.After))
            Restart();
    }

    private bool ShouldRun => attached && enabled && interval > 0;

    private void Restart()
    {
        timer?.Dispose();
        timer = null;
        running = null;
        if (!ShouldRun)
            return;
        var token = new object();
        var period = TimeSpan.FromMilliseconds(interval);
        running = token;
        timer = VBControl.GetTimeProvider(this).CreateTimer(_ => Elapsed(token), null, period, period);
    }

    private void Elapsed(object token)
    {
        if (Interlocked.Exchange(ref pending, 1) == 1)
            return;
        Dispatcher.UIThread.Post(() => Tick(token), DispatcherPriority.Background);
    }

    private void Tick(object token)
    {
        Volatile.Write(ref pending, 0);
        if (ReferenceEquals(token, running))
            VBControl.Raise(this, VBEvents.Timer);
    }
}
