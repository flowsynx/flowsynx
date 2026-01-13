namespace FlowSynx.BuildingBlocks.Errors;

public readonly record struct ErrorCode(int Value, ErrorCategory Category)
{
    public static string Prefix => "FSX";
    public override string ToString() => $"{Prefix}{Value:D6} ({Category})";
}