using FeedbackSorter.Application.Feedback.Analysis;
using FeedbackSorter.Application.Feedback.Queries.GetAnalyzedFeedbacks;
using FeedbackSorter.Application.Feedback.Query;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.Infrastructure.Persistence;
using FeedbackSorter.SharedKernel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FeedbackSorter.SystemTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public ILlmFeedbackAnalyzer LLMFeedbackAnalyzerMock { get; private set; } = null!;
    public ITimeProvider TimeProviderMock { get; private set; } = null!;
    public ILogger<AnalyzeFeedbackUseCase> AnalyzeFeedbackCommandHandlerLoggerMock { get; private set; } = null!;
    public ILogger<MarkFeedbackAnalyzedUseCase> MarkFeedbackAnalyzedCommandHandlerLoggerMock { get; private set; } = null!;
    public ILogger<MarkFeedbackAnalysisFailedUseCase> MarkFeedbackAnalysisFailedCommandHandlerLoggerMock { get; private set; } = null!;
    public ILogger<QueryAnalyzedFeedbacksUseCase> GetAnalyzedFeedbacksQueryHandlerLoggerMock { get; private set; } = null!;

    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            ServiceDescriptor? dbContextOptionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<FeedbackSorterDbContext>));

            if (dbContextOptionsDescriptor != null)
            {
                services.Remove(dbContextOptionsDescriptor);
            }

            // Add the DbContext service to use the in-memory SQLite database
            services.AddDbContext<FeedbackSorterDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // --- The rest of your mock setup remains the same ---
            var infrastructureServiceDescriptors = services.Where(
                descriptor =>
                              descriptor.ServiceType == typeof(ILlmFeedbackAnalyzer) ||
                              descriptor.ServiceType == typeof(ITimeProvider) ||
                              descriptor.ServiceType == typeof(ILogger<AnalyzeFeedbackUseCase>) ||
                              descriptor.ServiceType == typeof(ILogger<MarkFeedbackAnalyzedUseCase>) ||
                              descriptor.ServiceType == typeof(ILogger<MarkFeedbackAnalysisFailedUseCase>) ||
                              descriptor.ServiceType == typeof(ILogger<QueryAnalyzedFeedbacksUseCase>))
                .ToList();

            foreach (ServiceDescriptor? descriptor in infrastructureServiceDescriptors)
            {
                services.Remove(descriptor);
            }

            LLMFeedbackAnalyzerMock = Substitute.For<ILlmFeedbackAnalyzer>();
            TimeProviderMock = Substitute.For<ITimeProvider>();
            AnalyzeFeedbackCommandHandlerLoggerMock = Substitute.For<ILogger<AnalyzeFeedbackUseCase>>();
            MarkFeedbackAnalyzedCommandHandlerLoggerMock = Substitute.For<ILogger<MarkFeedbackAnalyzedUseCase>>();
            MarkFeedbackAnalysisFailedCommandHandlerLoggerMock = Substitute.For<ILogger<MarkFeedbackAnalysisFailedUseCase>>();
            GetAnalyzedFeedbacksQueryHandlerLoggerMock = Substitute.For<ILogger<QueryAnalyzedFeedbacksUseCase>>();

            services.AddSingleton(LLMFeedbackAnalyzerMock);
            services.AddSingleton(TimeProviderMock);
            services.AddSingleton(AnalyzeFeedbackCommandHandlerLoggerMock);
            services.AddSingleton(MarkFeedbackAnalyzedCommandHandlerLoggerMock);
            services.AddSingleton(MarkFeedbackAnalysisFailedCommandHandlerLoggerMock);
            services.AddSingleton(GetAnalyzedFeedbacksQueryHandlerLoggerMock);

            // Create the database schema in the in-memory database
            using IServiceScope scope = services.BuildServiceProvider().CreateScope();
            FeedbackSorterDbContext dbContext = scope.ServiceProvider.GetRequiredService<FeedbackSorterDbContext>();
        });
    }

    public void ResetMocks()
    {
        LLMFeedbackAnalyzerMock.ClearReceivedCalls();
        TimeProviderMock.ClearReceivedCalls();
        TimeProviderMock.UtcNow.Returns(new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc));
        AnalyzeFeedbackCommandHandlerLoggerMock.ClearReceivedCalls();
        MarkFeedbackAnalyzedCommandHandlerLoggerMock.ClearReceivedCalls();
        MarkFeedbackAnalysisFailedCommandHandlerLoggerMock.ClearReceivedCalls();
        GetAnalyzedFeedbacksQueryHandlerLoggerMock.ClearReceivedCalls();
        ClearDatabase();
    }

    private void ClearDatabase()
    {
        using (IServiceScope scope = Services.CreateScope())
        {
            IServiceProvider services = scope.ServiceProvider;
            FeedbackSorterDbContext dbContext = services.GetRequiredService<FeedbackSorterDbContext>();

            // This hacky retry logic is here because the 'EnsureDeleted' call may fail due to background processing
            // (see AnalyzeFeedbackCommandHandler). The background processing itself is hacky, and not production ready.
            try
            {
                dbContext.Database.EnsureDeleted();
            }
            catch (Exception)
            {
                Thread.Sleep(500);
                dbContext.Database.EnsureDeleted();
            }

            dbContext.Database.EnsureCreated();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Close and dispose of the connection when the factory is disposed.
            // This will destroy the in-memory database.
            _connection?.Close();
            _connection?.Dispose();
        }
        base.Dispose(disposing);
    }
}
