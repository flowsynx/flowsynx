namespace FlowSync.Core.Parers.Date;

public interface IDateParser : IParser
{
    DateTime Parse(string dateTime);
}