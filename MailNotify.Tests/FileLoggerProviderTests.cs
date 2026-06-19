using System.Text.Json;
using MailNotify.Logging;
using Microsoft.Extensions.Logging;

namespace MailNotify.Tests;

public class FileLoggerProviderTests
{
    [Fact]
    public void CreateLogger_ReusesLoggerForSameCategory()
    {
        using var provider = new FileLoggerProvider(CreateTempDirectory());

        provider.CreateLogger("MailNotify").Should().BeSameAs(provider.CreateLogger("MailNotify"));
    }

    [Fact]
    public void Log_WritesJsonRecordToDailyLogFile()
    {
        var directory = CreateTempDirectory();
        using var provider = new FileLoggerProvider(directory);
        var logger = provider.CreateLogger("MailNotify.Tests");

        logger.LogInformation(new EventId(7), "Started {Worker}", "NotifyWorker");

        var logFile = Path.Combine(directory, $"log-{DateTime.Now:dd-MM-yyyy}.log");
        var line = File.ReadLines(logFile).Single();
        using var json = JsonDocument.Parse(line);
        var root = json.RootElement;

        root.GetProperty("Level").GetString().Should().Be("Information");
        root.GetProperty("Category").GetString().Should().Be("MailNotify.Tests");
        root.GetProperty("EventId").GetInt32().Should().Be(7);
        root.GetProperty("Message").GetString().Should().Be("Started NotifyWorker");
        root.GetProperty("Exception").ValueKind.Should().Be(JsonValueKind.Null);
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "MailNotify.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
