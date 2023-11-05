using FlowSyncConsole;

using IFileSystem fs = new PhysicalFileSystem("D:");
var list = fs.GetEntities(new FileSystemPath());

foreach (var item in list)
{
    Console.WriteLine(item.Path);
}