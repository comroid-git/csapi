using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace comroid.csapi.common
    // ReSharper disable once ArrangeNamespaceBody
{
// https://www.lihaoyi.com/post/BuildyourownCommandLinewithANSIescapecodes.html
    public static class AnsiUtil
    {
        public const string Reset = "\u001b[0m";
        public const string Black = "\u001b[30m";
        public const string Red = "\u001b[31m";
        public const string Green = "\u001b[32m";
        public const string Yellow = "\u001b[33m";
        public const string Blue = "\u001b[34m";
        public const string Magenta = "\u001b[35m";
        public const string Cyan = "\u001b[36m";
        public const string White = "\u001b[37m";
        public const string BrightBlack = "\u001b[30;1m";
        public const string BrightRed = "\u001b[31;1m";
        public const string BrightGreen = "\u001b[32;1m";
        public const string BrightYellow = "\u001b[33;1m";
        public const string BrightBlue = "\u001b[34;1m";
        public const string BrightMagenta = "\u001b[35;1m";
        public const string BrightCyan = "\u001b[36;1m";
        public const string BrightWhite = "\u001b[37;1m";
        public const string BackgroundBlack = "\u001b[40m";
        public const string BackgroundRed = "\u001b[41m";
        public const string BackgroundGreen = "\u001b[42m";
        public const string BackgroundYellow = "\u001b[43m";
        public const string BackgroundBlue = "\u001b[44m";
        public const string BackgroundMagenta = "\u001b[45m";
        public const string BackgroundCyan = "\u001b[46m";
        public const string BackgroundWhite = "\u001b[47m";
        public const string BackgroundBrightBlack = "\u001b[40;1m";
        public const string BackgroundBrightRed = "\u001b[41;1m";
        public const string BackgroundBrightGreen = "\u001b[42;1m";
        public const string BackgroundBrightYellow = "\u001b[43;1m";
        public const string BackgroundBrightBlue = "\u001b[44;1m";
        public const string BackgroundBrightMagenta = "\u001b[45;1m";
        public const string BackgroundBrightCyan = "\u001b[46;1m";
        public const string BackgroundBrightWhite = "\u001b[47;1m";
        public const string Bold = "\u001b[1m";
        public const string Underline = "\u001b[4m";
        public const string Reversed = "\u001b[7m";
        public const string CursorUp = "\u001b[{1}A";
        public const string CursorDown = "\u001b[{1}B";
        public const string CursorRight = "\u001b[{1}C";
        public const string CursorLeft = "\u001b[{1}D";
        public const string ClearScreen = "\u001b[{2}J";
        public const string ClearLine = "\u001b[{2}K";

        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;
        private const string ERROR_MESSAGE = "Unable to load ANSI support";
        private static readonly Log log = new(typeof(AnsiUtil));

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        public static string ByteColor(byte b) => $"\u001b[38;5;${b}m";
        public static string CursorPos(int row, int col) => $"\u001b[{row};{col}H";

        public static bool Enabled => log.RunWithExceptionLogger(
            () => GetConsoleMode(GetStdHandle(STD_OUTPUT_HANDLE), out var mode) &&
                  (mode & ENABLE_VIRTUAL_TERMINAL_PROCESSING) == ENABLE_VIRTUAL_TERMINAL_PROCESSING,
            ERROR_MESSAGE, _ => false);

        public static bool Init()
        {
            return log.RunWithExceptionLogger(() =>
            {
                if (Enabled)
                    return false;
                var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
                if (!GetConsoleMode(iStdOut, out uint outConsoleMode))
                {
                    Console.WriteLine("Failed to get output console mode");
                    Console.ReadKey();
                    return false;
                }

                outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;
                if (!SetConsoleMode(iStdOut, outConsoleMode))
                {
                    Console.WriteLine($"Failed to set output console mode, error code: {GetLastError()}");
                    Console.ReadKey();
                    return false;
                }

                return Enabled;
            }, ERROR_MESSAGE, _ => false);
        }

        public static bool ContainsAnsi(this string str) => str.Contains('\u001b') && DebugUtil
            .GetConstantsOfClass<string>(typeof(AnsiUtil))
            .Select(e => e.Value)
            .Any(str.Contains!);
        public static string RemoveAnsi(this string str) => DebugUtil
            .GetConstantsOfClass<string>(typeof(AnsiUtil))
            .Select(e => e.Value)
            .Where(x => x != null)
            .Aggregate(str, (left, right) => left.Replace(right!, string.Empty));
    }
}