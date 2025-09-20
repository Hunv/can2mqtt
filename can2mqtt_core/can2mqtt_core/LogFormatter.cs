using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

public sealed class Can2MqttLogFormatter : ConsoleFormatter, IDisposable
{
    private readonly IDisposable _optionsReloadToken;
    private ConsoleFormatterOptions _formatterOptions;

    public Can2MqttLogFormatter(
        IOptionsMonitor<ConsoleFormatterOptions> options)
        : base(nameof(Can2MqttLogFormatter))
    {
        _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
        _formatterOptions = options.CurrentValue;
    }

    private void ReloadLoggerOptions(ConsoleFormatterOptions options) => _formatterOptions = options;

    public void Dispose() => _optionsReloadToken?.Dispose();

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
    {
        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var now = _formatterOptions.UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now;
        textWriter.WriteLine($"{now:yyyy-MM-dd HH:mm:ss.fff} {logEntry.Category} [{logEntry.LogLevel}]: {message}");
    }
}
