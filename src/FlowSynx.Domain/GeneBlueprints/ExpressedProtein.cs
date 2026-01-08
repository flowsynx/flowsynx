namespace FlowSynx.Domain.GeneBlueprints;

public record ExpressedProtein(
    string Type, // "assembly", "script", "container", "remote"
    string Location,
    string EntryPoint,
    string Runtime, // ".NET", "JavaScript", "Python", "Docker"
    string Version,
    Dictionary<string, object> Configuration);