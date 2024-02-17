using System.Collections;
using System.Drawing;
using comroid.common;
using comroid.textUI.model;
using Console = System.Console;
using Timer = System.Timers.Timer;

namespace comroid.textUI;

public abstract class UiPanel
{
    public TextInterface Interface { get; init; } = new model.Console(true);
    public Color Fill { get; set; } = Color.White;
    public Color Background { get; set; } = Color.Black;
    public long LastLength { get; private set; }

    public abstract IEnumerable<string> Print(int width, int height);

    public Timer Run(long refreshInterval, Action? callback = null)
    {
        var task = new Timer(refreshInterval) { AutoReset = true };
        task.Elapsed += (_, _) => Redraw();
        task.Start();
        return task;
    }

    public void Redraw()
    {
        lock (Interface)
        {
            Interface.Clear(LastLength);
            LastLength = 0;
            foreach (var line in Print((int)Interface.Size.X, (int)Interface.Size.Y))
            {
                var str = line;
                var len = str.Length;
                if (len > Interface.Size.X)
                    str = HandleLineTooLong(line, len);
                len = str.Length;
                LastLength += len;
                Interface.Write(str+'\n');
            }
        }
    }

    protected virtual string HandleLineTooLong(string line, int len)
    {
        var maxLen = Interface.Size.X;
        Log<UiPanel>.Warning($"Line too long: {len} > {maxLen}");
        return line.Adjust((int)maxLen);
    }
}

public class TextPanel : UiPanel
{
    public virtual string Text { get; init; } = string.Empty;

    public override IEnumerable<string> Print(int width, int height)
    {
        if (height <= 0)
        {
            Log<TextPanel>.Warning("Not enough space to fit " + this);
            goto end;
        }

        var nNewl = Text.Count(c=>c=='\n');
        var spacingH = height - (1 + nNewl);
        var halfSpacingH = spacingH / 2;
        var evenSpacingH = spacingH % 2 == 0;
        var limSpacingH = halfSpacingH;

        // top padding
        for (var h = 0; h < limSpacingH; h++)
            yield return "";

        // text content
        foreach (var str in Text.Split("\n"))
        {
            var spacingW = width - str.Length;
            var halfSpacingW = spacingW / 2;
            var evenSpacingW = spacingW % 2 == 0;
            var limSpacingW = halfSpacingW - (evenSpacingW?1:0);
            var spacer = string.Empty;
            for (var w = 0; w < limSpacingW; w++)
                spacer += ' ';
            yield return spacer + Text + ' ' + spacer;
        }

        // bottom padding
        limSpacingH -= evenSpacingH ? 1 : 0;
        for (var h = 0; h < limSpacingH; h++)
            yield return "";

        end: ;
    }
}

public class ListPanel : UiPanel
{
    public List<UiPanel> Children { get; init; } = new();

    public override IEnumerable<string> Print(int width, int height)
    {
        var childHeight = height / Children.Count;
        return Children.SelectMany(c => c.Print(width, childHeight));
    }
}

public class VerticalSplitPanel : UiPanel
{
    public List<UiPanel> Children { get; init; } = new();

    public override IEnumerable<string> Print(int width, int height)
    {
        var colWidth = width / Children.Count - 1;
        var enums = new IEnumerator<string>[Children.Count];
        for (var i = 0; i < Children.Count; i++)
            enums[i] = Children[i].Print(colWidth, height).GetEnumerator();
        while (enums.All(e => e.MoveNext()))
            yield return string.Join('|', enums.Select(e => e.Current));
    }
}

public class GridPanel : UiPanel
{
    public UiPanel[/*col*/][/*row*/] Panels { get; init; }
    private IEnumerable<VerticalSplitPanel> Rows => Panels.Select(row => new VerticalSplitPanel { Children = row.ToList() });

    public GridPanel(UiPanel[][] panels = null!)
    {
        Panels = panels;
    }

    public override IEnumerable<string> Print(int width, int height)
    {
        var rowHeight = height / Panels.Length - 1;
        var separator = string.Empty;
        for (var c = 0; c < width; c++) separator += '-';
        return Rows.SelectMany(row => row.Print(width, rowHeight).Append(separator));
    }
}