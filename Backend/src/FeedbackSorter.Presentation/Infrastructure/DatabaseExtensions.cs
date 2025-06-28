using FeedbackSorter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FeedbackSorter.Presentation.Infrastructure;

public static class DatabaseExtensions
{
    public static void ApplyMigrations(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        
        IServiceProvider services = scope.ServiceProvider;
        try
        {
            var dbContext = services.GetRequiredService<FeedbackSorterDbContext>();
            dbContext.Database.Migrate();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating the database.");
        }
    }
}
