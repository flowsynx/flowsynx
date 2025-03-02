using System.Collections.Concurrent;

namespace FlowSynx.Infrastructure.Logging;

public class LogQueue: ConcurrentQueue<LogMessage>
{

}
