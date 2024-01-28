using System.Text.Json.Serialization;

public class PhrasalTemplate
{
    [JsonInclude]
    public string[] blanks = new string[0];

    [JsonInclude]
    public string[] templateLines = new string[0];

    public IEnumerable<string> BuildLines(string[] words)
        => templateLines.Select(templateLine => string.Format(templateLine, words));
}