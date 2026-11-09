using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Simple;

namespace RDCore.Forms.Tests.Headless;

// The Avalonia application of the control tests: Avalonia's headless platform, with no window system, so the tests run the same on every OS.
public sealed class HeadlessTestApp : Application
{
    public override void Initialize() => Styles.Add(new SimpleTheme());

    // HeadlessUnitTestSession.StartNew finds the application's builder by this name.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<HeadlessTestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true });
}

// The one headless session of the test run: Avalonia sets its platform up once per process. A test's UI work runs on the session's
// dispatcher thread, and what it throws is rethrown to the test.
public static class Headless
{
    private static readonly Lazy<HeadlessUnitTestSession> session = new(() => HeadlessUnitTestSession.StartNew(typeof(HeadlessTestApp)));

    public static void Run(Action test) => session.Value.Dispatch(test, CancellationToken.None).GetAwaiter().GetResult();

    public static T Run<T>(Func<T> test) => session.Value.Dispatch(test, CancellationToken.None).GetAwaiter().GetResult();
}
