using FeedbackSorter.Application;
using FeedbackSorter.Infrastructure;
using FeedbackSorter.Presentation.Infrastructure;
using FeedbackSorter.Presentation.Middleware;

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

if (app.Environment.IsDevelopment())
{
    app.ApplyMigrations();
}


app.Run();

public partial class Program
{
}
