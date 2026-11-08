using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;

namespace RDCore.Forms.Tests.Headless;

[TestClass]
public sealed class HeadlessSessionTests
{
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
