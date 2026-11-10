using Avalonia;
using Avalonia.Themes.Simple;

namespace RDCore.Forms.Avalonia;

/// <summary>
/// An Avalonia application for a host that has none of its own: Avalonia's Simple theme, which gives the controls their templates.
/// </summary>
/// <remarks>
/// <para>
/// A host that has an Avalonia application keeps its own: the controls and <see cref="Rendering.VBFormWindow"/> need an application with a theme, not this one.
/// </para>
/// <para>
/// <see cref="Configure"/> builds it with Skia and the Inter font, so that text measures the same on every machine. The host adds the platform: Avalonia's desktop
/// platform to show windows, or its headless platform to show none.
/// </para>
/// </remarks>
public class RDFormsApplication : Application
{
    /// <summary>
    /// Adds the Simple theme to the application's styles.
    /// </summary>
    public override void Initialize() => Styles.Add(new SimpleTheme());

    /// <summary>
    /// The builder of an <see cref="RDFormsApplication"/>, which draws with Skia and whose default font is Inter. The host adds its platform and starts it.
    /// </summary>
    /// <returns>The application's builder.</returns>
    public static AppBuilder Configure() => Configure<RDFormsApplication>();

    /// <summary>
    /// The builder of an application derived from <see cref="RDFormsApplication"/>, which draws with Skia and whose default font is Inter.
    /// </summary>
    /// <typeparam name="TApplication">The application.</typeparam>
    /// <returns>The application's builder.</returns>
    public static AppBuilder Configure<TApplication>()
        where TApplication : RDFormsApplication, new()
        => AppBuilder.Configure<TApplication>().UseSkia().WithInterFont();
}
