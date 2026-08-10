using System.Collections.Concurrent;
using System.Text;

namespace Habbo_Downloader.App.Operations;

public sealed class OperationRunner : IAsyncDisposable
{
    private static readonly SemaphoreSlim ConsoleLease = new(1, 1);
    private readonly InputReader _input = new();
    private bool _disposed;

    public event Action<string>? OutputReceived;

    public bool IsRunning { get; private set; }

    public async Task<OperationResult> RunAsync(OperationDefinition operation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(operation);

        await ConsoleLease.WaitAsync();
        DateTimeOffset startedAt = DateTimeOffset.Now;
        TextWriter originalOut = Console.Out;
        TextWriter originalError = Console.Error;
        TextReader originalIn = Console.In;
        var writer = new EventWriter(text => OutputReceived?.Invoke(text));
        IsRunning = true;

        try
        {
            Console.SetOut(writer);
            Console.SetError(writer);
            Console.SetIn(_input);
            await Task.Run(operation.Action);
            return new OperationResult(true, startedAt, DateTimeOffset.Now);
        }
        catch (Exception exception)
        {
            OutputReceived?.Invoke($"{Environment.NewLine}Error: {exception.Message}{Environment.NewLine}");
            return new OperationResult(false, startedAt, DateTimeOffset.Now, exception);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            Console.SetIn(originalIn);
            IsRunning = false;
            ConsoleLease.Release();
        }
    }

    public void SubmitInput(string value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _input.Submit(value);
        OutputReceived?.Invoke($"{value}{Environment.NewLine}");
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;
        _input.Dispose();
        return ValueTask.CompletedTask;
    }

    private sealed class EventWriter(Action<string> write) : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
        public override void Write(char value) => write(value.ToString());
        public override void Write(string? value)
        {
            if (!string.IsNullOrEmpty(value)) write(value);
        }
        public override void WriteLine() => write(Environment.NewLine);
        public override void WriteLine(string? value) => write((value ?? string.Empty) + Environment.NewLine);
    }

    private sealed class InputReader : TextReader, IDisposable
    {
        private readonly BlockingCollection<string> _lines = new();

        public void Submit(string value)
        {
            if (!_lines.IsAddingCompleted) _lines.Add(value);
        }

        public override string? ReadLine()
        {
            try { return _lines.Take(); }
            catch (InvalidOperationException) { return null; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _lines.CompleteAdding();
                _lines.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
