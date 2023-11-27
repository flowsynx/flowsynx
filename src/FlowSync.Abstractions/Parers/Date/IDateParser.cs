namespace FlowSync.Abstractions.Parers.Date;

public interface IDateParser
{
    DateTime Parse(string dateTime);
}