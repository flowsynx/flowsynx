namespace FlowSynx.Domain.Primitives;

public interface IResult
{
    List<string> Messages { get; set; }

    bool Succeeded { get; set; }

    DateTime GeneratedAtUtc { get; }
}

public interface IResult<out T> : IResult
{
    T Data { get; }
}