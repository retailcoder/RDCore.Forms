using Avalonia.Controls;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// A TextBox's SelStart, SelLength and SelText as a program reads and sets them: on the component, as the runtime's host does. The
// expected rows are VB6's, native and p-code agreeing, for the same steps on a box of each kind: after each step, the error, the
// selection, the text, the Changes so far, and the text and SelStart each Change's handler saw. VB6 rounds 2.6 to 3 and -0.4 to 0 and
// takes the number 5 as "5" before the box sees them: the steps here pass what the box receives. A component with no view has the same
// selection and text; it raises no Change.
[TestClass]
public sealed class TextSelectionTests
{
    private static readonly (string Label, Action<ComponentInstance> Run)[] Steps =
    [
        ("reset", c => c.Let(P.Text, "Hello world")),
        ("get", c => { }),
        ("SelStart=3", c => c.Let(P.SelStart, 3)),
        ("SelLength=4", c => c.Let(P.SelLength, 4)),
        ("SelStart=5 after a selection", c => c.Let(P.SelStart, 5)),
        ("SelStart=100", c => c.Let(P.SelStart, 100)),
        ("SelStart=-1", c => c.Let(P.SelStart, -1)),
        ("SelStart=0 SelLength=100", c => { c.Let(P.SelStart, 0); c.Let(P.SelLength, 100); }),
        ("SelLength=-1", c => c.Let(P.SelLength, -1)),
        ("SelStart=2 SelLength=3 SelText=XY", c => { c.Let(P.SelStart, 2); c.Let(P.SelLength, 3); c.Let(P.SelText, "XY"); }),
        ("SelText=empty with a selection", c => { c.Let(P.SelStart, 0); c.Let(P.SelLength, 2); c.Let(P.SelText, ""); }),
        ("SelText=same text", c => { c.Let(P.SelStart, 0); c.Let(P.SelLength, 3); c.Let(P.SelText, c.Get(P.Text)[..3]); }),
        ("SelText=long at end", c => { c.Let(P.SelStart, c.Get(P.Text).Length); c.Let(P.SelText, "ABCDEFGHIJ"); }),
        ("Text=abc after a selection", c => { c.Let(P.SelStart, 1); c.Let(P.SelLength, 2); c.Let(P.Text, "abc"); }),
        ("Text=same after a selection", c => { c.Let(P.SelStart, 1); c.Let(P.SelLength, 1); c.Let(P.Text, c.Get(P.Text)); }),
        ("SelText=CRLF", c => { c.Let(P.SelStart, 1); c.Let(P.SelLength, 0); c.Let(P.SelText, "1\r\n2"); }),
        ("SelStart=3 in CRLF", c => c.Let(P.SelStart, 3)),
        ("SelStart=2.6", c => c.Let(P.SelStart, 3)),
        ("SelStart=70000", c => c.Let(P.SelStart, 70000)),
        ("SelLength=70000", c => { c.Let(P.SelStart, 0); c.Let(P.SelLength, 70000); }),
        ("SelText=5", c => { c.Let(P.SelStart, 0); c.Let(P.SelText, "5"); }),
        ("SelStart=4 SelLength=100", c => { c.Let(P.SelStart, 4); c.Let(P.SelLength, 100); }),
        ("SelStart=32767", c => c.Let(P.SelStart, 32767)),
        ("SelStart=32768", c => c.Let(P.SelStart, 32768)),
        ("SelStart=65535", c => c.Let(P.SelStart, 65535)),
        ("SelStart=65536", c => c.Let(P.SelStart, 65536)),
        ("SelLength=32767", c => { c.Let(P.SelStart, 0); c.Let(P.SelLength, 32767); }),
        ("SelLength=32768", c => { c.Let(P.SelStart, 0); c.Let(P.SelLength, 32768); }),
        ("SelLength=65535", c => { c.Let(P.SelStart, 0); c.Let(P.SelLength, 65535); }),
        ("SelLength=65536", c => { c.Let(P.SelStart, 0); c.Let(P.SelLength, 65536); }),
        ("SelStart=1 SelLength=-0.4", c => { c.Let(P.SelStart, 1); c.Let(P.SelLength, 0); }),
        ("SelText=empty, nothing selected", c => { c.Let(P.SelStart, 1); c.Let(P.SelText, ""); }),
        ("SelText=Z into a full box", c => { c.Let(P.Text, "123456789012"); c.Let(P.SelStart, 3); c.Let(P.SelText, "Z"); }),
        ("SelText=Z over a selection, full box", c => { c.Let(P.SelStart, 3); c.Let(P.SelLength, 2); c.Let(P.SelText, "Z"); }),
        ("SelText=same char over a selection", c => { c.Let(P.SelStart, 0); c.Let(P.SelLength, 1); c.Let(P.SelText, c.Get(P.Text)[..1]); }),
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
                    seen += $"|{instance.Get(P.Text)}@{instance.Read(P.SelStart)}";
                }
            };
        var rows = Run(instance, label =>
        {
            if (label == "reset")
                changes = 0;
            var row = $"\tChanges={changes}\tSeen={Esc(seen)}";
            seen = "";
            return row;
        });
        VBControl.SetEventSink(window, null);
        window.Close();
        return rows;
    });

    // The rows the steps give on the component, each with what `events` adds after the step.
    private static List<string> Run(ComponentInstance instance, Func<string, string> events)
    {
        var rows = new List<string>();
        foreach (var (label, run) in Steps)
        {
            var error = "0";
            try
            {
                run(instance);
            }
            catch (FormErrorException exception)
            {
                error = exception.Error.Number.ToString();
            }
            catch (Exception exception)
            {
                error = exception.GetType().Name;
            }
            rows.Add($"{label}\terr={error}\tSelStart={instance.Read(P.SelStart)}\tSelLength={instance.Read(P.SelLength)}" +
                $"\tSelText=[{Esc((string)instance.Read(P.SelText))}]\tText=[{Esc(instance.Get(P.Text))}]{events(label)}");
        }
        return rows;
    }

    // VB6's rows without their events: a component with no view raises none.
    private static string[] WithoutEvents(string[] rows) => [.. rows.Select(row => row[..row.IndexOf("\tChanges=", StringComparison.Ordinal)])];

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

    [TestMethod]
    public void ABoxWithNoView_HoldsItsSelection()
    {
        Expect.Lines(WithoutEvents(Text1), Run(Box("Text1"), _ => ""));
        Expect.Lines(WithoutEvents(Text2), Run(Box("Text2", (P.MultiLine, true)), _ => ""));
        Expect.Lines(WithoutEvents(Text3), Run(Box("Text3", (P.MaxLength, 12)), _ => ""));
        Expect.Lines(WithoutEvents(Text6), Run(Box("Text6", (P.MultiLine, true), (P.MaxLength, 12)), _ => ""));
    }

    // The component's selection shows in the box; a text from code shows none.
    [TestMethod]
    public void TheBox_ShowsTheComponentsSelection()
    {
        var shown = UI.Run(() =>
        {
            var instance = Box("Text1");
            instance.Let(P.Text, "Hello world");
            instance.Let(P.SelStart, 2);
            instance.Let(P.SelLength, 3);
            var box = new VBTextBox(instance);
            var window = new Window { Width = 400, Height = 300, Content = new Canvas { Children = { box } } };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var bound = (box.SelectionStart, box.SelectionEnd);
            instance.Let(P.SelStart, 4);
            instance.Let(P.SelLength, 5);
            var set = (box.SelectionStart, box.SelectionEnd);
            instance.Let(P.Text, "abc");
            var text = (box.SelectionStart, box.SelectionEnd);
            window.Close();
            return (bound, set, text);
        });
        Assert.AreEqual(((2, 5), (4, 9), (0, 0)), shown);
    }

    // A change to the box's text leaves the component's selection where the box's caret is, even where the caret does not move.
    [TestMethod]
    public void TheUsersText_KeepsTheBoxsCaret()
    {
        var caret = UI.Run(() =>
        {
            var instance = Box("Text1");
            var box = new VBTextBox(instance);
            var window = new Window { Width = 400, Height = 300, Content = new Canvas { Children = { box } } };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            box.Text = "Hello world";
            box.CaretIndex = 5;
            box.Text = "Hello";
            var caret = (instance.Get(P.SelStart), box.CaretIndex);
            window.Close();
            return caret;
        });
        Assert.AreEqual((5, 5), caret);
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
