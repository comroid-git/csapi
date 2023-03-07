using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using comroid.common;

namespace comroid.common;

// ReSharper disable once ArrangeNamespaceBody


public static class StringUtil
{
    public static string Adjust(this string str, int len, bool rightBound = false, bool doFill = true,
        char fill = ' ')
    {
        str = str.Trim();
        var n = len - str.Length;
        if (n < 0) // remove n chars
        {
            if (rightBound)
                str = str.Substring(0, len);
            else str = str.Substring(Math.Abs(n), str.Length + n);
        }
        else if (n > 0 && doFill)
        {
            // pre-/append n chars
            var extra = string.Empty;
            for (var i = 0; i < n; i++)
                extra += fill;
            if (rightBound)
                str = extra + str;
            else str += extra;
        }

        return str;
    }

    public static string StripExtension(this string str, string ext) => str.Substring(0, str.IndexOf(ext));

    private static readonly Regex NewLinePattern = new("[^\\r]\\n", RegexOptions.Multiline | RegexOptions.CultureInvariant);
    public static string Cleanup(this string str, bool replaceLfWithCrlf = false, bool keepAnsi = false, bool trimWhitespaces = false)
    {
        if (!replaceLfWithCrlf) while (NewLinePattern.IsMatch(str))
            str = NewLinePattern.Replace(str, "\r\n");
        if (!keepAnsi) while (str.ContainsAnsi())
            str = str.RemoveAnsi();
        if (trimWhitespaces)
            return str.Trim();
        return str;
    }
}

[SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeEvident")]
public class TextTable
{
    private const int margin = 1;

    private int[]? colLengths;

    public TextTable()
    {
        Rows = new List<Row>();
        Columns = new List<Column>();
    }

    public List<Column> Columns { get; }
    public virtual List<Row> Rows { get; }
    public string? Title { get; set; }
    public bool Header { get; set; } = true;
    public LineMode? Lines { get; set; }

    public Column AddColumn(string name, bool justifyRight = false)
    {
        var col = new Column(justifyRight) { Name = name };
        Columns.Add(col);
        return col;
    }

    public Row AddRow()
    {
        var row = new Row();
        foreach (var col in Columns)
            row._data[col] = string.Empty;
        Rows.Add(row);
        return row;
    }

    public void AddSeparator(LineType? detail = null)
    {
        Rows.Add(new SeparatorRow { Detail = detail ?? LineType.None });
    }

    public override string ToString()
    {
        var cc = Columns.Count;
        var lens = new int[cc];
        for (var i = 0; i < cc; i++)
        {
            // for each column, collect longest data
            var col = Columns[i];
            foreach (var data in new[] { Header ? col.Name : string.Empty }
                         .Concat(Rows
                             .Where(row => !row.Separator)
                             .Select(row => row._data[col])))
            {
                var len = data.ToString()!.Cleanup(trimWhitespaces: true).Length;
                if (lens[i] < len)
                    lens[i] = len;
            }
        }

        // todo: include outlines & inlines
        var sb = new StringBuilder();
        var lines = Lines != null;
        var totalW = lens.Aggregate(0, (a, b) => a + b + margin /**/) + cc + lens.Length * margin;
        var colTrims = lens.Select(x => 1 + x + margin * 2).Append(0).ToArray();
        if (Header)
        {
            if (lines) sb.Append(HoriDetailLine(totalW, colTrims, LineType.ConD, LineType.Bold));

            var dir = LineType.ConR;
            for (var i = 0; i < cc; i++)
            {
                if (i == cc)
                    dir &= ~LineType.ConR;
                if (lines)
                    sb.Append(VertIndent(dir, LineType.Bold));
                dir |= LineType.ConL;
                sb.Append(Columns[i].Name.Adjust(lens[i]));
                if (!lines)
                    sb.Append(' ');
            }

            if (lines)
                sb.Append(VertIndent(dir, LineType.Bold));
            sb.AppendLine();
        }

        if (lines)
            sb.Append(HoriDetailLine(totalW, colTrims,
                Header ? LineType.IdxVertical : LineType.ConD,
                Header ? LineType.Bold : LineType.None));
        var rc = 0;
        var rows = Rows;
        for (var ri = 0; ri < rows.Count; ri++)
        {
            var row = rows[ri];
            if (lines && row.Separator)
            {
                if (rc > 0 && ri < rows.Count)
                    sb.Append(HoriDetailLine(totalW, colTrims, LineType.IdxVertical, row.Detail));
                continue;
            }

            var dir = LineType.ConR;
            for (var i = 0; i < cc; i++)
            {
                var col = Columns[i];
                if (i == cc)
                    dir &= ~LineType.ConR;
                if (lines)
                    sb.Append(VertIndent(dir));
                dir |= LineType.ConL;
                sb.Append(row._data[col].ToString()!.Adjust(lens[i], col._justifyRight));
                if (!lines)
                    sb.Append(' ');
            }

            if (lines)
                sb.Append(VertIndent(LineType.ConL));
            rc += 1;
            sb.AppendLine();
        }

        if (lines) sb.Append(HoriDetailLine(totalW, colTrims, LineType.ConU));

        return sb.ToString();
    }

    public void WriteLine(Row row, TextWriter writer = null!)
    {
        writer ??= Console.Out;
        var cc = Columns.Count;
        if (cc != (colLengths?.Length ?? -1))
        {
            (var old, colLengths) = (colLengths, new int[cc]);
            Array.Copy(old ?? Array.Empty<int>(), colLengths, old?.Length ?? 0);
        }

        var sb = new StringBuilder();
        var indent = VertIndent(LineType.IdxHorizontal);
        for (var i = 0; i < cc; i++)
        {
            // for each column, collect longest data
            var col = Columns[i];
            var text = row._data[col].ToString()!;
            var len = text.Length;
            if (colLengths![i] < len)
                colLengths[i] = Math.Min(len, 64);

            // and then write column with updated lengths
            var lines = Lines != null;
            if (lines && row.Separator)
            {
                var totalW = colLengths.Aggregate(0, (a, b) => a + b + margin /**/) + cc + colLengths.Length * margin;
                var colTrims = colLengths.Select(x => 1 + x + margin * 2).Append(0).ToArray();
                sb.Append(HoriDetailLine(totalW, colTrims, LineType.IdxVertical, row.Detail));
                continue;
            }
            
            sb.Append(text.Adjust(Math.Max(colLengths[i], text.Length), col._justifyRight));
            if (i < cc - 1)
                if (lines) sb.Append(indent);
                else sb.Append(' ');
        }

        writer.WriteLine(sb);
    }

    #region Components

    public class Row
    {
        internal readonly Dictionary<Column, object> _data = new Dictionary<Column, object>();
        public virtual LineType Detail { get; set; }

        protected internal virtual bool Separator => false;

        public Row SetData(Column col, object data)
        {
            _data[col] = data;
            return this;
        }
    }

    protected class SeparatorRow : Row
    {
        public override LineType Detail { get; set; }
        protected internal override bool Separator => true;
    }

    public class Column
    {
        internal readonly bool _justifyRight;

        public Column(bool justifyRight)
        {
            _justifyRight = justifyRight;
        }

        public string Name { get; set; } = null!;
    }

    #endregion

    #region Table Lining

    private char GetLining(LineType type)
    {
        var db = LiningSymbols[Lines ?? LineMode.ASCII];
        if (db.Keys.Contains(type))
            return db[type];
        (int nh, char c)? best = null;
        foreach (var (id, c) in db)
        {
            var comp = type & id & ~LineType.Bold;
            var bits = Convert.ToString((int)comp, 2).Count(x => x == '1');
            if (comp == (type & ~LineType.Bold))
                return c;
            if (bits >= (best?.nh ?? 0))
                best = (bits, c);
        }

        return best?.c ?? '#';
    }

    private string VertIndent(LineType dir, LineType detail = LineType.None)
    {
        var str = string.Empty;
        if ((dir & LineType.ConL) != 0)
            str += new string(' ', margin);
        str += GetLining(LineType.IdxVertical | detail);
        if ((dir & LineType.ConR) != 0)
            str += new string(' ', margin);
        return str;
    }

    private string HoriDetailLine(int len, int[] trims, LineType diff, LineType detail = LineType.None)
    {
        var line = LineType.ConR;
        var str = string.Empty;
        int ti = 0, tc = -1;
        for (var i = 0; i <= len; i++)
        {
            if (i == 1)
                line |= LineType.ConL;
            if (i == len)
                line &= ~LineType.ConR;

            var useDiff = false;
            // trim counter
            if (--tc <= 0 && ti < trims.Length)
            {
                useDiff = true;
                tc = trims[ti++];
            }

            str += GetLining(line | detail | (useDiff ? diff : 0));
        }

        return str + "\r\n";
    }

    public enum LineMode
    {
        ASCII,
        Unicode
    }

    private static readonly Dictionary<LineMode, Dictionary<LineType, char>> LiningSymbols =
        new Dictionary<LineMode, Dictionary<LineType, char>>
        {
            {
                LineMode.ASCII,
                new Dictionary<LineType, char>
                {
                    { LineType.IdxHorizontal, '-' },
                    { LineType.IdxVertical, '|' },
                    { LineType.IdxCrossing, '+' }
                }
            },
            {
                LineMode.Unicode,
                new Dictionary<LineType, char>
                {
                    { LineType.IdxHorizontal, '─' },
                    { LineType.IdxVertical, '│' },
                    { LineType.IdxCrossing, '┼' },
                    { LineType.IdxTHoriU, '┴' },
                    { LineType.IdxTHoriD, '┬' },
                    { LineType.IdxTVertL, '┤' },
                    { LineType.IdxTVertR, '├' },
                    { LineType.IdxCornerTL, '┌' },
                    { LineType.IdxCornerTR, '┐' },
                    { LineType.IdxCornerBL, '└' },
                    { LineType.IdxCornerBR, '┘' },
                    { LineType.Bold | LineType.IdxHorizontal, '═' },
                    { LineType.Bold | LineType.IdxVertical, '║' },
                    { LineType.Bold | LineType.IdxCrossing, '╬' },
                    { LineType.Bold | LineType.IdxTHoriU, '╩' },
                    { LineType.Bold | LineType.IdxTHoriD, '╦' },
                    { LineType.Bold | LineType.IdxTVertL, '╣' },
                    { LineType.Bold | LineType.IdxTVertR, '╠' },
                    { LineType.Bold | LineType.IdxCornerTL, '╔' },
                    { LineType.Bold | LineType.IdxCornerTR, '╗' },
                    { LineType.Bold | LineType.IdxCornerBL, '╚' },
                    { LineType.Bold | LineType.IdxCornerBR, '╝' }
                }
            }
        };

    [Flags]
    public enum LineType
    {
        None = 0b0_0000,
        Bold = 0b1_0000,

        ConU = 0b0_0001,
        ConD = 0b0_0010,
        ConL = 0b0_0100,
        ConR = 0b0_1000,

        IdxHorizontal = ConL | ConR,
        IdxVertical = ConU | ConD,

        IdxCrossing = IdxHorizontal | IdxVertical,
        IdxTHoriU = IdxHorizontal | ConU,
        IdxTHoriD = IdxHorizontal | ConD,
        IdxTVertL = IdxVertical | ConL,
        IdxTVertR = IdxVertical | ConR,

        IdxCornerTL = ConD | ConR,
        IdxCornerTR = ConD | ConL,
        IdxCornerBL = ConU | ConR,
        IdxCornerBR = ConU | ConL
    }

    #endregion
}