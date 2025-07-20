namespace FlowSynx.Infrastructure.Workflow.ResultStorageProviders;

public interface IResultStorageFactory
{
    IResultStorageProvider GetDefaultProvider();
}