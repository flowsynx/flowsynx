namespace FlowSynx.Core.Parers.Specifications;

public interface ISpecificationsParser
{
    SpecificationsResult Parse(string type, Dictionary<string, object?>? specifications);
}