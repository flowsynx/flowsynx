namespace FlowSynx.Connectors.Database.MySql.Models;

public class Table
{
    public Table(string tableName) : this(tableName, string.Empty)
    {
    }

    public Table(string tableName, string aliasName)
    {
        Name = tableName;
        Alias = aliasName;
    }
    
    public string Name { get; }
    public string Alias { get; }

    public override string ToString()
    {
        return string.IsNullOrEmpty(Alias) 
            ? $"{Name.ToUpper()}" 
            : $"{Name.ToUpper()} AS {Alias.ToUpper()}";
    }
}
