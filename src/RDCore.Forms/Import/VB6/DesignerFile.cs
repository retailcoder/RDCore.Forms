// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

namespace RDCore.Forms.Import.VB6;

// The designer half of a .frm, .ctl or .pag file as written: header lines, then one Begin/End block per component, each with its
// property lines and BeginProperty/EndProperty bags. Everything after the root block's End is the module text.
internal sealed class DesignerFile
{
    private DesignerFile(List<DesignerProperty> header, DesignerBlock? root, string moduleText, int moduleLine)
    {
        Header = header;
        Root = root;
        ModuleText = moduleText;
        ModuleLine = moduleLine;
    }

    // Each "Name = Value" line before the root Begin other than VERSION: the Object lines naming the control libraries the form uses.
    public List<DesignerProperty> Header { get; }

    public DesignerBlock? Root { get; }

    public string ModuleText { get; }

    // The 1-based line the module text starts on.
    public int ModuleLine { get; }

    public static DesignerFile Parse(string text, DiagnosticBag diagnostics)
    {
        var header = new List<DesignerProperty>();
        var blocks = new Stack<DesignerBlock>();
        var bags = new Stack<DesignerBag>();
        DesignerBlock? root = null;

        // a block's or a bag's text is a range of the file, cut only when it is read, so closing a block costs nothing however
        // deep it is.
        var lineNumber = 0;
        var previousLineEnd = 0;
        for (var start = 0; start < text.Length;)
        {
            var (end, next) = LineAt(text, start);
            var lineStart = start;
            var line = text[start..end].Trim();
            start = next;
            lineNumber++;
            if (line.Length == 0)
            {
                continue;
            }

            var keyword = FirstWord(line);
            if (root is null)
            {
                if (keyword.Equals("VERSION", StringComparison.OrdinalIgnoreCase))
                {
                    previousLineEnd = end;
                    continue;
                }

                if (keyword.Equals("Begin", StringComparison.OrdinalIgnoreCase))
                {
                    root = BeginBlock(text, line, lineNumber, lineStart);
                    blocks.Push(root);
                }
                else if (SplitProperty(line, lineNumber) is { } headerLine)
                {
                    header.Add(headerLine);
                }
                else
                {
                    diagnostics.Add(VB6ImportDiagnosticKind.MalformedLine, lineNumber, line);
                }

                previousLineEnd = end;
                continue;
            }

            if (keyword.Equals("BeginProperty", StringComparison.OrdinalIgnoreCase))
            {
                var bag = new DesignerBag(text, BagName(line), lineNumber, lineStart);
                if (bags.TryPeek(out var outer))
                {
                    outer.Bags.Add(bag);
                }
                else
                {
                    blocks.Peek().Bags.Add(bag);
                }

                bags.Push(bag);
            }
            else if (keyword.Equals("EndProperty", StringComparison.OrdinalIgnoreCase))
            {
                if (bags.TryPop(out var bag))
                {
                    bag.End = end;
                }
                else
                {
                    diagnostics.Add(VB6ImportDiagnosticKind.UnmatchedEnd, lineNumber, line);
                }
            }
            else if (keyword.Equals("Begin", StringComparison.OrdinalIgnoreCase) && bags.Count == 0)
            {
                var block = BeginBlock(text, line, lineNumber, lineStart);
                blocks.Peek().Children.Add(block);
                blocks.Push(block);
            }
            else if (line.Equals("End", StringComparison.OrdinalIgnoreCase))
            {
                // the bags still open end with the line before this End.
                while (bags.TryPop(out var open))
                {
                    diagnostics.Add(VB6ImportDiagnosticKind.UnbalancedBlock, open.Line, open.Name);
                    open.End = previousLineEnd;
                }

                var block = blocks.Pop();
                block.End = end;
                if (blocks.Count == 0)
                {
                    return new DesignerFile(header, root, text[start..], lineNumber + 1);
                }
            }
            else if (SplitProperty(line, lineNumber) is { } property)
            {
                if (bags.TryPeek(out var bag))
                {
                    bag.Properties.Add(property);
                }
                else
                {
                    blocks.Peek().Properties.Add(property);
                }
            }
            else
            {
                diagnostics.Add(VB6ImportDiagnosticKind.MalformedLine, lineNumber, line);
            }

            previousLineEnd = end;
        }

        if (root is null)
        {
            diagnostics.Add(VB6ImportDiagnosticKind.NoDesignerBlock, Math.Max(lineNumber, 1));
        }

        // every block and bag still open ends with the file.
        while (bags.TryPop(out var open))
        {
            diagnostics.Add(VB6ImportDiagnosticKind.UnbalancedBlock, open.Line, open.Name);
            open.End = text.Length;
        }

        while (blocks.TryPop(out var open))
        {
            diagnostics.Add(VB6ImportDiagnosticKind.UnbalancedBlock, open.Line, open.Name);
            open.End = text.Length;
        }

        return new DesignerFile(header, root, "", lineNumber + 1);
    }

    // A line ends at CR LF, or at a CR or an LF alone; the last line may have no terminator.
    private static (int End, int Next) LineAt(string text, int start)
    {
        var end = text.IndexOfAny(['\r', '\n'], start);
        if (end < 0)
        {
            return (text.Length, text.Length);
        }

        var next = text[end] == '\r' && end + 1 < text.Length && text[end + 1] == '\n' ? end + 2 : end + 1;
        return (end, next);
    }

    private static string FirstWord(string line)
    {
        var space = line.IndexOfAny([' ', '\t', '=']);
        return space < 0 ? line : line[..space];
    }

    // Begin VB.CommandButton Command1: a block with no name is read as one named "", which the import keeps as text.
    private static DesignerBlock BeginBlock(string text, string line, int lineNumber, int lineStart)
    {
        var words = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        return new DesignerBlock(text, words.Length > 1 ? words[1] : "", words.Length > 2 ? words[2] : "", lineNumber, lineStart);
    }

    // BeginProperty Font {0BE35203-8F91-11CE-9DE3-00AA004BB851}: the bag's name, without the class id of the object it persists.
    private static string BagName(string line)
    {
        var words = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        return words.Length > 1 ? words[1] : "";
    }

    private static DesignerProperty? SplitProperty(string line, int lineNumber)
    {
        var equals = line.IndexOf('=');
        if (equals <= 0)
        {
            return null;
        }

        var name = line[..equals].Trim();
        return name.Length == 0 ? null : new DesignerProperty(name, line[(equals + 1)..].Trim(), lineNumber);
    }
}

// One Begin/End block: a component of the form.
internal sealed class DesignerBlock(string source, string typeName, string name, int line, int start)
{
    public string TypeName { get; } = typeName;

    public string Name { get; } = name;

    // The 1-based line of the block's Begin.
    public int Line { get; } = line;

    public List<DesignerProperty> Properties { get; } = [];

    public List<DesignerBag> Bags { get; } = [];

    public List<DesignerBlock> Children { get; } = [];

    // Where the block's text ends in the file: after its End, or at the end of the file for a block with none.
    public int End { get; set; } = source.Length;

    // The block's lines as the file has them, from its Begin to its End.
    public string Text => source[start..End];
}

// One BeginProperty/EndProperty bag, such as a Font.
internal sealed class DesignerBag(string source, string name, int line, int start)
{
    public string Name { get; } = name;

    public int Line { get; } = line;

    public List<DesignerProperty> Properties { get; } = [];

    public List<DesignerBag> Bags { get; } = [];

    // Where the bag's text ends in the file: after its EndProperty, or, for a bag with none, at the line before the End or the end
    // of the file that closes it.
    public int End { get; set; } = source.Length;

    // The bag's lines as the file has them, from its BeginProperty to its EndProperty.
    public string Text => source[start..End];
}

// One "Name = Value" line, the value as written, its comment included.
internal sealed record DesignerProperty(string Name, string Value, int Line);
