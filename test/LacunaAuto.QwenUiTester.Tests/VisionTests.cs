using System.Net;
using System.Text.Json.Nodes;

namespace LacunaAuto.QwenUiTester.Tests;

public class VisionTests
{
    [Fact]
    public async Task SmokeSendsTwoPngsAfterAllToolResponses()
    {
        using var agent = new StrataAgent(Options(), handler: new StubHandler(request =>
        {
            var messages = request["messages"]!.AsArray();
            Assert.Equal(new[] { "system", "user", "assistant", "tool", "tool", "user" },
                messages.Select(message => message!["role"]!.GetValue<string>()));
            var calls = messages[2]!["tool_calls"]!.AsArray();
            var parts = messages[5]!["content"]!.AsArray();
            Assert.Equal(2, calls.Count);
            Assert.Equal(4, parts.Count);
            for (var index = 0; index < calls.Count; index++)
            {
                Assert.Equal(calls[index]!["id"]!.ToString(), messages[index + 3]!["tool_call_id"]!.ToString());
                Assert.DoesNotContain("base64", messages[index + 3]!["content"]!.ToString());
                Assert.Contains(index == 0 ? "mobile" : "tablet", parts[index * 2]!["text"]!.ToString());
                var url = parts[index * 2 + 1]!["image_url"]!["url"]!.ToString();
                Assert.StartsWith("data:image/png;base64,", url);
                var png = Convert.FromBase64String(url.Split(',')[1]);
                Assert.Equal("89504E470D0A1A0A", Convert.ToHexString(png.AsSpan(0, 8)));
            }
            Assert.NotEqual(parts[1]!.ToJsonString(), parts[3]!.ToJsonString());
            Assert.DoesNotContain("274 839", request.ToJsonString());
            return Completion("274 839");
        }));

        Assert.True((await agent.CheckVisionAsync()).Available);
    }

    [Theory]
    [InlineData(400, "Vision disabled; data:image/png;base64,DO_NOT_EXPOSE", "vision_unavailable")]
    [InlineData(400, "This server was started without the vision encoder", "vision_unavailable")]
    [InlineData(503, "unavailable", "endpoint_unavailable")]
    [InlineData(401, "bad credentials", "model_error")]
    [InlineData(200, "wrong answer", "vision_unverified")]
    [InlineData(200, "", "model_error")]
    public async Task SmokeClassifiesFailuresWithoutEchoingResponse(int status, string body, string expected)
    {
        using var agent = new StrataAgent(Options(), handler: new StubHandler(_ =>
            status == 200 ? Completion(body) : new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(body) }));

        var result = await agent.CheckVisionAsync();
        Assert.Equal(expected, result.Status);
        Assert.False(result.Available);
        Assert.DoesNotContain("DO_NOT_EXPOSE", result.Detail);
    }

    [Fact]
    public void MissingResponsiveEvidenceBlocksAnAffectedRoute()
    {
        var changedPaths = new[] { "src/LacunaAuto.Web/LacunaAuto.Web.Client/Pages/Listings.razor.css" };
        var review = new VisualReview(".", "http://127.0.0.1:1/", changedPaths);

        Assert.Equal(new[] { "/listings" }, review.RelevantRoutes);
        Assert.Null(review.CoveredRoute);
        Assert.NotNull(review.MissingEvidenceReason());
    }

    private static RunnerOptions Options() => RunnerOptions.Parse(["--strata-url", "http://127.0.0.1:1/v1/chat/completions"]);

    private static HttpResponseMessage Completion(string answer) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(
            new { choices = new[] { new { message = new { content = answer } } } })),
    };

    private sealed class StubHandler(Func<JsonObject, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var requestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return respond(JsonNode.Parse(requestBody)!.AsObject());
        }
    }
}
