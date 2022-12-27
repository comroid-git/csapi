using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace comroid.csapi.common
    // ReSharper disable once ArrangeNamespaceBody
{
    public static class StringUtil
    {
        public static string Adjust(this string str, int len, bool rightBound = false, bool doFill = true,
            char fill = ' ')
        {
            str = str.Trim();
            var n = len - str.Length;
            if (n < 0) // remove n chars
                if (rightBound)
                    str = str.Substring(0, len);
                else str = str.Substring(Math.Abs(n), str.Length + n);
            else if (n > 0 && doFill)
            {
                // pre-/append n chars
                var extra = string.Empty;
                for (int i = 0; i < n; i++)
                    extra += fill;
                if (rightBound)
                    str = extra + str;
                else str += extra;
            }

            return str;
        }
    }

    [SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeEvident")]
    public class TextTable
    {
        public readonly List<Column> Columns = new List<Column>();
        public readonly List<Row> Rows = new List<Row>();
        private readonly bool _header;
        private readonly LineMode? _lineMode;
        private const int margin = 1;

        public TextTable(bool header = true, LineMode? lineMode = null)
        {
            _header = header;
            _lineMode = lineMode;
        }

        public Column AddColumn(string name, bool justifyRight = false)
        {
            var col = new Column(name, justifyRight);
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

        public void AddSeparator() => Rows.Add(new SeparatorRow());

        public override string ToString()
        {
            var cc = Columns.Count;
            var lens = new int[cc];
            for (var i = 0; i < cc; i++)
            {
                // for each column, collect longest data
                var col = Columns[i];
                foreach (var data in new[] { _header ? col.Name : string.Empty }
                             .Concat(Rows
                                 .Where(row => !row.Separator)
                                 .Select(row => row._data[col])))
                {
                    var len = data.ToString()!.Length;
                    if (lens[i] < len)
                        lens[i] = len;
                }
            }

            // todo: include outlines & inlines
            var sb = new StringBuilder();
            var lines = _lineMode != null;
            var totalW = lens.Aggregate(0, (a, b) => a + b + margin/**/) + cc + lens.Length * margin;
            var colTrims = lens.Select(x => 1 + x + margin * 2).Append(0).ToArray();
            if (_header)
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
                        _header ? LineType.IdxVertical : LineType.ConD,
                        _header ? LineType.Bold : LineType.None));
            int rc = 0;
            for (var ri = 0; ri < Rows.Count; ri++)
            {
                var row = Rows[ri];
                if (lines && row.Separator)
                {
                    if (rc > 0 && ri < Rows.Count) 
                        sb.Append(HoriDetailLine(totalW, colTrims, LineType.IdxVertical));
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

        public class Row
        {
            internal readonly Dictionary<Column, object> _data = new Dictionary<Column, object>();

            protected internal virtual bool Separator => false;

            public Row SetData(Column col, object data)
            {
                _data[col] = data;
                return this;
            }
        }

        private class SeparatorRow : Row
        {
            protected internal override bool Separator => true;
        }

        public class Column
        {
            public readonly string Name;
            internal readonly bool _justifyRight;

            public Column(string name, bool justifyRight)
            {
                Name = name;
                _justifyRight = justifyRight;
            }
        }

        #region Table Lining
        private char GetLining(LineType type)
        {
            var db = LiningSymbols[_lineMode ?? LineMode.ASCII];
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

            return str + '\n';
        }

        public enum LineMode
        {
            ASCII,
            Unicode
        }

        private static readonly Dictionary<LineMode, Dictionary<LineType, char>> LiningSymbols = new Dictionary<LineMode, Dictionary<LineType, char>>
        {
            {
                LineMode.ASCII,
                new Dictionary<LineType, char>
                {
                    {LineType.IdxHorizontal, '-' },
                    {LineType.IdxVertical, '|'},
                    {LineType.IdxCrossing, '+'}
                }
            },
            {
                LineMode.Unicode,
                new Dictionary<LineType, char>
                {
                    {LineType.IdxHorizontal, '─'},
                    {LineType.IdxVertical, '│'},
                    {LineType.IdxCrossing, '┼'},
                    {LineType.IdxTHoriU, '┴'},
                    {LineType.IdxTHoriD, '┬'},
                    {LineType.IdxTVertL, '┤'},
                    {LineType.IdxTVertR, '├'},
                    {LineType.IdxCornerTL, '┌'},
                    {LineType.IdxCornerTR, '┐'},
                    {LineType.IdxCornerBL, '└'},
                    {LineType.IdxCornerBR, '┘'},
                    {LineType.Bold | LineType.IdxHorizontal, '═'},
                    {LineType.Bold | LineType.IdxVertical, '║'},
                    {LineType.Bold | LineType.IdxCrossing, '╬'},
                    {LineType.Bold | LineType.IdxTHoriU, '╩'},
                    {LineType.Bold | LineType.IdxTHoriD, '╦'},
                    {LineType.Bold | LineType.IdxTVertL, '╣'},
                    {LineType.Bold | LineType.IdxTVertR, '╠'},
                    {LineType.Bold | LineType.IdxCornerTL, '╔'},
                    {LineType.Bold | LineType.IdxCornerTR, '╗'},
                    {LineType.Bold | LineType.IdxCornerBL, '╚'},
                    {LineType.Bold | LineType.IdxCornerBR, '╝'}
                }
            }
        };

        [Flags]
        private enum LineType : int
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
}
