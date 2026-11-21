using Avalonia;
using Avalonia.Themes.Simple;

namespace RDCore.Forms.Avalonia;

/// <summary>
/// An Avalonia application for a host that has none of its own: Avalonia's Simple theme, which gives the controls their templates.
/// </summary>
/// <remarks>
/// <para>
/// A host that has an Avalonia application keeps it: the controls and <see cref="Rendering.RDFormsFormWindow"/> need an application with a theme, whichever
/// application it is.
/// </para>
/// <para>
/// <see cref="Configure()"/> builds it with Skia and the Inter font, so that text measures the same on every machine. The host adds the platform: Avalonia's
/// desktop platform to show windows, or its headless platform to show none.
/// </para>
/// <para>
/// The platform shows the application's <see cref="Application.Name"/> where it names the application, such as the application menu of macOS. It is
/// <see cref="DefaultName"/> unless the host names the application (<see cref="Configure(string?)"/>) or a derived class names it in its constructor.
/// </para>
/// </remarks>
public class RDFormsApplication : Application
{
    /// <summary>
    /// The name of an application that neither its host nor a derived class names.
    /// </summary>
    public const string DefaultName = "RDForms";

    /// <summary>
    /// Makes an application named <see cref="DefaultName"/>.
    /// </summary>
    public RDFormsApplication() => Name = DefaultName;

    /// <summary>
    /// Adds the Simple theme to the application's styles.
    /// </summary>
    public override void Initialize() => Styles.Add(new SimpleTheme());

    /// <summary>
    /// The builder of an <see cref="RDFormsApplication"/> named <see cref="DefaultName"/>, which draws with Skia and whose default font is Inter. The host
    /// adds its platform and starts it.
    /// </summary>
    /// <returns>The application's builder.</returns>
    public static AppBuilder Configure() => Configure<RDFormsApplication>(null);

    /// <summary>
    /// The builder of an <see cref="RDFormsApplication"/> named <paramref name="name"/>, which draws with Skia and whose default font is Inter. The host adds
    /// its platform and starts it.
    /// </summary>
    /// <param name="name">
    /// The name of the application, such as the name of the program whose forms it shows; <see cref="DefaultName"/> when it is <see langword="null"/>, empty
    /// or white space.
    /// </param>
    /// <returns>The application's builder.</returns>
    public static AppBuilder Configure(string? name) => Configure<RDFormsApplication>(name);

    /// <summary>
    /// The builder of an application derived from <see cref="RDFormsApplication"/>, named as its constructor names it (<see cref="DefaultName"/> unless the
    /// derived class names it), which draws with Skia and whose default font is Inter.
    /// </summary>
    /// <typeparam name="TApplication">The application.</typeparam>
    /// <returns>The application's builder.</returns>
    public static AppBuilder Configure<TApplication>()
        where TApplication : RDFormsApplication, new()
        => Configure<TApplication>(null);

    /// <summary>
    /// The builder of an application derived from <see cref="RDFormsApplication"/>, named <paramref name="name"/>, which draws with Skia and whose default
    /// font is Inter.
    /// </summary>
    /// <typeparam name="TApplication">The application.</typeparam>
    /// <param name="name">
    /// The name of the application; the name its constructor gives it when it is <see langword="null"/>, empty or white space.
    /// </param>
    /// <returns>The application's builder.</returns>
    public static AppBuilder Configure<TApplication>(string? name)
        where TApplication : RDFormsApplication, new()
        => AppBuilder.Configure(() => Create<TApplication>(name)).UseSkia().WithInterFont();

    /// <summary>
    /// A new application derived from <see cref="RDFormsApplication"/>, named <paramref name="name"/>: the application the builder of
    /// <see cref="Configure{TApplication}(string?)"/> makes.
    /// </summary>
    /// <typeparam name="TApplication">The application.</typeparam>
    /// <param name="name">
    /// The name of the application; the name its constructor gives it when it is <see langword="null"/>, empty or white space.
    /// </param>
    /// <returns>The application.</returns>
    public static TApplication Create<TApplication>(string? name)
        where TApplication : RDFormsApplication, new()
    {
        var application = new TApplication();
        if (!string.IsNullOrWhiteSpace(name))
        {
            application.Name = name;
        }

        return application;
    }
}
