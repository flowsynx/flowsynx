using FlowSynx.Application.Models;
using FlowSynx.Persistence.SQLite.Contexts;
using FlowSynx.PluginCore.Exceptions;

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
            var errorMessage = new ErrorMessage((int)ErrorCode.LoggerCreation, "Error occurred while creating the logger: " + ex.Message);
            Console.WriteLine(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }
}