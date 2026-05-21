using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Habbo_Downloader.App.Runners;
using Terminal.Gui;

namespace Habbo_Downloader.App.Menus
{
    /// <summary>
    /// Runs an action inside a mainframe-styled TUI window that captures
    /// Console.Out and Console.In so plain Console.WriteLine /
    /// Console.ReadLine calls inside the action render into the same
    /// green/black 3270 chrome as the menus.
    ///
    /// Output is buffered in a thread-safe StringBuilder and the TextView
    /// is refreshed by a 60ms MainLoop timer, never directly from the
    /// background task - this avoids "Collection was modified" exceptions
    /// during Terminal.Gui's redraw pass.
    ///
    /// Color codes (Console.ForegroundColor) and Console.Clear() are
    /// intentionally ignored.
    /// </summary>
    internal static class TuiOutputWindow
    {
        public static async Task RunAsync(string title, Func<Task> action)
        {
            Exception? capturedError = null;
            var actionDoneSignal = new ManualResetEventSlim(false);

            var bufferLock = new object();
            var buffer = new StringBuilder();
            bool dirty = false;

            Application.Init();
            try
            {
                var top = Application.Top;
                top.ColorScheme = TuiTheme.Base();

                var header = BuildHeader(title);
                top.Add(header);

                var logFrame = new FrameView($" {title.ToUpperInvariant()} - OUTPUT ")
                {
                    X = 0, Y = 3,
                    Width = Dim.Fill(),
                    Height = Dim.Fill(4),
                    ColorScheme = TuiTheme.Base()
                };
                var log = new TextView
                {
                    X = 0, Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill(),
                    ReadOnly = true,
                    WordWrap = false,
                    CanFocus = false,                  // never grab focus - avoids the Focus scheme
                                                        // (Black on BrightGreen) flooding the whole pane
                    ColorScheme = TuiTheme.LogPane(),  // dedicated scheme with no green-fill Focus
                    Text = ""
                };
                logFrame.Add(log);
                top.Add(logFrame);

                var inputFrame = new FrameView(" INPUT (ENTER to submit) ")
                {
                    X = 0,
                    Y = Pos.Bottom(logFrame),
                    Width = Dim.Fill(),
                    Height = 3,
                    ColorScheme = TuiTheme.Header()
                };
                var inputField = new TextField("")
                {
                    X = 0, Y = 0,
                    Width = Dim.Fill(),
                    ColorScheme = TuiTheme.Button()
                };
                inputFrame.Add(inputField);
                top.Add(inputFrame);

                var writer = new BufferedTextWriter(buffer, bufferLock, () => dirty = true);
                var reader = new TuiTextReader();
                inputField.KeyPress += e =>
                {
                    if (e.KeyEvent.Key == Key.Enter)
                    {
                        var line = inputField.Text?.ToString() ?? string.Empty;
                        lock (bufferLock) { buffer.Append(line).Append('\n'); dirty = true; }
                        reader.Submit(line);
                        inputField.Text = "";
                        e.Handled = true;
                    }
                };

                var statusBar = new StatusBar(new StatusItem[]
                {
                    new StatusItem(Key.F10, "~F10~ CLOSE (when finished)", () =>
                    {
                        if (actionDoneSignal.IsSet) Application.RequestStop();
                    }),
                    new StatusItem(Key.Esc, "~ESC~ CLOSE (when finished)", () =>
                    {
                        if (actionDoneSignal.IsSet) Application.RequestStop();
                    }),
                })
                {
                    ColorScheme = TuiTheme.StatusBar()
                };
                top.Add(statusBar);

                var oldOut = Console.Out;
                var oldIn = Console.In;

                // Refresh timer: 60ms, copies the buffer into the TextView only
                // when dirty. Runs on the MainLoop thread - safe for redraw.
                object? refreshToken = Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(60), _ =>
                {
                    if (!dirty) return true;
                    string snapshot;
                    lock (bufferLock)
                    {
                        snapshot = buffer.ToString();
                        dirty = false;
                    }
                    log.Text = snapshot;
                    log.MoveEnd();
                    log.SetNeedsDisplay();
                    return true;
                });

                bool actionStarted = false;
                Action? iterationHandler = null;
                iterationHandler = () =>
                {
                    if (actionStarted) return;
                    actionStarted = true;

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
                            capturedError = ex;
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
                                buffer.Append('\n').Append("--- ACTION COMPLETE. PRESS F10 OR ESC TO CLOSE. ---").Append('\n');
                                dirty = true;
                            }
                            actionDoneSignal.Set();
                            reader.Cancel();
                        }
                    });
                };
                Application.Iteration += iterationHandler;

                try
                {
                    Application.Run();
                }
                finally
                {
                    if (refreshToken != null)
                        Application.MainLoop.RemoveTimeout(refreshToken);
                    if (iterationHandler != null)
                        Application.Iteration -= iterationHandler;
                    Console.SetOut(oldOut);
                    Console.SetIn(oldIn);
                }
            }
            finally
            {
                Application.Shutdown();
            }

            if (capturedError != null)
                Console.Error.WriteLine($"[output-window] action ended with error: {capturedError.Message}");
            await Task.CompletedTask;
        }

        private static View BuildHeader(string title)
        {
            var header = new View
            {
                X = 0, Y = 0,
                Width = Dim.Fill(),
                Height = 3,
                ColorScheme = TuiTheme.Header()
            };
            var titleLeft   = new Label("ALL-IN-1 CONVERTER") { X = 1, Y = 0 };
            var titleCenter = new Label(title.ToUpperInvariant()) { X = Pos.Center(), Y = 0 };
            var titleRight  = new Label($"DATE: {DateTime.UtcNow:yyyy-MM-dd}") { X = Pos.AnchorEnd(18), Y = 0 };
            var separator   = new Label(new string('=', 200)) { X = 0, Y = 2, Width = Dim.Fill() };
            header.Add(titleLeft, titleCenter, titleRight, separator);
            return header;
        }

        /// <summary>
        /// Writes go to a shared StringBuilder; the UI thread drains it on a timer.
        /// </summary>
        private sealed class BufferedTextWriter : TextWriter
        {
            private readonly StringBuilder _buf;
            private readonly object _lock;
            private readonly Action _markDirty;
            public override Encoding Encoding => Encoding.UTF8;

            public BufferedTextWriter(StringBuilder buf, object lockObj, Action markDirty)
            {
                _buf = buf; _lock = lockObj; _markDirty = markDirty;
            }

            public override void Write(char value)
            {
                lock (_lock) { _buf.Append(value); }
                _markDirty();
            }
            public override void Write(string? value)
            {
                if (string.IsNullOrEmpty(value)) return;
                lock (_lock) { _buf.Append(value); }
                _markDirty();
            }
            public override void WriteLine()
            {
                lock (_lock) { _buf.Append('\n'); }
                _markDirty();
            }
            public override void WriteLine(string? value)
            {
                lock (_lock) { _buf.Append(value ?? string.Empty).Append('\n'); }
                _markDirty();
            }
        }

        private sealed class TuiTextReader : TextReader
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
                try
                {
                    return _lines.Take();
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            }
        }
    }
}
