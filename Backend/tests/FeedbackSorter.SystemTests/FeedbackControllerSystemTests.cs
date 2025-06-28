using System.Net;
using System.Net.Http.Json;
using FeedbackSorter.Presentation.UserFeedback;


namespace FeedbackSorter.SystemTests;

public class FeedbackControllerSystemTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public FeedbackControllerSystemTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _factory.ResetMocks();
    }

    [Fact]
    public async Task SubmitFeedback_ValidInput_ReturnsAccepted()
    {
        // Arrange
        var inputDto = new UserFeedbackInputDto("This is a test feedback.");

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/feedback", inputDto);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        FeedbackSubmissionAcknowledgementDto? acknowledgement = await response.Content.ReadFromJsonAsync<FeedbackSubmissionAcknowledgementDto>();
        Assert.NotNull(acknowledgement);
        Assert.Equal("Feedback received and queued for analysis.", acknowledgement.Message);
    }


}
