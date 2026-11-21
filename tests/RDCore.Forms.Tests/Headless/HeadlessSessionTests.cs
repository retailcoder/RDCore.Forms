using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Themes.Simple;
using RDCore.Forms.Avalonia;

namespace RDCore.Forms.Tests.Headless;

[TestClass]
public sealed class HeadlessSessionTests
{
    [TestMethod]
    public void TheSession_RunsTheShippedApplication_WhoseThemeGivesTheControlsTheirTemplates()
    {
        var (application, themed, templated) = Headless.Run(() =>
        {
            var box = new TextBox();
            var window = new Window { Content = box };
            window.Show();
            var result = (Application.Current is RDFormsApplication, Application.Current!.Styles.OfType<SimpleTheme>().Any(), box.Template is not null);
            window.Close();
            return result;
        });

        Assert.IsTrue(application);
        Assert.IsTrue(themed);
        Assert.IsTrue(templated);
    }

    // the name the platform shows where it names the application, as macOS names its application menu: Avalonia's own is "Avalonia Application".
    [TestMethod]
    public void TheSessionsApplication_BuiltWithNoName_IsNamedTheDefaultName()
    {
        var (built, constructed) = Headless.Run(() => (Application.Current!.Name, new HeadlessTestApp().Name));

        Assert.AreEqual(RDFormsApplication.DefaultName, constructed, "HeadlessTestApp names itself; rewrite this test to build an application that does not.");
        Assert.AreEqual(RDFormsApplication.DefaultName, built, "HeadlessTestApp.BuildAvaloniaApp builds with Configure<HeadlessTestApp>() and no name.");
    }

    [TestMethod]
    [DataRow("Ducky", "Ducky", DisplayName = "the name given")]
    [DataRow(null, RDFormsApplication.DefaultName, DisplayName = "no name")]
    [DataRow("", RDFormsApplication.DefaultName, DisplayName = "an empty name")]
    [DataRow("  ", RDFormsApplication.DefaultName, DisplayName = "white space")]
    public void AnApplication_IsNamedAsItsHostNamesIt_OrTheDefaultName(string? name, string expected)
        => Assert.AreEqual(expected, Headless.Run(() => RDFormsApplication.Create<RDFormsApplication>(name).Name));

    [TestMethod]
    [DataRow("Ducky", "Ducky", DisplayName = "the name given")]
    [DataRow(null, "Named in its constructor", DisplayName = "no name")]
    [DataRow("  ", "Named in its constructor", DisplayName = "white space")]
    public void ADerivedApplication_NamedInItsConstructor_KeepsItsName_ItsHostNamingItNone(string? name, string expected)
        => Assert.AreEqual(expected, Headless.Run(() => RDFormsApplication.Create<SelfNamedApplication>(name).Name));

    private sealed class SelfNamedApplication : RDFormsApplication
    {
        public SelfNamedApplication() => Name = "Named in its constructor";
    }

    [TestMethod]
    public void AWindowShown_InTheHeadlessSession_TakesTypedText()
    {
        var text = Headless.Run(() =>
        {
            var box = new TextBox();
            var window = new Window { Content = box };
            window.Show();
            box.Focus();

            window.KeyTextInput("abc");
            window.Close();
            return box.Text;
        });

        Assert.AreEqual("abc", text);
    }
}
