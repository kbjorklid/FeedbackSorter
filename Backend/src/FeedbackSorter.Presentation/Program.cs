using FeedbackSorter.Application;
using FeedbackSorter.Infrastructure;
using FeedbackSorter.Infrastructure.Persistence;
using FeedbackSorter.Presentation.Middleware;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add application and infrastructure services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<DomainValidationExceptionMiddleware>();

app.MapControllers();

using (IServiceScope scope = app.Services.CreateScope())
{
    IServiceProvider services = scope.ServiceProvider;
    try
    {
        FeedbackSorterDbContext dbContext = services.GetRequiredService<FeedbackSorterDbContext>();
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        // Log the error or handle it as appropriate for your application
        ILogger<Program> logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
        // Depending on the severity, you might want to stop the application
    }
}


app.Run();

public partial class Program
{
}
