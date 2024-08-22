using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Exist.Query;

public class ExistRequest : IRequest<Result<ExistResponse>>
{
    public string Path { get; set; } = string.Empty;
}