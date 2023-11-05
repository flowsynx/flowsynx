namespace FlowSync.Abstractions;

public class Usage
{
    public long Total { get; set; }     // quota of bytes that can be used
    public long Used { get; set; }      // bytes in use
    public long Trashed { get; set; }   // bytes in trash
    public long Other { get; set; }     // other usage e.g. gmail in drive
    public long Free { get; set; }      // bytes which can be uploaded before reaching the quota
    public long Objects { get; set; }   // objects in the storage system
}