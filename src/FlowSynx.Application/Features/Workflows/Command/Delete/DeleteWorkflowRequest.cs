﻿using MediatR;
using FlowSynx.Application.Wrapper;

namespace FlowSynx.Application.Features.Workflows.Command.Delete;

public class DeleteWorkflowRequest : IRequest<Result<Unit>>
{
    public required string Id { get; set; }
}