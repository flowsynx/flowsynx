namespace FlowSync.Core.FileSystem.Parers.Date;

internal interface IDateParser
{
    DateTime Parse(string dateTime);
}