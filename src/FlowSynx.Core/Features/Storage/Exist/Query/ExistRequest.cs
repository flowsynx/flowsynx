using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Core.Features.Storage.List.Query;

namespace FlowSynx.Core.Features.Storage.Exist.Query;

public class ExistRequest : IRequest<Result<ExistResponse>>
{
    public string Path { get; set; } = string.Empty;
}