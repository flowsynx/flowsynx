namespace FlowSynx.Connectors.Database.MySql.Models;

public static class TemplateLibrary
{

    public static Template Select
    {
        get
        {
            var sql = "{{START}}SELECT {{FIELDS}} FROM {{TABLE}}{{JOINS}}{{FILTERS}}{{GROUPBY}}{{ORDERBY}}{{END}}";
            return new Template(sql);
        }
    }
}