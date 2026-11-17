using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Rendering;

[TestClass]
public sealed class KeepSystemThemeTests
{
    // What a form shows in system colours: a CommandButton's face and caption, a TextBox's back colour, a Label's caption, the root's
    // back colour, and the menu bar's face and the caption of its item.
    private readonly record struct Look(ThemeVariant? Variant, Color Face, Color Caption, Color TextBack, Color LabelText, Color RootBack,
        Color Bar, Color BarText);

    private static ComponentInstance Probe()
    {
        var root = new ComponentInstance(ComponentClasses.Form, "Form1").Set(P.Width, 300d).Set(P.Height, 200d);
        new ComponentInstance(ComponentClasses.CommandButton, "Command1").Set(P.Caption, "OK").SetContainer(root);
        new ComponentInstance(ComponentClasses.TextBox, "Text1").Set(P.Top, 40d).SetContainer(root);
        new ComponentInstance(ComponentClasses.Label, "Label1").Set(P.Caption, "Name").Set(P.Top, 80d).SetContainer(root);
        new ComponentInstance(ComponentClasses.Menu, "mnuFile").Set(P.Caption, "&File").SetContainer(root);
        return root;
    }

    private static Color Of(IBrush? brush) => ((ISolidColorBrush)brush!).Color;

    private static Look LookOf(RDFormsFormWindow window)
    {
        Control Named(string name) => window.View.ControlOf(window.Root.Contents.Single(c => c.Name == name))!;
        var button = (TemplatedControl)Named("Command1");
        var item = window.MenuBar.Items.OfType<MenuItem>().Single();
        return new Look(window.View.ActualThemeVariant, Of(button.Background), Of(button.Foreground), Of(((TemplatedControl)Named("Text1")).Background),
            Of(Named("Label1").GetValue(TextElement.ForegroundProperty)), Of(window.View.Background), Of(window.MenuBar.Background), Of(item.Foreground));
    }

    // The look of the probe drawn with one palette: the root's and the controls' own colours, read from their components.
    private static Look Expected(ComponentInstance root, ThemeVariant variant)
    {
        Color System(SystemColor color) => FormPalette.ToColor(FormColor.FromSystem(color), variant);
        Color Own(string name, PropertyDefinition<FormColor> property) => FormPalette.ToColor(root.Contents.Single(c => c.Name == name).Get(property), variant);
        return new Look(variant, System(SystemColor.ButtonFace), System(SystemColor.ButtonText), Own("Text1", P.BackColor), Own("Label1", P.ForeColor),
            FormPalette.ToColor(root.Get(P.BackColor), variant), System(SystemColor.Menu), System(SystemColor.MenuText));
    }

    // Runs a test under an application theme variant, and gives the application its own back.
    private static T Under<T>(ThemeVariant application, Func<T> test) => UI.Run(() =>
    {
        var app = Application.Current!;
        var before = app.RequestedThemeVariant;
        app.RequestedThemeVariant = application;
        try
        {
            Dispatcher.UIThread.RunJobs();
            return test();
        }
        finally
        {
            app.RequestedThemeVariant = before;
            Dispatcher.UIThread.RunJobs();
        }
    });

    private static RDFormsFormWindow Open(ComponentInstance root, bool keep)
    {
        var window = new RDFormsFormWindow(new FormDefinition(root)) { KeepSystemTheme = keep };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static ThemeVariant Variant(string name) => name == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    [TestMethod]
    [DataRow(false, "Light", "Light")]
    [DataRow(false, "Dark", "Light")]
    [DataRow(true, "Light", "Light")]
    [DataRow(true, "Dark", "Dark")]
    public void AFormsSystemColours_AreTheLightPalettes_UnlessItKeepsTheSystemTheme_ThenTheApplicationVariants(bool keep, string application, string drawn)
    {
        var root = Probe();
        var look = Under(Variant(application), () =>
        {
            var window = Open(root, keep);
            var look = LookOf(window);
            window.Close();
            return look;
        });

        Assert.AreEqual(Expected(root, Variant(drawn)), look);
    }

    [TestMethod]
    public void AShownForm_IsDrawnAgain_AsTheApplicationsVariantChanges_AndAsItsKeepSystemThemeChanges()
    {
        var root = Probe();
        var looks = Under(ThemeVariant.Light, () =>
        {
            var window = Open(root, keep: true);
            var looks = new List<Look> { LookOf(window) };
            void Step(Action change)
            {
                change();
                Dispatcher.UIThread.RunJobs();
                looks.Add(LookOf(window));
            }
            Step(() => Application.Current!.RequestedThemeVariant = ThemeVariant.Dark);
            Step(() => window.KeepSystemTheme = false);
            Step(() => window.KeepSystemTheme = true);
            Step(() => Application.Current!.RequestedThemeVariant = ThemeVariant.Light);
            window.Close();
            return looks;
        });

        var (light, dark) = (Expected(root, ThemeVariant.Light), Expected(root, ThemeVariant.Dark));
        CollectionAssert.AreEqual(new[] { light, dark, light, dark, light }, looks);
    }

    // Under SimpleTheme's dark variant the bar is #282828 with white captions, beside a light form: the bar takes the form's Menu and
    // MenuText colours.
    [TestMethod]
    public void AFormsMenuBar_UnderADarkApplication_IsTheLightMenuColours_WhereTheFormDoesNotKeepTheSystemTheme()
    {
        var (bar, text) = Under(ThemeVariant.Dark, () =>
        {
            var window = new RDFormsFormWindow(new FormDefinition(Probe()));
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var colours = (Of(window.MenuBar.Background), Of(window.MenuBar.Items.OfType<MenuItem>().Single().Foreground));
            window.Close();
            return colours;
        });

        Assert.AreEqual((Color.Parse("#F0F0F0"), Color.Parse("#000000")), (bar, text));
    }

    [TestMethod]
    public void AFormMadeToKeepTheSystemTheme_UnderADarkApplication_IsDarkBeforeItIsShown()
    {
        var root = Probe();
        var look = Under(ThemeVariant.Dark, () =>
        {
            var window = new RDFormsFormWindow(new FormDefinition(root)) { KeepSystemTheme = true };
            Dispatcher.UIThread.RunJobs();
            var look = LookOf(window);
            window.Close();
            return look;
        });

        Assert.AreEqual(Expected(root, ThemeVariant.Dark), look);
    }

    // Each window has its own KeepSystemTheme, as two hosts in one process each give theirs: neither one's value, nor a change of it,
    // reaches the other.
    [TestMethod]
    public void TwoWindowsInOneProcess_EachKeepTheirOwnVariant()
    {
        var (first, second) = (Probe(), Probe());
        var looks = Under(ThemeVariant.Dark, () =>
        {
            var keeping = Open(first, keep: true);
            var light = Open(second, keep: false);
            var before = (LookOf(keeping), LookOf(light));
            keeping.KeepSystemTheme = false;
            light.KeepSystemTheme = true;
            Dispatcher.UIThread.RunJobs();
            var after = (LookOf(keeping), LookOf(light));
            keeping.Close();
            light.Close();
            return (before, after);
        });

        Assert.AreEqual((Expected(first, ThemeVariant.Dark), Expected(second, ThemeVariant.Light)), looks.before);
        Assert.AreEqual((Expected(first, ThemeVariant.Light), Expected(second, ThemeVariant.Dark)), looks.after);
    }

    [TestMethod]
    public void AColourSetAsRgb_IsDrawnAsItIs_UnderTheDarkVariant()
    {
        var root = Probe();
        root.Contents.Single(c => c.Name == "Text1").Set(P.BackColor, FormColor.FromRgb(0x12, 0x34, 0xAB));
        var back = Under(ThemeVariant.Dark, () =>
        {
            var window = Open(root, keep: true);
            var back = LookOf(window).TextBack;
            window.Close();
            return back;
        });

        Assert.AreEqual(Color.FromRgb(0x12, 0x34, 0xAB), back);
    }
}
