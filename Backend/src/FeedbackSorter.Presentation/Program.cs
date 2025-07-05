using System.Text.Json.Serialization;
using FeedbackSorter.Application;
using FeedbackSorter.Application.Notifications;
using FeedbackSorter.Infrastructure;
using FeedbackSorter.Presentation.Infrastructure;
using FeedbackSorter.Presentation.Middleware;
using FeedbackSorter.Presentation.Hubs;
using FeedbackSorter.Presentation.Notifications;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add application and infrastructure services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddSignalR();
builder.Services.AddScoped<IFeedbackHubContext, FeedbackHubContextWrapper>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        policyBuilder =>
        {
            policyBuilder.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowLocalhost");

app.UseMiddleware<DomainValidationExceptionMiddleware>();

app.MapControllers();
app.MapHub<FeedbackHub>("/feedbackHub");

if (app.Environment.IsDevelopment())
{
    app.ApplyMigrations();
}


app.Run();

public partial class Program
{
}
