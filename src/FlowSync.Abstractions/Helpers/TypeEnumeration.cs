using System.Reflection;

namespace FlowSync.Abstractions.Helpers;

public abstract class TypeEnumeration : IComparable
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }

    protected TypeEnumeration(Guid id, string name, string description) => (Id, Name, Description) = (id, name, description);

    public override string ToString() => Name;

    public static IEnumerable<T> GetAll<T>() where T : TypeEnumeration =>
        typeof(T).GetFields(BindingFlags.Public |
                            BindingFlags.Static |
                            BindingFlags.DeclaredOnly)
            .Select(f => f.GetValue(null))
            .Cast<T>();

    public override bool Equals(object obj)
    {
        var otherValue = obj as TypeEnumeration;
        if (otherValue == null)
        {
            return false;
        }
        var typeMatches = GetType() == obj.GetType();
        var valueMatches = Id.Equals(otherValue.Id);
        return typeMatches && valueMatches;
    }

    public int CompareTo(object other)
    {
        return Id.CompareTo(((TypeEnumeration)other).Id);
    }
}
