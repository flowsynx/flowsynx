using System.Collections.Immutable;

namespace FlowSynx.Application.Localizations;

public class Language
{
    public string Code { get; }
    public string Name { get; }

    private Language(string code, string name)
    {
        Code = code;
        Name = name;
    }

    public static readonly Language English = new("en", "English");

    public static readonly ImmutableArray<Language> All =
        ImmutableArray.Create(English);

    public static bool IsValid(string code) =>
        All.Any(lang => string.Equals(lang.Code, code, StringComparison.OrdinalIgnoreCase));

    public static Language? GetByCode(string code) =>
        All.FirstOrDefault(lang => string.Equals(lang.Code, code, StringComparison.OrdinalIgnoreCase));
}