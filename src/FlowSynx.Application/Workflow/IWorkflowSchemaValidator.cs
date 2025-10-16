namespace FlowSynx.Application.Workflow;

public interface IWorkflowSchemaValidator
{
    Task ValidateAsync(string? schemaUrl, string definitionJson, CancellationToken cancellationToken);
}
