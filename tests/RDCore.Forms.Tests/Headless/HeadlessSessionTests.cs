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
