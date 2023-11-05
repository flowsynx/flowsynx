namespace FlowSync.Core.FileSystem.Parse.Date;

internal interface IDateParser
{
    DateTime Parse(string dateTime);
}