namespace FlowSync.Models;

public class CustomHeadersToAddAndRemove
{
    public Dictionary<string, string> HeadersToAdd = new();
    public HashSet<string> HeadersToRemove = new();
}