using System.Text.RegularExpressions;

namespace FlowSynx.Connectors.Database.MySql.Models;

public class Snippet
{

    private string _name;

    public string Code { get; set; }

    public string Prefix { get; set; }

    public string Postfix { get; set; }

    public string Name
    {
        get => _name;
        set
        {
            if (!IsValid(value))
                throw new Exception($"{value} {Name}");

            _name = value;
        }
    }

    public Snippet(string name, string code, string prefix = "", string postfix = "")
    {
        this.Name = name;
        this.Code = code;
        this.Prefix = prefix;
        this.Postfix = postfix;
    }

    private bool IsValid(string name)
    {
        return Regex.IsMatch(name, "^([A-Za-z0-9_]+)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    }

}