using System.Collections.Concurrent;
using System.Text;

namespace MailNotify.Logging;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logsDirectory;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();

    public FileLoggerProvider(string logsDirectory)
    {
        _logsDirectory = logsDirectory 
            ?? Path.Combine(AppContext.BaseDirectory, "logs");
        try
        {
            Directory.CreateDirectory(_logsDirectory);
        }
        catch {}
    }

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _logsDirectory));

    public void Dispose()
    {
        _loggers.Clear();
    }

    private sealed class FileLogger(string categoryName, string logsDirectory) : ILogger
    {
        private readonly string _categoryName = categoryName;
        private readonly string _logsDirectory = logsDirectory;
        private readonly Lock _sync = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => 
            NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => 
            logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (formatter == null)
                return;

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception == null) 
                return;

            var logRecord = new StringBuilder();
            logRecord.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            logRecord.AppendLine(logLevel.ToString());
            logRecord.AppendLine(_categoryName);
            logRecord.AppendLine(message);
            if (exception != null)
                logRecord.AppendLine(exception.ToString());

            try
            {
                var fileName = Path.Combine(_logsDirectory, $"log-{DateTime.Now:yyyy-MM-dd}.txt");
                lock (_sync)
                    File.AppendAllText(fileName, logRecord.ToString());
            }
            catch {}
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new NullScope();
        public void Dispose() { }
    }
}
