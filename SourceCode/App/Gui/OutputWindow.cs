using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Habbo_Downloader.App.Gui
{
    /// <summary>
    /// Mainframe-themed window that runs an action with Console.Out and Console.In
    /// rerouted into its log TextBox + input field. Buffered through a StringBuilder
    /// and refreshed at 60ms by a UI-thread timer to keep the log responsive without
    /// thrashing the renderer.
    /// </summary>
    public sealed class OutputWindow : Window
    {
        private readonly TaskCompletionSource _closedTcs = new();
        public Task ClosedTask => _closedTcs.Task;

        public OutputWindow(string title, Func<Task> action, GuiTheme theme)
        {
            Title = $"ALL-IN-1 CONVERTER - {title.ToUpperInvariant()}";
            Width = 1000;
            Height = 720;
            Background = theme.Background;
            FontFamily = new FontFamily(theme.FontFamily);
            FontSize = 14;
            CanResize = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var bufferLock = new object();
            var buffer = new StringBuilder();
            bool dirty = false;
            var actionDone = new ManualResetEventSlim(false);

            var grid = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"),
                Background = theme.Background
            };

            // Header
            var header = new TextBlock
            {
                Text = $"  ALL-IN-1 CONVERTER  -  {title.ToUpperInvariant()}  -  DATE: {DateTime.UtcNow:yyyy-MM-dd}  |  THEME: {theme.Kind}",
                Background = theme.HeaderBg,
                Foreground = theme.HeaderFg,
                Padding = new Avalonia.Thickness(8),
                FontWeight = FontWeight.Bold
            };
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            // Log pane
            var logBox = new TextBox
            {
                Text = "",
                Background = theme.Background,
                Foreground = theme.Foreground,
                BorderBrush = theme.FrameBorder,
                BorderThickness = new Avalonia.Thickness(2),
                Margin = new Avalonia.Thickness(8, 8, 8, 4),
                Padding = new Avalonia.Thickness(8),
                IsReadOnly = true,
                AcceptsReturn = true,
                FontFamily = new FontFamily(theme.FontFamily),
                TextWrapping = TextWrapping.NoWrap,
                FontSize = 13
            };
            Grid.SetRow(logBox, 1);
            grid.Children.Add(logBox);

            // Input row
            var inputGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
                Margin = new Avalonia.Thickness(8, 4, 8, 4)
            };
            var inputLabel = new TextBlock
            {
                Text = "INPUT> ",
                Foreground = theme.Foreground,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Avalonia.Thickness(0, 0, 8, 0),
                FontWeight = FontWeight.Bold
            };
            Grid.SetColumn(inputLabel, 0);
            inputGrid.Children.Add(inputLabel);

            var inputBox = new TextBox
            {
                Background = theme.InputBg,
                Foreground = theme.InputFg,
                CaretBrush = theme.InputCaret,
                BorderBrush = theme.FrameBorder,
                BorderThickness = new Avalonia.Thickness(1),
                Padding = new Avalonia.Thickness(6, 4),
                FontFamily = new FontFamily(theme.FontFamily),
                Watermark = "Type then ENTER to submit to the running action"
            };
            Grid.SetColumn(inputBox, 1);
            inputGrid.Children.Add(inputBox);

            var closeBtn = new Button
            {
                Content = "[CLOSE] (when finished)",
                Background = theme.ExitBg,
                Foreground = theme.ExitFg,
                BorderBrush = theme.ExitFg,
                BorderThickness = new Avalonia.Thickness(1),
                Padding = new Avalonia.Thickness(12, 4),
                Margin = new Avalonia.Thickness(8, 0, 0, 0),
                IsEnabled = false,
                FontFamily = new FontFamily(theme.FontFamily)
            };
            Grid.SetColumn(closeBtn, 2);
            inputGrid.Children.Add(closeBtn);

            Grid.SetRow(inputGrid, 2);
            grid.Children.Add(inputGrid);

            // Footer
            var footer = new TextBlock
            {
                Text = " Output captured live. Type into INPUT to answer prompts. Window closes when the action ends and you press [CLOSE]. ",
                Background = theme.FooterBg,
                Foreground = theme.FooterFg,
                Padding = new Avalonia.Thickness(8, 4)
            };
            Grid.SetRow(footer, 3);
            grid.Children.Add(footer);

            Content = grid;

            var writer = new BufferedTextWriter(buffer, bufferLock, () => dirty = true);
            var reader = new GuiTextReader();

            inputBox.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    var line = inputBox.Text ?? string.Empty;
                    lock (bufferLock) { buffer.Append(line).Append('\n'); dirty = true; }
                    reader.Submit(line);
                    inputBox.Text = "";
                    e.Handled = true;
                }
            };

            closeBtn.Click += (_, __) =>
            {
                if (actionDone.IsSet) Close();
            };

            // 60ms timer to flush buffer into the TextBox on the UI thread.
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(60) };
            timer.Tick += (_, __) =>
            {
                if (!dirty) return;
                string snapshot;
                lock (bufferLock)
                {
                    snapshot = buffer.ToString();
                    dirty = false;
                }
                logBox.Text = snapshot;
                logBox.CaretIndex = snapshot.Length;
            };

            Opened += async (_, __) =>
            {
                timer.Start();
                var oldOut = Console.Out;
                var oldIn = Console.In;
                Console.SetOut(writer);
                Console.SetIn(reader);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await action();
                    }
                    catch (Exception ex)
                    {
                        lock (bufferLock)
                        {
                            buffer.Append('\n').Append("!! ERROR: ").Append(ex.Message).Append('\n');
                            dirty = true;
                        }
                    }
                    finally
                    {
                        lock (bufferLock)
                        {
                            buffer.Append('\n').Append("--- ACTION COMPLETE. PRESS [CLOSE] OR ESC TO RETURN. ---").Append('\n');
                            dirty = true;
                        }
                        reader.Cancel();
                        actionDone.Set();
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            closeBtn.IsEnabled = true;
                            closeBtn.Focus();
                        });
                    }
                });

                Closed += (_, __) =>
                {
                    timer.Stop();
                    Console.SetOut(oldOut);
                    Console.SetIn(oldIn);
                    if (!_closedTcs.Task.IsCompleted) _closedTcs.TrySetResult();
                };

                KeyDown += (_, e) =>
                {
                    if (e.Key == Key.Escape && actionDone.IsSet) Close();
                };

                await Task.CompletedTask;
            };
        }

        private sealed class BufferedTextWriter : TextWriter
        {
            private readonly StringBuilder _buf;
            private readonly object _lock;
            private readonly Action _markDirty;
            public override Encoding Encoding => Encoding.UTF8;
            public BufferedTextWriter(StringBuilder buf, object lockObj, Action markDirty)
            { _buf = buf; _lock = lockObj; _markDirty = markDirty; }
            public override void Write(char value)
            { lock (_lock) { _buf.Append(value); } _markDirty(); }
            public override void Write(string? value)
            { if (string.IsNullOrEmpty(value)) return; lock (_lock) { _buf.Append(value); } _markDirty(); }
            public override void WriteLine()
            { lock (_lock) { _buf.Append('\n'); } _markDirty(); }
            public override void WriteLine(string? value)
            { lock (_lock) { _buf.Append(value ?? string.Empty).Append('\n'); } _markDirty(); }
        }

        private sealed class GuiTextReader : TextReader
        {
            private readonly System.Collections.Concurrent.BlockingCollection<string> _lines = new();
            private bool _cancelled;
            public void Submit(string line)
            {
                if (_cancelled) return;
                _lines.Add(line);
            }
            public void Cancel()
            {
                _cancelled = true;
                try { _lines.CompleteAdding(); } catch { }
            }
            public override string? ReadLine()
            {
                try { return _lines.Take(); }
                catch (InvalidOperationException) { return null; }
            }
        }
    }
}
