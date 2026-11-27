using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// A Data control's pixels against VB6's, from the measured fixture DataLook.txt. The caption's text is drawn in another font than VB6's,
// so the caption's strip is compared only in its last three columns, where no text reaches.
[TestClass]
public sealed class DataControlTests
{
    private const int FormWidth = 360, FormHeight = 213;

    // Each measured case: its place and size in pixels, and its properties but its place and size.
    private static readonly Dictionary<string, (int Left, int Top, int Width, int Height, Action<ComponentInstance> Set)> Cases = new()
    {
        ["D1"] = (8, 8, 76, 23, data => data.Set(P.Caption, "Data1")),
        ["D2"] = (8, 40, 160, 23, data => data.Set(P.Caption, "Data1")),
        ["D3"] = (8, 72, 160, 40, data => data.Set(P.Caption, "Data2")),
        ["D4"] = (8, 120, 60, 23, data => data.Set(P.Caption, "Long caption text")),
        ["D5"] = (184, 8, 160, 23, data => data.Set(P.Caption, "Off").Set(P.Enabled, false)),
        ["D6"] = (184, 40, 160, 23, data => data.Set(P.Caption, "Flat").Set(P.Appearance, Appearance.Flat)),
        ["D7"] = (184, 72, 160, 23, data => data.Set(P.Caption, "Colour")
            .Set(P.BackColor, FormColor.FromRgb(255, 0, 0)).Set(P.ForeColor, FormColor.FromRgb(0, 0, 255))),
        ["D8"] = (184, 104, 40, 23, data => data.Set(P.Caption, "X")),
        ["D9"] = (184, 136, 160, 30, data => data.Set(P.Caption, "Bold").Set(P.Font, new FormFont("Arial", 12, FormFont.BoldWeight))),
        // 13 pixels tall, drawn 23 tall: the fixture holds its first 15 rows, which are those of a control 23 pixels tall.
        ["D10"] = (184, 176, 160, 13, data => data.Set(P.Caption, "Short")),
    };

    // The height VB6 draws a Data control at: its own, 23 pixels at least.
    private static int Drawn(int height) => Math.Max(height, 23);

    private static ComponentInstance Data(ComponentInstance root, string name)
    {
        var (left, top, width, height, set) = Cases[name];
        var data = new ComponentInstance(ComponentClasses.Data, name).Set(P.Left, (double)left).Set(P.Top, (double)top)
            .Set(P.Width, (double)width).Set(P.Height, (double)height);
        set(data);
        data.SetContainer(root);
        return data;
    }

    // The measured grids by case, with 2 pixels around each, and the colours of their letters.
    private static (Dictionary<string, string[]> Grids, Dictionary<char, Color> Key) Measured()
    {
        var lines = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Controls", "Fixtures", "DataLook.txt"));
        var grids = new Dictionary<string, string[]>();
        var key = new Dictionary<char, Color>();
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("case ", StringComparison.Ordinal))
            {
                var name = lines[i].Split(' ')[1];
                var height = Cases.TryGetValue(name, out var c) ? c.Height : 0;
                grids[name] = height > 0 ? lines[(i + 1)..(i + 1 + height + 4)] : [];
            }
            else if (lines[i].StartsWith("key: ", StringComparison.Ordinal))
            {
                foreach (var entry in lines[i][5..].Split(' '))
                {
                    var rgb = uint.Parse(entry[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    key[entry[0]] = Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
                }
            }
        }
        return (grids, key);
    }

    // Every pixel of the form's frame.
    private static Color[,] Frame(RDFormsFormWindow window)
    {
        using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("no frame was rendered");
        using var buffer = frame.Lock();
        var pixels = new Color[buffer.Size.Width, buffer.Size.Height];
        for (var y = 0; y < buffer.Size.Height; y++)
        {
            for (var x = 0; x < buffer.Size.Width; x++)
            {
                var value = Marshal.ReadInt32(buffer.Address, y * buffer.RowBytes + x * 4);
                var (b, g, r) = ((byte)value, (byte)(value >> 8), (byte)(value >> 16));
                pixels[x, y] = buffer.Format == PixelFormats.Rgba8888 ? Color.FromRgb(b, g, r) : Color.FromRgb(r, g, b);
            }
        }
        return pixels;
    }

    private static Color[,] Render(params string[] names)
    {
        var root = new ComponentInstance(ComponentClasses.Form, "Form1").Set(P.Width, (double)FormWidth).Set(P.Height, (double)FormHeight);
        foreach (var name in names)
            Data(root, name);
        var window = new RDFormsFormWindow(new FormDefinition(root));
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        var pixels = Frame(window);
        window.Close();
        return pixels;
    }

    // Whether a pixel of a case, from its control's top-left, is in its caption's text: the strip but its last three columns.
    private static bool InCaptionText(string name, int x, int y)
    {
        var (_, _, width, height, _) = Cases[name];
        var flat = name == "D6";
        var (inset, right, bottom) = flat ? (1, width, Drawn(height) - 1) : (3, width - 3, Drawn(height) - 3);
        return y >= inset && y < bottom && x >= inset + 38 && x < right - 39 - 3;
    }

    [TestMethod]
    [DataRow("D1")]
    [DataRow("D2")]
    [DataRow("D3")]
    [DataRow("D4")]
    [DataRow("D5")]
    [DataRow("D6")]
    [DataRow("D7")]
    [DataRow("D8")]
    [DataRow("D9")]
    [DataRow("D10", DisplayName = "D10, less than 23 pixels tall")]
    public void ADataControl_IsDrawnAsVB6DrawsIt_ButItsCaptionsText(string name)
    {
        var (grids, key) = Measured();
        var pixels = UI.Run(() => Render(name));
        var (left, top, _, _, _) = Cases[name];

        var differences = new List<string>();
        var grid = grids[name];
        for (var row = 0; row < grid.Length; row++)
        {
            for (var column = 0; column < grid[row].Length; column++)
            {
                var (x, y) = (column - 2, row - 2);
                if (InCaptionText(name, x, y))
                    continue;
                var expected = key[grid[row][column]];
                var actual = pixels[left + x, top + y];
                if (expected != actual)
                    differences.Add($"({x},{y}) {actual} for {expected}");
            }
        }

        Assert.IsEmpty(differences, string.Join("; ", differences.Take(12)));
    }

    [TestMethod]
    public void ADisabledDataControlsCaption_IsInTheGreyTextColour_AndAnEnabledOnesInItsForeColor()
    {
        var pixels = UI.Run(() => Render("D5", "D7"));

        // The text's pixels, from 3 pixels into each strip on the parts' rows. The text is smoothed, so a pixel can be a blend of the
        // ink and the strip's colour.
        IEnumerable<Color> Text(string name)
        {
            var (left, top, _, height, _) = Cases[name];
            for (var y = 3; y < height - 3; y++)
                for (var x = 44; x < 80; x++)
                    yield return pixels[left + x, top + y];
        }

        // The smoothing is per subpixel: on white, grey ink leaves no channel darker than the ink's; blue ink on red, some pixel bluer
        // than red.
        var disabled = Text("D5").ToArray();
        Assert.IsTrue(disabled.All(c => Math.Min(c.R, Math.Min(c.G, c.B)) >= 0x6D));
        Assert.IsTrue(disabled.Any(c => c.R < 0xC0 && c.G < 0xC0 && c.B < 0xC0));
        Assert.IsTrue(Text("D7").Any(c => c.B > c.R));
    }

    [TestMethod]
    public void ItsUpdateRecordAndUpdateControls_AreError91_AsItOpensNoRecordset()
    {
        var numbers = UI.Run(() =>
        {
            var root = new ComponentInstance(ComponentClasses.Form, "Form1").Set(P.Width, (double)FormWidth).Set(P.Height, (double)FormHeight);
            var data = Data(root, "D2");
            var window = new RDFormsFormWindow(new FormDefinition(root));
            window.Show();
            Dispatcher.UIThread.RunJobs();
            int Number(MethodDefinition method)
            {
                try
                {
                    window.Invoke(data, method, []);
                    return 0;
                }
                catch (FormErrorException e)
                {
                    return e.Error.Number;
                }
            }
            var result = (Number(Methods.UpdateRecord), Number(Methods.UpdateControls), Number(Methods.Refresh));
            window.Close();
            return result;
        });

        Assert.AreEqual((91, 91, 0), numbers);
    }

    [TestMethod]
    public void ADataControlDrawnTallerThanItsHeight_ReadsBackItsHeightAsSet()
    {
        var height = UI.Run(() =>
        {
            var root = new ComponentInstance(ComponentClasses.Form, "Form1").Set(P.Width, (double)FormWidth).Set(P.Height, (double)FormHeight);
            var data = Data(root, "D10");
            var window = new RDFormsFormWindow(new FormDefinition(root));
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var read = data.Get(P.Height);
            window.Close();
            return read;
        });

        Assert.AreEqual(13d, height);
    }

    [TestMethod]
    public void ItHasNoDatabaseNoRecordset_AndAnEditModeOf0() =>
        UI.Run(() =>
        {
            var data = new RDFormsData(new ComponentInstance(ComponentClasses.Data, "Data1"));
            Assert.IsNull(data.Database);
            Assert.IsNull(data.Recordset);
            Assert.AreEqual((short)0, data.EditMode);
        });
}
