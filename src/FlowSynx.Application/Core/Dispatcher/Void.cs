namespace FlowSynx.Application.Core.Dispatcher;

public readonly struct Void : IEquatable<Void>
{
    public static readonly Void Value = new();
    public bool Equals(Void other) => true;
    public override bool Equals(object? obj) => obj is Void;
    public override int GetHashCode() => 0;
}