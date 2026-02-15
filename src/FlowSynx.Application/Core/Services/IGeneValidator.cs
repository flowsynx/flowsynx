//using FlowSynx.BuildingBlocks.Results;
//using FlowSynx.Domain.Chromosomes;
//using FlowSynx.Domain.GeneBlueprints;
//using FlowSynx.Domain.GeneInstances;
//using FlowSynx.Domain.Genomes;

//namespace FlowSynx.Application.Core.Services;

//public interface IActivityValidator
//{
//    Task<ValidationResult> ValidateActivityInstanceAsync(
//        ActivityInstance instance, 
//        ActivityBlueprint blueprint, 
//        CancellationToken cancellationToken);

//    Task<ValidationResult> ValidateWorkflowAsync(
//        Workflow workflow, 
//        CancellationToken cancellationToken);

//    Task<ValidationResult> ValidateWorkflowApplicationAsync(
//        WorkflowApplication workflowApplication, 
//        CancellationToken cancellationToken);
//}