using FlowSynx.Persistence.SQLite.Contexts;

namespace FlowSynx.Extensions;

public static class ServiceProviderExtensions
{
    public static void EnsureLogDatabaseCreated(this IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<LoggerContext>();

        try
        {
            var result = context.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error occurred while creating the log database: " + ex.Message);
            if (ex.Message.Contains("Cannot create log database"))
            {
                Console.WriteLine("Failed to create log database due to other reasons.");
            }
            throw;
        }
    }
}