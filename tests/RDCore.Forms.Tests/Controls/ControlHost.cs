using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;

namespace RDCore.Forms.Tests.Controls;

// The events a test's controls raise, as "Name.Event(arguments)", in order. A test may answer an event as a handler would, by writing
// its arguments.
public sealed class RecordingSink : IComponentEventSink
{
    public List<string> Raised { get; } = [];

    public Action<ComponentInstance, EventDefinition, FormValue[]>? Handler { get; set; }

    public void Raise(ComponentInstance source, EventDefinition definition, FormValue[] arguments)
    {
        Raised.Add(arguments.Length == 0
            ? $"{source.Name}.{definition.Name}"
            : $"{source.Name}.{definition.Name}({string.Join(", ", arguments.Select(a => a.ToString()))})");
        Handler?.Invoke(source, definition, arguments);
    }

    // The events raised, without the mouse moves a press brings.
    public List<string> WithoutMoves => [.. Raised.Where(e => !e.Contains(".MouseMove"))];
}

// A list of strings, compared as lines so that a failure shows the whole list.
public static class Expect
{
    public static void Lines(IEnumerable<string> expected, IEnumerable<string> actual) =>
        Assert.AreEqual(string.Join(Environment.NewLine, expected), string.Join(Environment.NewLine, actual));
}

// Shows controls on a canvas in a headless window, with a sink for their events.
public static class ControlHost
{
    public static (Window Window, RecordingSink Sink) Show(params (Control Control, Rect Bounds)[] controls)
    {
        var canvas = new Canvas();
        foreach (var (control, bounds) in controls)
        {
            Canvas.SetLeft(control, bounds.X);
            Canvas.SetTop(control, bounds.Y);
            control.Width = bounds.Width;
            control.Height = bounds.Height;
            canvas.Children.Add(control);
        }
        var sink = new RecordingSink();
        var window = new Window { Width = 400, Height = 300, Content = canvas };
        VBControl.SetEventSink(window, sink);
        // A test closes the window when it is done; the focus leaving a control as it closes is not what it looks at.
        window.Closing += (_, _) => VBControl.SetEventSink(window, null);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, sink);
    }

    public static (Window Window, RecordingSink Sink) Show(Control control) => Show((control, new Rect(10, 5, 120, 30)));
}
