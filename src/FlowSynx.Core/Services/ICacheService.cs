namespace FlowSynx.Core.Services;

public interface ICacheService<in TKey, TValue>
{
    TValue? Get(TKey key);
    void Set(TKey key, TValue value);
    void Delete(TKey key);
    int Count();
}