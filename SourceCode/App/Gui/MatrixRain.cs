using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Habbo_Downloader.App.Gui
{
    /// <summary>
    /// Classic Matrix "digital rain" effect using digits 0-9. Sits behind the menu
    /// content with IsHitTestVisible = false so it never intercepts clicks.
    /// Head character is bright phosphor green; trailing characters fade through
    /// dim and very dim green, low alpha, so it stays as background atmosphere
    /// without competing with the foreground UI.
    /// </summary>
    public sealed class MatrixRain : Control
    {
        private readonly Random _rng = new();
        private readonly DispatcherTimer _timer;
        private int _cols;
        private int _rows;
        private int[] _heads = Array.Empty<int>();
        private char[][] _grid = Array.Empty<char[]>();
        private const double CellW = 14;
        private const double CellH = 18;

        // Pre-built brushes - the head is the only bright one; the trail is dim and
        // semi-transparent so it never overpowers buttons, labels, the input field.
        private static readonly IBrush HeadBrush   = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x41));
        private static readonly IBrush NearBrush   = new SolidColorBrush(Color.FromArgb(0xAA, 0x00, 0xAA, 0x33));
        private static readonly IBrush MidBrush    = new SolidColorBrush(Color.FromArgb(0x60, 0x00, 0x77, 0x22));
        private static readonly IBrush FarBrush    = new SolidColorBrush(Color.FromArgb(0x30, 0x00, 0x55, 0x18));
        private static readonly Typeface MonoFace  = new Typeface("Cascadia Mono,Consolas,Courier New");

        public MatrixRain()
        {
            ClipToBounds = true;
            IsHitTestVisible = false;
            // background fill is done in Render() since Control has no Background prop

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(110) };
            _timer.Tick += (_, __) => Step();

            AttachedToVisualTree += (_, __) => { Rebuild(); _timer.Start(); };
            DetachedFromVisualTree += (_, __) => _timer.Stop();
            SizeChanged += (_, __) => Rebuild();
        }

        private void Rebuild()
        {
            if (Bounds.Width <= 0 || Bounds.Height <= 0)
            {
                _cols = 0; _rows = 0;
                return;
            }
            _cols = Math.Max(1, (int)(Bounds.Width  / CellW));
            _rows = Math.Max(1, (int)(Bounds.Height / CellH));
            _heads = new int[_cols];
            _grid = new char[_cols][];
            for (int c = 0; c < _cols; c++)
            {
                _heads[c] = _rng.Next(_rows);
                _grid[c] = new char[_rows];
                for (int r = 0; r < _rows; r++)
                {
                    _grid[c][r] = _rng.Next(4) == 0
                        ? (char)('0' + _rng.Next(10))
                        : ' ';
                }
            }
        }

        private void Step()
        {
            if (_cols == 0 || _rows == 0) return;
            for (int c = 0; c < _cols; c++)
            {
                int newHead = (_heads[c] + 1) % _rows;
                _grid[c][newHead] = (char)('0' + _rng.Next(10));
                _heads[c] = newHead;

                // occasional sparse cleanup so the column doesn't end up fully lit
                if (_rng.Next(20) == 0)
                {
                    int cleanRow = _rng.Next(_rows);
                    if (((cleanRow - newHead + _rows) % _rows) > 6)
                        _grid[c][cleanRow] = ' ';
                }
            }
            InvalidateVisual();
        }

        public override void Render(DrawingContext ctx)
        {
            base.Render(ctx);
            ctx.FillRectangle(Brushes.Black, new Rect(Bounds.Size));
            if (_cols == 0 || _rows == 0) return;

            for (int c = 0; c < _cols; c++)
            {
                int head = _heads[c];
                for (int r = 0; r < _rows; r++)
                {
                    char ch = _grid[c][r];
                    if (ch == ' ') continue;

                    int dist = (head - r + _rows) % _rows;
                    IBrush brush = dist switch
                    {
                        0       => HeadBrush,
                        < 4     => NearBrush,
                        < 10    => MidBrush,
                        < 18    => FarBrush,
                        _       => null!
                    };
                    if (brush == null) continue;

                    var ft = new FormattedText(
                        ch.ToString(),
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        MonoFace,
                        CellH * 0.9,
                        brush);

                    ctx.DrawText(ft, new Point(c * CellW, r * CellH));
                }
            }
        }
    }
}
