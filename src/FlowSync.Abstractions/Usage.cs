namespace FlowSync.Abstractions;

public class Usage
{
    public long Total { get; set; }
    public long Free { get; set; }
    public long Used => Total - Free;
}