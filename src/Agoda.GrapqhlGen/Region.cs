namespace Agoda.GrapqhlGen;

public record Region(string Name, List<string> Lines)
{
    public string GetSourceCode() =>
        string.Join(Environment.NewLine, Lines.Where(l => !string.IsNullOrWhiteSpace(l)));
}
