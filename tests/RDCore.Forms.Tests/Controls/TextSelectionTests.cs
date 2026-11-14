using Avalonia.Controls;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// A TextBox's SelStart, SelLength and SelText as a program reads and sets them. The expected rows are VB6's, native and p-code agreeing,
// for the same steps on a box of each kind: after each step, the error, the selection, the text, the Changes so far, and the text and
// SelStart each Change's handler saw. VB6 rounds 2.6 to 3 and -0.4 to 0 and takes the number 5 as "5" before the box sees them: the
// steps here pass what the box receives.
[TestClass]
public sealed class TextSelectionTests
{
    private static readonly (string Label, Action<VBTextBox, ComponentInstance> Run)[] Steps =
    [
        ("reset", (b, c) => c.Set(P.Text, "Hello world")),
        ("get", (b, c) => { }),
        ("SelStart=3", (b, c) => b.SelStart = 3),
        ("SelLength=4", (b, c) => b.SelLength = 4),
        ("SelStart=5 after a selection", (b, c) => b.SelStart = 5),
        ("SelStart=100", (b, c) => b.SelStart = 100),
        ("SelStart=-1", (b, c) => b.SelStart = -1),
        ("SelStart=0 SelLength=100", (b, c) => { b.SelStart = 0; b.SelLength = 100; }),
        ("SelLength=-1", (b, c) => b.SelLength = -1),
        ("SelStart=2 SelLength=3 SelText=XY", (b, c) => { b.SelStart = 2; b.SelLength = 3; b.SelText = "XY"; }),
        ("SelText=empty with a selection", (b, c) => { b.SelStart = 0; b.SelLength = 2; b.SelText = ""; }),
        ("SelText=same text", (b, c) => { b.SelStart = 0; b.SelLength = 3; b.SelText = c.Get(P.Text)[..3]; }),
        ("SelText=long at end", (b, c) => { b.SelStart = c.Get(P.Text).Length; b.SelText = "ABCDEFGHIJ"; }),
        ("Text=abc after a selection", (b, c) => { b.SelStart = 1; b.SelLength = 2; c.Set(P.Text, "abc"); }),
        ("Text=same after a selection", (b, c) => { b.SelStart = 1; b.SelLength = 1; c.Set(P.Text, c.Get(P.Text)); }),
        ("SelText=CRLF", (b, c) => { b.SelStart = 1; b.SelLength = 0; b.SelText = "1\r\n2"; }),
        ("SelStart=3 in CRLF", (b, c) => b.SelStart = 3),
        ("SelStart=2.6", (b, c) => b.SelStart = 3),
        ("SelStart=70000", (b, c) => b.SelStart = 70000),
        ("SelLength=70000", (b, c) => { b.SelStart = 0; b.SelLength = 70000; }),
        ("SelText=5", (b, c) => { b.SelStart = 0; b.SelText = "5"; }),
        ("SelStart=4 SelLength=100", (b, c) => { b.SelStart = 4; b.SelLength = 100; }),
        ("SelStart=32767", (b, c) => b.SelStart = 32767),
        ("SelStart=32768", (b, c) => b.SelStart = 32768),
        ("SelStart=65535", (b, c) => b.SelStart = 65535),
        ("SelStart=65536", (b, c) => b.SelStart = 65536),
        ("SelLength=32767", (b, c) => { b.SelStart = 0; b.SelLength = 32767; }),
        ("SelLength=32768", (b, c) => { b.SelStart = 0; b.SelLength = 32768; }),
        ("SelLength=65535", (b, c) => { b.SelStart = 0; b.SelLength = 65535; }),
        ("SelLength=65536", (b, c) => { b.SelStart = 0; b.SelLength = 65536; }),
        ("SelStart=1 SelLength=-0.4", (b, c) => { b.SelStart = 1; b.SelLength = 0; }),
        ("SelText=empty, nothing selected", (b, c) => { b.SelStart = 1; b.SelText = ""; }),
        ("SelText=Z into a full box", (b, c) => { c.Set(P.Text, "123456789012"); b.SelStart = 3; b.SelText = "Z"; }),
        ("SelText=Z over a selection, full box", (b, c) => { b.SelStart = 3; b.SelLength = 2; b.SelText = "Z"; }),
        ("SelText=same char over a selection", (b, c) => { b.SelStart = 0; b.SelLength = 1; b.SelText = c.Get(P.Text)[..1]; }),
    ];

    // Single-line.
    private static readonly string[] Text1 =
    [
        "reset\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=|Hello world@0",
        "get\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=3\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelLength=4\terr=0\tSelStart=3\tSelLength=4\tSelText=[lo w]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=5 after a selection\terr=0\tSelStart=5\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=100\terr=0\tSelStart=11\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=-1\terr=380\tSelStart=11\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=0 SelLength=100\terr=0\tSelStart=0\tSelLength=11\tSelText=[Hello world]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelLength=-1\terr=380\tSelStart=0\tSelLength=11\tSelText=[Hello world]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=2 SelLength=3 SelText=XY\terr=0\tSelStart=4\tSelLength=0\tSelText=[]\tText=[HeXY world]\tChanges=1\tSeen=|HeXY world@4",
        "SelText=empty with a selection\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[XY world]\tChanges=2\tSeen=|XY world@0",
        "SelText=same text\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[XY world]\tChanges=3\tSeen=|XY world@3",
        "SelText=long at end\terr=0\tSelStart=18\tSelLength=0\tSelText=[]\tText=[XY worldABCDEFGHIJ]\tChanges=4\tSeen=|XY worldABCDEFGHIJ@18",
        "Text=abc after a selection\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[abc]\tChanges=5\tSeen=|abc@0",
        "Text=same after a selection\terr=0\tSelStart=1\tSelLength=1\tSelText=[b]\tText=[abc]\tChanges=5\tSeen=",
        "SelText=CRLF\terr=0\tSelStart=5\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=6\tSeen=|a1\\r\\n2bc@5",
        "SelStart=3 in CRLF\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=6\tSeen=",
        "SelStart=2.6\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=6\tSeen=",
        "SelStart=70000\terr=380\tSelStart=3\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=6\tSeen=",
        "SelLength=70000\terr=380\tSelStart=0\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=6\tSeen=",
        "SelText=5\terr=0\tSelStart=1\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=|5a1\\r\\n2bc@1",
        "SelStart=4 SelLength=100\terr=0\tSelStart=4\tSelLength=4\tSelText=[\\n2bc]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=",
        "SelStart=32767\terr=0\tSelStart=8\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=",
        "SelStart=32768\terr=0\tSelStart=8\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=",
        "SelStart=65535\terr=0\tSelStart=8\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=",
        "SelStart=65536\terr=380\tSelStart=8\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=",
        "SelLength=32767\terr=0\tSelStart=0\tSelLength=8\tSelText=[5a1\\r\\n2bc]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=",
        "SelLength=32768\terr=0\tSelStart=0\tSelLength=8\tSelText=[5a1\\r\\n2bc]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=",
        "SelLength=65535\terr=0\tSelStart=0\tSelLength=8\tSelText=[5a1\\r\\n2bc]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=",
        "SelLength=65536\terr=380\tSelStart=0\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=",
        "SelStart=1 SelLength=-0.4\terr=0\tSelStart=1\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=",
        "SelText=empty, nothing selected\terr=0\tSelStart=1\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=8\tSeen=|5a1\\r\\n2bc@1",
        "SelText=Z into a full box\terr=0\tSelStart=4\tSelLength=0\tSelText=[]\tText=[123Z456789012]\tChanges=10\tSeen=|123456789012@0|123Z456789012@4",
        "SelText=Z over a selection, full box\terr=0\tSelStart=4\tSelLength=0\tSelText=[]\tText=[123Z56789012]\tChanges=11\tSeen=|123Z56789012@4",
        "SelText=same char over a selection\terr=0\tSelStart=1\tSelLength=0\tSelText=[]\tText=[123Z56789012]\tChanges=12\tSeen=|123Z56789012@1",
    ];

    // MultiLine.
    private static readonly string[] Text2 =
    [
        "reset\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=|Hello world@0",
        "get\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=3\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelLength=4\terr=0\tSelStart=3\tSelLength=4\tSelText=[lo w]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=5 after a selection\terr=0\tSelStart=5\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=100\terr=0\tSelStart=11\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=-1\terr=380\tSelStart=11\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=0 SelLength=100\terr=0\tSelStart=0\tSelLength=11\tSelText=[Hello world]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelLength=-1\terr=380\tSelStart=0\tSelLength=11\tSelText=[Hello world]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=2 SelLength=3 SelText=XY\terr=0\tSelStart=4\tSelLength=0\tSelText=[]\tText=[HeXY world]\tChanges=2\tSeen=|He world@2|HeXY world@4",
        "SelText=empty with a selection\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[XY world]\tChanges=3\tSeen=|XY world@0",
        "SelText=same text\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[XY world]\tChanges=5\tSeen=|world@0|XY world@3",
        "SelText=long at end\terr=0\tSelStart=18\tSelLength=0\tSelText=[]\tText=[XY worldABCDEFGHIJ]\tChanges=6\tSeen=|XY worldABCDEFGHIJ@18",
        "Text=abc after a selection\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[abc]\tChanges=7\tSeen=|abc@0",
        "Text=same after a selection\terr=0\tSelStart=1\tSelLength=1\tSelText=[b]\tText=[abc]\tChanges=7\tSeen=",
        "SelText=CRLF\terr=0\tSelStart=5\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=8\tSeen=|a1\\r\\n2bc@5",
        "SelStart=3 in CRLF\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=8\tSeen=",
        "SelStart=2.6\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=8\tSeen=",
        "SelStart=70000\terr=380\tSelStart=3\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=8\tSeen=",
        "SelLength=70000\terr=380\tSelStart=0\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=8\tSeen=",
        "SelText=5\terr=0\tSelStart=1\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=|5a1\\r\\n2bc@1",
        "SelStart=4 SelLength=100\terr=0\tSelStart=4\tSelLength=4\tSelText=[\\n2bc]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelStart=32767\terr=0\tSelStart=8\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelStart=32768\terr=0\tSelStart=8\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelStart=65535\terr=0\tSelStart=8\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelStart=65536\terr=380\tSelStart=8\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelLength=32767\terr=0\tSelStart=0\tSelLength=8\tSelText=[5a1\\r\\n2bc]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelLength=32768\terr=0\tSelStart=0\tSelLength=8\tSelText=[5a1\\r\\n2bc]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelLength=65535\terr=0\tSelStart=0\tSelLength=8\tSelText=[5a1\\r\\n2bc]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelLength=65536\terr=380\tSelStart=0\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelStart=1 SelLength=-0.4\terr=0\tSelStart=1\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelText=empty, nothing selected\terr=0\tSelStart=1\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelText=Z into a full box\terr=0\tSelStart=4\tSelLength=0\tSelText=[]\tText=[123Z456789012]\tChanges=11\tSeen=|123456789012@0|123Z456789012@4",
        "SelText=Z over a selection, full box\terr=0\tSelStart=4\tSelLength=0\tSelText=[]\tText=[123Z56789012]\tChanges=13\tSeen=|12356789012@3|123Z56789012@4",
        "SelText=same char over a selection\terr=0\tSelStart=1\tSelLength=0\tSelText=[]\tText=[123Z56789012]\tChanges=15\tSeen=|23Z56789012@0|123Z56789012@1",
    ];

    // Single-line, MaxLength 12.
    private static readonly string[] Text3 =
    [
        "reset\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=|Hello world@0",
        "get\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=3\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelLength=4\terr=0\tSelStart=3\tSelLength=4\tSelText=[lo w]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=5 after a selection\terr=0\tSelStart=5\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=100\terr=0\tSelStart=11\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=-1\terr=380\tSelStart=11\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=0 SelLength=100\terr=0\tSelStart=0\tSelLength=11\tSelText=[Hello world]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelLength=-1\terr=380\tSelStart=0\tSelLength=11\tSelText=[Hello world]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=2 SelLength=3 SelText=XY\terr=0\tSelStart=4\tSelLength=0\tSelText=[]\tText=[HeXY world]\tChanges=1\tSeen=|HeXY world@4",
        "SelText=empty with a selection\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[XY world]\tChanges=2\tSeen=|XY world@0",
        "SelText=same text\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[XY world]\tChanges=3\tSeen=|XY world@3",
        "SelText=long at end\terr=0\tSelStart=12\tSelLength=0\tSelText=[]\tText=[XY worldABCD]\tChanges=4\tSeen=|XY worldABCD@12",
        "Text=abc after a selection\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[abc]\tChanges=5\tSeen=|abc@0",
        "Text=same after a selection\terr=0\tSelStart=1\tSelLength=1\tSelText=[b]\tText=[abc]\tChanges=5\tSeen=",
        "SelText=CRLF\terr=0\tSelStart=5\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=6\tSeen=|a1\\r\\n2bc@5",
        "SelStart=3 in CRLF\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=6\tSeen=",
        "SelStart=2.6\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=6\tSeen=",
        "SelStart=70000\terr=380\tSelStart=3\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=6\tSeen=",
        "SelLength=70000\terr=380\tSelStart=0\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=6\tSeen=",
        "SelText=5\terr=0\tSelStart=1\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=|5a1\\r\\n2bc@1",
        "SelStart=4 SelLength=100\terr=0\tSelStart=4\tSelLength=4\tSelText=[\\n2bc]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=",
        "SelStart=32767\terr=0\tSelStart=8\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=",
        "SelStart=32768\terr=0\tSelStart=8\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=",
        "SelStart=65535\terr=0\tSelStart=8\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=",
        "SelStart=65536\terr=380\tSelStart=8\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=",
        "SelLength=32767\terr=0\tSelStart=0\tSelLength=8\tSelText=[5a1\\r\\n2bc]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=",
        "SelLength=32768\terr=0\tSelStart=0\tSelLength=8\tSelText=[5a1\\r\\n2bc]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=",
        "SelLength=65535\terr=0\tSelStart=0\tSelLength=8\tSelText=[5a1\\r\\n2bc]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=",
        "SelLength=65536\terr=380\tSelStart=0\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=",
        "SelStart=1 SelLength=-0.4\terr=0\tSelStart=1\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=7\tSeen=",
        "SelText=empty, nothing selected\terr=0\tSelStart=1\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=8\tSeen=|5a1\\r\\n2bc@1",
        "SelText=Z into a full box\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[123456789012]\tChanges=9\tSeen=|123456789012@0",
        "SelText=Z over a selection, full box\terr=0\tSelStart=4\tSelLength=0\tSelText=[]\tText=[123Z6789012]\tChanges=10\tSeen=|123Z6789012@4",
        "SelText=same char over a selection\terr=0\tSelStart=1\tSelLength=0\tSelText=[]\tText=[123Z6789012]\tChanges=11\tSeen=|123Z6789012@1",
    ];

    // MultiLine, MaxLength 12.
    private static readonly string[] Text6 =
    [
        "reset\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=|Hello world@0",
        "get\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=3\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelLength=4\terr=0\tSelStart=3\tSelLength=4\tSelText=[lo w]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=5 after a selection\terr=0\tSelStart=5\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=100\terr=0\tSelStart=11\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=-1\terr=380\tSelStart=11\tSelLength=0\tSelText=[]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=0 SelLength=100\terr=0\tSelStart=0\tSelLength=11\tSelText=[Hello world]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelLength=-1\terr=380\tSelStart=0\tSelLength=11\tSelText=[Hello world]\tText=[Hello world]\tChanges=0\tSeen=",
        "SelStart=2 SelLength=3 SelText=XY\terr=0\tSelStart=4\tSelLength=0\tSelText=[]\tText=[HeXY world]\tChanges=2\tSeen=|He world@2|HeXY world@4",
        "SelText=empty with a selection\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[XY world]\tChanges=3\tSeen=|XY world@0",
        "SelText=same text\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[XY world]\tChanges=5\tSeen=|world@0|XY world@3",
        "SelText=long at end\terr=0\tSelStart=12\tSelLength=0\tSelText=[]\tText=[XY worldABCD]\tChanges=6\tSeen=|XY worldABCD@12",
        "Text=abc after a selection\terr=0\tSelStart=0\tSelLength=0\tSelText=[]\tText=[abc]\tChanges=7\tSeen=|abc@0",
        "Text=same after a selection\terr=0\tSelStart=1\tSelLength=1\tSelText=[b]\tText=[abc]\tChanges=7\tSeen=",
        "SelText=CRLF\terr=0\tSelStart=5\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=8\tSeen=|a1\\r\\n2bc@5",
        "SelStart=3 in CRLF\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=8\tSeen=",
        "SelStart=2.6\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=8\tSeen=",
        "SelStart=70000\terr=380\tSelStart=3\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=8\tSeen=",
        "SelLength=70000\terr=380\tSelStart=0\tSelLength=0\tSelText=[]\tText=[a1\\r\\n2bc]\tChanges=8\tSeen=",
        "SelText=5\terr=0\tSelStart=1\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=|5a1\\r\\n2bc@1",
        "SelStart=4 SelLength=100\terr=0\tSelStart=4\tSelLength=4\tSelText=[\\n2bc]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelStart=32767\terr=0\tSelStart=8\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelStart=32768\terr=0\tSelStart=8\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelStart=65535\terr=0\tSelStart=8\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelStart=65536\terr=380\tSelStart=8\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelLength=32767\terr=0\tSelStart=0\tSelLength=8\tSelText=[5a1\\r\\n2bc]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelLength=32768\terr=0\tSelStart=0\tSelLength=8\tSelText=[5a1\\r\\n2bc]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelLength=65535\terr=0\tSelStart=0\tSelLength=8\tSelText=[5a1\\r\\n2bc]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelLength=65536\terr=380\tSelStart=0\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelStart=1 SelLength=-0.4\terr=0\tSelStart=1\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelText=empty, nothing selected\terr=0\tSelStart=1\tSelLength=0\tSelText=[]\tText=[5a1\\r\\n2bc]\tChanges=9\tSeen=",
        "SelText=Z into a full box\terr=0\tSelStart=3\tSelLength=0\tSelText=[]\tText=[123456789012]\tChanges=10\tSeen=|123456789012@0",
        "SelText=Z over a selection, full box\terr=0\tSelStart=4\tSelLength=0\tSelText=[]\tText=[123Z6789012]\tChanges=12\tSeen=|1236789012@3|123Z6789012@4",
        "SelText=same char over a selection\terr=0\tSelStart=1\tSelLength=0\tSelText=[]\tText=[123Z6789012]\tChanges=14\tSeen=|23Z6789012@0|123Z6789012@1",
    ];

    private static string Esc(string text) => text.Replace("\r", "\\r").Replace("\n", "\\n");

    private static ComponentInstance Box(string name, params (PropertyDefinition Property, object Value)[] set)
    {
        var instance = new ComponentInstance(VBClasses.TextBox, name);
        foreach (var (property, value) in set)
            Assert.IsTrue(instance.TrySet(property, value));
        return instance;
    }

    // The rows the steps give on the box, in a window with a sink: shown, or loaded and not shown.
    private static List<string> Script(ComponentInstance instance, bool shown) => UI.Run(() =>
    {
        var box = new VBTextBox(instance);
        var (changes, seen) = (0, "");
        var sink = new RecordingSink();
        var window = new Window { Width = 400, Height = 300, Content = new Canvas { Children = { box } } };
        VBControl.SetEventSink(window, sink);
        if (shown)
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
        }
        sink.Handler = (source, definition, _) =>
            {
                if (ReferenceEquals(source, instance) && ReferenceEquals(definition, VBEvents.Change))
                {
                    changes++;
                    seen += $"|{instance.Get(P.Text)}@{box.SelStart}";
                }
            };
        var rows = new List<string>();
        foreach (var (label, run) in Steps)
        {
            var error = "0";
            try
            {
                run(box, instance);
            }
            catch (FormErrorException exception)
            {
                error = exception.Error.Number.ToString();
            }
            catch (Exception exception)
            {
                error = exception.GetType().Name;
            }
            if (label == "reset")
                changes = 0;
            rows.Add($"{label}\terr={error}\tSelStart={box.SelStart}\tSelLength={box.SelLength}\tSelText=[{Esc(box.SelText)}]" +
                $"\tText=[{Esc(instance.Get(P.Text))}]\tChanges={changes}\tSeen={Esc(seen)}");
            seen = "";
        }
        VBControl.SetEventSink(window, null);
        window.Close();
        return rows;
    });

    [TestMethod]
    public void ASingleLineBox_SelectsAndReplaces_AsVB6Does() => Expect.Lines(Text1, Script(Box("Text1"), shown: true));

    [TestMethod]
    public void AMultiLineBox_DeletesThenInserts_EachWithItsChange() => Expect.Lines(Text2, Script(Box("Text2", (P.MultiLine, true)), shown: true));

    [TestMethod]
    public void ASingleLineBoxWithAMaxLength_TakesWhatFits() => Expect.Lines(Text3, Script(Box("Text3", (P.MaxLength, 12)), shown: true));

    [TestMethod]
    public void AMultiLineBoxWithAMaxLength_TakesWhatFits() =>
        Expect.Lines(Text6, Script(Box("Text6", (P.MultiLine, true), (P.MaxLength, 12)), shown: true));

    [TestMethod]
    public void ALockedBox_AndAPasswordBox_AreSetFromCodeAsAnyOther()
    {
        Expect.Lines(Text1, Script(Box("Text1", (P.Locked, true)), shown: true));
        Expect.Lines(Text1, Script(Box("Text1", (P.PasswordChar, "*")), shown: true));
    }

    [TestMethod]
    public void ABoxOfAFormLoadedAndNotShown_IsSetAsAShownOne()
    {
        Expect.Lines(Text1, Script(Box("Text1"), shown: false));
        Expect.Lines(Text2, Script(Box("Text2", (P.MultiLine, true)), shown: false));
    }

    // The user's selection, from its anchor back to its caret, is the program's.
    [TestMethod]
    public void TheUsersSelection_IsTheProgramsToo()
    {
        var user = UI.Run(() =>
        {
            var instance = Box("Text1");
            var box = new VBTextBox(instance);
            var window = new Window { Width = 400, Height = 300, Content = new Canvas { Children = { box } } };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            instance.Set(P.Text, "Hello world");
            box.SelectionStart = 7;
            box.SelectionEnd = 3;
            var user = (box.SelStart, box.SelLength);
            window.Close();
            return user;
        });
        Assert.AreEqual((3, 4), user);
    }

    [TestMethod]
    public void SelText_IsNotNull() =>
        Assert.ThrowsExactly<ArgumentNullException>(() => UI.Run(() => new VBTextBox(Box("Text1")).SelText = null!));
}
