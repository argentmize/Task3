using System.Text.RegularExpressions;

Console.WriteLine("Введите путь к папке с логами:");

var pathToLogsFolder = Console.ReadLine();

if (!Directory.Exists(pathToLogsFolder))
    Console.WriteLine("Указанная папка не существует");
else
{
    var outputFolder = Path.Combine(pathToLogsFolder, "OutputLogs");
    if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);
    var normalized = Path.Combine(outputFolder, "normalized.txt");
    var problems = Path.Combine(outputFolder, "problems.txt");
    var files = Directory.GetFiles(pathToLogsFolder);
    foreach (var file in files)
    {
        Console.WriteLine($"Обработка файла {file} ...");
        var lines = File.ReadLinesAsync(file).ToBlockingEnumerable();
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line)) continue;
            LogEntry entry = Parser.Parse(line);
            var newLine = entry.IsValid ? entry.ToString() : entry.Message;
            var _ = entry.IsValid ? normalized : problems;
            await LogWriter.WriteLog(_, newLine);
            Console.WriteLine($"Запись добавлена в {_}");
        }
        Console.WriteLine();
    }
}

static class LogWriter
{
    public static async Task WriteLog(string file, string line)
    {
        using StreamWriter streamWriter = new(file, true);
        await streamWriter.WriteLineAsync(line);
    }
}
static class Parser
{
    static readonly Regex pattern1 = new(@"^(?<date>\d{2}\.\d{2}\.\d{4}) (?<time>\d{2}:\d{2}:\d{2}\.\d{3}) (?<level>INFORMATION|WARNING|ERROR|DEBUG) +(?<message>.+)$");
    static readonly Regex pattern2 = new(@"^(?<date>\d{4}-\d{2}-\d{2}) (?<time>\d{2}:\d{2}:\d{2}\.\d{4})\| (?<level>INFO|WARN|ERROR|DEBUG)\|.*\|(?<method>.+?)\| (?<message>.+)$");
    public static LogEntry Parse(string line)
    {
        LogEntry result = new();
        var match1 = pattern1.Match(line);
        if (match1.Success)
        {
            result.Date = DateTime.Parse(match1.Groups["date"].Value).ToString("dd-MM-yyyy");
            result.Time = match1.Groups["time"].Value;
            result.LogLevel = match1.Groups["level"].Value.NormalizeLogLevel();
            result.Message = match1.Groups["message"].Value;
            result.IsValid = true;
            return result;
        }
        var match2 = pattern2.Match(line);
        if (match2.Success)
        {
            result.Date = DateTime.Parse(match2.Groups["date"].Value).ToString("dd-MM-yyyy");
            result.Time = match2.Groups["time"].Value;
            result.LogLevel = match2.Groups["level"].Value.NormalizeLogLevel();
            result.CallerMethod = match2.Groups["method"].Value;
            result.Message = match2.Groups["message"].Value;
            result.IsValid = true;
            return result;
        }
        result.Message = line;
        result.IsValid = false;
        return result;
    }
}
record LogEntry
{
    public string Date { get; set; }
    public string Time { get; set; }
    public string LogLevel { get; set; }
    public string CallerMethod { get; set; } = "DEFAULT";
    public string Message { get; set; }
    public bool IsValid { get; set; } = true;
    public override string ToString() => $"{Date}\t{Time}\t{LogLevel}\t{CallerMethod}\t{Message}";
}
static class LogEntryExt
{
    public static string NormalizeLogLevel(this string value) => value.ToUpper() switch
    {
        "INFORMATION" or "INFO" => "INFO",
        "WARNING" or "WARN" => "WARN",
        "ERROR" => "ERROR",
        "DEBUG" => "DEBUG",
        _ => "UNKNOWN"
    };
}