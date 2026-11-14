using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// A ComboBox's SelStart, SelLength and SelText as a program reads and sets them. The expected rows are VB6's, native and p-code agreeing,
// for the TextBox steps and four of a combo's own, on a combo of each style: after each step, the error, the selection, the text, the
// Changes so far, what each Change's handler saw (`|Text@SelStart`) and each Click's (`#Text@SelStart+SelLength`), the ListIndex and the
// Clicks so far. Each step's statements run as under On Error Resume Next: one that fails does not stop the next, and the error is the
// last one, a handler's too. VB6 rounds 2.6 to 3 and -0.4 to 0 and takes the number 5 as "5" before the combo sees them: the steps here
// pass what the combo receives.
[TestClass]
public sealed class ComboSelectionTests
{
    private static readonly (string Label, Action<VBComboBox, ComponentInstance>[] Statements)[] Steps =
    [
        ("reset", [
            (b, c) =>
            {
                if (b.ListCount == 0)
                {
                    b.AddItem("Hello world");
                    b.AddItem("abc");
                    b.AddItem("Other");
                }
            },
            (b, c) =>
            {
                if (c.Get(P.ComboBoxStyle) == ComboBoxStyle.DropdownList)
                    b.ListIndex = 0;
                else
                    b.SetText("Hello world");
            }]),
        ("get", []),
        ("SelStart=3", [(b, c) => b.SelStart = 3]),
        ("SelLength=4", [(b, c) => b.SelLength = 4]),
        ("SelStart=5 after a selection", [(b, c) => b.SelStart = 5]),
        ("SelStart=100", [(b, c) => b.SelStart = 100]),
        ("SelStart=-1", [(b, c) => b.SelStart = -1]),
        ("SelStart=0 SelLength=100", [(b, c) => b.SelStart = 0, (b, c) => b.SelLength = 100]),
        ("SelLength=-1", [(b, c) => b.SelLength = -1]),
        ("SelStart=2 SelLength=3 SelText=XY", [(b, c) => b.SelStart = 2, (b, c) => b.SelLength = 3, (b, c) => b.SelText = "XY"]),
        ("SelText=empty with a selection", [(b, c) => b.SelStart = 0, (b, c) => b.SelLength = 2, (b, c) => b.SelText = ""]),
        ("SelText=same text", [(b, c) => b.SelStart = 0, (b, c) => b.SelLength = 3, (b, c) => b.SelText = b.Text[..3]]),
        ("SelText=long at end", [(b, c) => b.SelStart = b.Text.Length, (b, c) => b.SelText = "ABCDEFGHIJ"]),
        ("Text=abc after a selection", [(b, c) => b.SelStart = 1, (b, c) => b.SelLength = 2, (b, c) => b.SetText("abc")]),
        ("Text=same after a selection", [(b, c) => b.SelStart = 1, (b, c) => b.SelLength = 1, (b, c) => b.SetText(b.Text)]),
        ("SelText=CRLF", [(b, c) => b.SelStart = 1, (b, c) => b.SelLength = 0, (b, c) => b.SelText = "1\r\n2"]),
        ("SelStart=3 in CRLF", [(b, c) => b.SelStart = 3]),
        ("SelStart=2.6", [(b, c) => b.SelStart = 3]),
        ("SelStart=70000", [(b, c) => b.SelStart = 70000]),
        ("SelLength=70000", [(b, c) => b.SelStart = 0, (b, c) => b.SelLength = 70000]),
        ("SelText=5", [(b, c) => b.SelStart = 0, (b, c) => b.SelText = "5"]),
        ("SelStart=4 SelLength=100", [(b, c) => b.SelStart = 4, (b, c) => b.SelLength = 100]),
        ("SelStart=32767", [(b, c) => b.SelStart = 32767]),
        ("SelStart=32768", [(b, c) => b.SelStart = 32768]),
        ("SelStart=65535", [(b, c) => b.SelStart = 65535]),
        ("SelStart=65536", [(b, c) => b.SelStart = 65536]),
        ("SelLength=32767", [(b, c) => b.SelStart = 0, (b, c) => b.SelLength = 32767]),
        ("SelLength=32768", [(b, c) => b.SelStart = 0, (b, c) => b.SelLength = 32768]),
        ("SelLength=65535", [(b, c) => b.SelStart = 0, (b, c) => b.SelLength = 65535]),
        ("SelLength=65536", [(b, c) => b.SelStart = 0, (b, c) => b.SelLength = 65536]),
        ("SelStart=1 SelLength=-0.4", [(b, c) => b.SelStart = 1, (b, c) => b.SelLength = 0]),
        ("SelText=empty, nothing selected", [(b, c) => b.SelStart = 1, (b, c) => b.SelText = ""]),
        ("SelText=Z into a full box", [(b, c) => b.SetText("123456789012"), (b, c) => b.SelStart = 3, (b, c) => b.SelText = "Z"]),
        ("SelText=Z over a selection, full box", [(b, c) => b.SelStart = 3, (b, c) => b.SelLength = 2, (b, c) => b.SelText = "Z"]),
        ("SelText=same char over a selection", [(b, c) => b.SelStart = 0, (b, c) => b.SelLength = 1, (b, c) => b.SelText = b.Text[..1]]),
        ("ListIndex=1", [(b, c) => b.ListIndex = 1]),
        ("ListIndex=1 then SelStart=1 SelText=Q", [(b, c) => b.ListIndex = 1, (b, c) => b.SelStart = 1, (b, c) => b.SelText = "Q"]),
        ("ListIndex=2 then SelStart=1 SelLength=2", [(b, c) => b.ListIndex = 2, (b, c) => b.SelStart = 1, (b, c) => b.SelLength = 2]),
        ("Text=Other after a selection", [(b, c) => b.SelStart = 1, (b, c) => b.SelLength = 2, (b, c) => b.SetText("Other")]),
        ("ListIndex=1 then SelText=same char", [(b, c) => b.ListIndex = 1, (b, c) => b.SelStart = 0, (b, c) => b.SelLength = 1, (b, c) => b.SelText = "a"]),
        ("ListIndex=2 then SelText=empty, nothing selected", [(b, c) => b.ListIndex = 2, (b, c) => b.SelStart = 2, (b, c) => b.SelText = ""]),
        ("ListIndex=1 over the same text after a selection", [(b, c) => b.SetText("abc"), (b, c) => b.SelStart = 1, (b, c) => b.SelLength = 1, (b, c) => b.ListIndex = 1]),
    ];

    // Dropdown Combo and Simple Combo: VB6 gives the same rows for both.
    private static readonly string[] Editable =
    [
        "reset\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=|Hello world@0\tListIndex=-1\tClicks=0",
        "get\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=\tListIndex=-1\tClicks=0",
        "SelStart=3\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=\tListIndex=-1\tClicks=0",
        "SelLength=4\terr=0\tSelStart=3\tSelLength=4\tSelText=[lo w]\tText=[Hello world]\tChanges=0\tSeen=\tListIndex=-1\tClicks=0",
        "SelStart=5 after a selection\terr=0\tSelStart=5\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=\tListIndex=-1\tClicks=0",
        "SelStart=100\terr=0\tSelStart=11\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=\tListIndex=-1\tClicks=0",
        "SelStart=-1\terr=380\tSelStart=11\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=\tListIndex=-1\tClicks=0",
        "SelStart=0 SelLength=100\terr=0\tSelStart=0\tSelLength=11\tSelText=[Hello world]\tText=[Hello world]\tChanges=0\tSeen=\tListIndex=-1\tClicks=0",
        "SelLength=-1\terr=380\tSelStart=0\tSelLength=11\tSelText=[Hello world]\tText=[Hello world]\tChanges=0\tSeen=\tListIndex=-1\tClicks=0",
        "SelStart=2 SelLength=3 SelText=XY\terr=0\tSelStart=4\tSelLength=0\tSelText=[]\tText=[HeXY world]\tChanges=1\tSeen=|HeXY world@4\tListIndex=-1\tClicks=0",
        "SelText=empty with a selection\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[XY world]\tChanges=2\tSeen=|XY world@0\tListIndex=-1\tClicks=0",
        "SelText=same text\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[XY world]\tChanges=3\tSeen=|XY world@3\tListIndex=-1\tClicks=0",
        "SelText=long at end\terr=0\tSelStart=18\tSelLength=0\tSelText=[]\tText=[XY worldABCDEFGHIJ]\tChanges=4\tSeen=|XY worldABCDEFGHIJ@18\tListIndex=-1\tClicks=0",
        "Text=abc after a selection\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[abc]\tChanges=5\tSeen=|abc@0\tListIndex=-1\tClicks=0",
        "Text=same after a selection\terr=0\tSelStart=1\tSelLength=1\tSelText=[b]\tText=[abc]\tChanges=5\tSeen=\tListIndex=-1\tClicks=0",
        "SelText=CRLF\terr=0\tSelStart=5\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=6\tSeen=|a1\\r\\n2bc@5\tListIndex=-1\tClicks=0",
        "SelStart=3 in CRLF\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=6\tSeen=\tListIndex=-1\tClicks=0",
        "SelStart=2.6\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=6\tSeen=\tListIndex=-1\tClicks=0",
        "SelStart=70000\terr=380\tSelStart=3\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=6\tSeen=\tListIndex=-1\tClicks=0",
        "SelLength=70000\terr=380\tSelStart=0\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=6\tSeen=\tListIndex=-1\tClicks=0",
        "SelText=5\terr=0\tSelStart=1\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=|5a1\\r\\n2bc@1\tListIndex=-1\tClicks=0",
        "SelStart=4 SelLength=100\terr=0\tSelStart=4\tSelLength=4\tSelText=[\\n2bc]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=\tListIndex=-1\tClicks=0",
        "SelStart=32767\terr=0\tSelStart=8\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=\tListIndex=-1\tClicks=0",
        "SelStart=32768\terr=0\tSelStart=8\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=\tListIndex=-1\tClicks=0",
        "SelStart=65535\terr=0\tSelStart=8\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=\tListIndex=-1\tClicks=0",
        "SelStart=65536\terr=380\tSelStart=8\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=\tListIndex=-1\tClicks=0",
        "SelLength=32767\terr=0\tSelStart=0\tSelLength=8\tSelText=[5a1\\r\\n2bc]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=\tListIndex=-1\tClicks=0",
        "SelLength=32768\terr=0\tSelStart=0\tSelLength=8\tSelText=[5a1\\r\\n2bc]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=\tListIndex=-1\tClicks=0",
        "SelLength=65535\terr=0\tSelStart=0\tSelLength=8\tSelText=[5a1\\r\\n2bc]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=\tListIndex=-1\tClicks=0",
        "SelLength=65536\terr=380\tSelStart=0\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=\tListIndex=-1\tClicks=0",
        "SelStart=1 SelLength=-0.4\terr=0\tSelStart=1\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=\tListIndex=-1\tClicks=0",
        "SelText=empty, nothing selected\terr=0\tSelStart=1\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=8\tSeen=|5a1\\r\\n2bc@1\tListIndex=-1\tClicks=0",
        "SelText=Z into a full box\terr=0\tSelStart=4\tSelLength=0\tSelText=[]\tText=[123Z456789012]\tChanges=10\tSeen=|123456789012@0|123Z456789012@4\tListIndex=-1\tClicks=0",
        "SelText=Z over a selection, full box\terr=0\tSelStart=4\tSelLength=0\tSelText=[]\tText=[123Z56789012]\tChanges=11\tSeen=|123Z56789012@4\tListIndex=-1\tClicks=0",
        "SelText=same char over a selection\terr=0\tSelStart=1\tSelLength=0\tSelText=[]\tText=[123Z56789012]\tChanges=12\tSeen=|123Z56789012@1\tListIndex=-1\tClicks=0",
        "ListIndex=1\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[abc]\tChanges=12\tSeen=#abc@0+0\tListIndex=1\tClicks=1",
        "ListIndex=1 then SelStart=1 SelText=Q\terr=0\tSelStart=2\tSelLength=0\tSelText=[]\tText=[aQbc]\tChanges=13\tSeen=|aQbc@2\tListIndex=-1\tClicks=1",
        "ListIndex=2 then SelStart=1 SelLength=2\terr=0\tSelStart=1\tSelLength=2\tSelText=[th]\tText=[Other]\tChanges=13\tSeen=#Other@0+0\tListIndex=2\tClicks=2",
        "Text=Other after a selection\terr=0\tSelStart=1\tSelLength=2\tSelText=[th]\tText=[Other]\tChanges=13\tSeen=\tListIndex=2\tClicks=2",
        "ListIndex=1 then SelText=same char\terr=0\tSelStart=1\tSelLength=0\tSelText=[]\tText=[abc]\tChanges=14\tSeen=#abc@0+0|abc@1\tListIndex=-1\tClicks=3",
        "ListIndex=2 then SelText=empty, nothing selected\terr=0\tSelStart=2\tSelLength=0\tSelText=[]\tText=[Other]\tChanges=15\tSeen=#Other@0+0|Other@2\tListIndex=-1\tClicks=4",
        "ListIndex=1 over the same text after a selection\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[abc]\tChanges=16\tSeen=|abc@0#abc@0+0\tListIndex=1\tClicks=5",
    ];

    // Dropdown List: no edit box, and 380 for every read and set of the selection, its Click handler's reads too.
    private static readonly string[] NoEditBox =
    [
        "reset\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[Hello world]\tChanges=0\tSeen=\tListIndex=0\tClicks=0",
        "get\terr=0\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[Hello world]\tChanges=0\tSeen=\tListIndex=0\tClicks=0",
        "SelStart=3\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[Hello world]\tChanges=0\tSeen=\tListIndex=0\tClicks=0",
        "SelLength=4\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[Hello world]\tChanges=0\tSeen=\tListIndex=0\tClicks=0",
        "SelStart=5 after a selection\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[Hello world]\tChanges=0\tSeen=\tListIndex=0\tClicks=0",
        "SelStart=100\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[Hello world]\tChanges=0\tSeen=\tListIndex=0\tClicks=0",
        "SelStart=-1\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[Hello world]\tChanges=0\tSeen=\tListIndex=0\tClicks=0",
        "SelStart=0 SelLength=100\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[Hello world]\tChanges=0\tSeen=\tListIndex=0\tClicks=0",
        "SelLength=-1\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[Hello world]\tChanges=0\tSeen=\tListIndex=0\tClicks=0",
        "SelStart=2 SelLength=3 SelText=XY\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[Hello world]\tChanges=0\tSeen=\tListIndex=0\tClicks=0",
        "SelText=empty with a selection\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[Hello world]\tChanges=0\tSeen=\tListIndex=0\tClicks=0",
        "SelText=same text\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[Hello world]\tChanges=0\tSeen=\tListIndex=0\tClicks=0",
        "SelText=long at end\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[Hello world]\tChanges=0\tSeen=\tListIndex=0\tClicks=0",
        "Text=abc after a selection\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=1",
        "Text=same after a selection\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "SelText=CRLF\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "SelStart=3 in CRLF\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "SelStart=2.6\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "SelStart=70000\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "SelLength=70000\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "SelText=5\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "SelStart=4 SelLength=100\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "SelStart=32767\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "SelStart=32768\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "SelStart=65535\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "SelStart=65536\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "SelLength=32767\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "SelLength=32768\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "SelLength=65535\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "SelLength=65536\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "SelStart=1 SelLength=-0.4\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "SelText=empty, nothing selected\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "SelText=Z into a full box\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "SelText=Z over a selection, full box\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "SelText=same char over a selection\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "ListIndex=1\terr=0\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "ListIndex=1 then SelStart=1 SelText=Q\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=2",
        "ListIndex=2 then SelStart=1 SelLength=2\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[Other]\tChanges=0\tSeen=\tListIndex=2\tClicks=3",
        "Text=Other after a selection\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[Other]\tChanges=0\tSeen=\tListIndex=2\tClicks=4",
        "ListIndex=1 then SelText=same char\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=5",
        "ListIndex=2 then SelText=empty, nothing selected\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[Other]\tChanges=0\tSeen=\tListIndex=2\tClicks=6",
        "ListIndex=1 over the same text after a selection\terr=380\tSelStart=err380\tSelLength=err380\tSelText=[err380]\tText=[abc]\tChanges=0\tSeen=\tListIndex=1\tClicks=7",
    ];

    private static string Esc(string text) => text.Replace("\r", "\\r").Replace("\n", "\\n");

    private static ComponentInstance Combo(string name, ComboBoxStyle style)
    {
        var instance = new ComponentInstance(VBClasses.ComboBox, name);
        Assert.IsTrue(instance.TrySet(P.ComboBoxStyle, style));
        return instance;
    }

    // A read as the probe makes it: the value, or "err" and the error's number.
    private static string Read<T>(Func<T> read)
    {
        try
        {
            return $"{read()}";
        }
        catch (FormErrorException exception)
        {
            return $"err{exception.Error.Number}";
        }
    }

    private static string ErrorOf(Exception exception) =>
        exception is FormErrorException error ? error.Error.Number.ToString() : exception.GetType().Name;

    // The rows the steps give on the combo, in a window with a sink: shown, or loaded and not shown.
    private static List<string> Script(ComponentInstance instance, bool shown) => UI.Run(() =>
    {
        var combo = new VBComboBox(instance) { Width = 120 };
        var (changes, clicks, seen, error) = (0, 0, "", "0");
        var sink = new RecordingSink();
        var window = new Window { Width = 400, Height = 300, Content = new Canvas { Children = { combo } } };
        VBControl.SetEventSink(window, sink);
        if (shown)
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
        }
        sink.Handler = (source, definition, _) =>
            {
                if (!ReferenceEquals(source, instance))
                    return;
                if (ReferenceEquals(definition, VBEvents.Change))
                {
                    changes++;
                    seen += $"|{combo.Text}@{combo.SelStart}";
                }
                else if (ReferenceEquals(definition, VBEvents.Click))
                {
                    clicks++;
                    try
                    {
                        seen += $"#{combo.Text}@{combo.SelStart}+{combo.SelLength}";
                    }
                    catch (Exception exception)
                    {
                        error = ErrorOf(exception);
                    }
                }
            };
        var rows = new List<string>();
        foreach (var (label, statements) in Steps)
        {
            error = "0";
            foreach (var statement in statements)
            {
                try
                {
                    statement(combo, instance);
                }
                catch (Exception exception)
                {
                    error = ErrorOf(exception);
                }
            }
            if (label == "reset")
                (changes, clicks) = (0, 0);
            rows.Add($"{label}\terr={error}\tSelStart={Read(() => combo.SelStart)}\tSelLength={Read(() => combo.SelLength)}" +
                $"\tSelText=[{Esc(Read(() => combo.SelText))}]\tText=[{Esc(combo.Text)}]\tChanges={changes}\tSeen={Esc(seen)}" +
                $"\tListIndex={combo.ListIndex}\tClicks={clicks}");
            seen = "";
        }
        VBControl.SetEventSink(window, null);
        window.Close();
        return rows;
    });

    [TestMethod]
    public void ADropdownCombo_SelectsAndReplaces_AsASingleLineTextBox() =>
        Expect.Lines(Editable, Script(Combo("Combo1", ComboBoxStyle.DropdownCombo), shown: true));

    [TestMethod]
    public void ASimpleCombo_SelectsAndReplaces_AsASingleLineTextBox() =>
        Expect.Lines(Editable, Script(Combo("Combo2", ComboBoxStyle.SimpleCombo), shown: true));

    [TestMethod]
    public void ADropdownList_HasNoSelection() => Expect.Lines(NoEditBox, Script(Combo("Combo3", ComboBoxStyle.DropdownList), shown: true));

    [TestMethod]
    public void AComboOfAFormLoadedAndNotShown_IsSetAsAShownOne()
    {
        Expect.Lines(Editable, Script(Combo("Combo1", ComboBoxStyle.DropdownCombo), shown: false));
        Expect.Lines(NoEditBox, Script(Combo("Combo3", ComboBoxStyle.DropdownList), shown: false));
    }

    // A selection set before the combo has its edit box shows in the box once it does; one the user makes in the box, from its anchor
    // back to its caret, is the program's; one set after shows at once.
    [TestMethod]
    public void TheEditBox_ShowsAProgramsSelection_AndGivesBackTheUsers()
    {
        var (shown, user, set) = UI.Run(() =>
        {
            var combo = new VBComboBox(Combo("Combo1", ComboBoxStyle.DropdownCombo)) { Width = 120 };
            var window = new Window { Width = 400, Height = 300, Content = new Canvas { Children = { combo } } };
            combo.SetText("Hello world");
            combo.SelStart = 2;
            combo.SelLength = 3;
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var edit = combo.GetVisualDescendants().OfType<TextBox>().Single();
            var shown = (edit.SelectionStart, edit.SelectionEnd);
            edit.SelectionStart = 7;
            edit.SelectionEnd = 3;
            var user = (combo.SelStart, combo.SelLength);
            combo.SelStart = 4;
            combo.SelLength = 5;
            var set = (edit.SelectionStart, edit.SelectionEnd);
            window.Close();
            return (shown, user, set);
        });
        Assert.AreEqual((2, 5), shown);
        Assert.AreEqual((3, 4), user);
        Assert.AreEqual((4, 9), set);
    }

    // A change to the edit box's text leaves the component's selection where the box's caret is, even where the caret does not move.
    [TestMethod]
    public void TheUsersText_KeepsTheEditBoxsCaret()
    {
        var caret = UI.Run(() =>
        {
            var instance = Combo("Combo1", ComboBoxStyle.DropdownCombo);
            var combo = new VBComboBox(instance) { Width = 120 };
            var window = new Window { Width = 400, Height = 300, Content = new Canvas { Children = { combo } } };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var edit = combo.GetVisualDescendants().OfType<TextBox>().Single();
            edit.Text = "Hello world";
            edit.CaretIndex = 5;
            edit.Text = "Hello";
            var caret = (instance.Get(P.SelStart), edit.CaretIndex);
            window.Close();
            return caret;
        });
        Assert.AreEqual((5, 5), caret);
    }

    [TestMethod]
    public void SelText_IsNotNull() =>
        Assert.ThrowsExactly<ArgumentNullException>(() => UI.Run(() => new VBComboBox(Combo("Combo1", ComboBoxStyle.DropdownCombo)).SelText = null!));
}
