using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace LacunaAuto.QwenUiTester;

internal sealed record VisionCheck(string Status, string Detail)
{
    public bool Available => Status == "available";
}

internal sealed class StrataRequestException(string status, string message) : Exception(message)
{
    public string Status { get; } = status;
}

internal sealed class StrataAgent : IDisposable
{
    private const string SmokeToken = "LACUNA_QWEN_SMOKE";
    private static readonly Regex _imageDataUrl = new(
        @"(?i:data:image/[^;,\s]+;base64,)[A-Za-z0-9+/_=-]+|iVBORw0KGgo[A-Za-z0-9+/=]+");

    private readonly RunnerOptions _options;
    private readonly ToolRegistry? _tools;
    private readonly HttpClient _httpClient;

    public StrataAgent(RunnerOptions options, ToolRegistry? tools = null, HttpMessageHandler? handler = null)
    {
        _options = options;
        _tools = tools;
        _httpClient = new HttpClient(handler ?? new HttpClientHandler
        {
            AllowAutoRedirect = !(options.UiRelevant || options.StrataVisionSmoke),
        });
        _httpClient.Timeout = TimeSpan.FromMinutes(30);

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", options.ApiKey);
        }
    }

    public async Task<string> RunAsync(string systemPrompt, string userPrompt)
    {
        if (_tools is null)
            throw new InvalidOperationException("The UI review agent requires a tool registry.");

        var messages = new List<JsonObject>
        {
            Message("system", systemPrompt),
            Message("user", userPrompt),
        };
        var pendingCaptures = new List<VisualCapture>();
        var toolDefinitions = _tools.GetDefinitions();

        for (var turn = 1; turn <= _options.MaxToolTurns; turn++)
        {
            Console.WriteLine($"Qwen turn   : {turn}/{_options.MaxToolTurns} - sending request...");

            var response = await SendAsync(
                messages,
                toolDefinitions,
                JsonValue.Create("auto"));
            MarkDelivered(pendingCaptures);

            Console.WriteLine($"Qwen turn   : {turn}/{_options.MaxToolTurns} - response received.");

            var responseMessage = response.Message;
            var assistantContent = GetStringOrNull(responseMessage["content"]);
            var toolCalls = responseMessage["tool_calls"]?.AsArray();

            if (toolCalls is not { Count: > 0 })
            {
                if (string.IsNullOrWhiteSpace(assistantContent))
                {
                    Console.WriteLine("Qwen        : empty final message; requesting concise recovery response.");

                    var recoveryMessages = messages
                        .Select(message => (JsonObject)message.DeepClone())
                        .ToList();

                    recoveryMessages.Add(Message(
                        "user",
                        "Return the final review now. Do not call tools. Do not include hidden reasoning. " +
                        "Use the required Result: PASS/FAIL/BLOCKED first line and required sections only."));

                    var recovery = await SendAsync(
                        recoveryMessages,
                        _tools.GetDefinitions(),
                        JsonValue.Create("none"),
                        reasoningEffort: "low",
                        maxTokens: Math.Min(_options.MaxTokens, 8000));

                    var recoveredContent = GetStringOrNull(recovery.Message["content"]);
                    if (string.IsNullOrWhiteSpace(recoveredContent))
                    {
                        throw new InvalidOperationException(
                            $"STRATA returned an empty assistant message and recovery also returned no content. " +
                            $"Original finish_reason={response.FinishReason ?? "<null>"}, " +
                            $"recovery finish_reason={recovery.FinishReason ?? "<null>"}.");
                    }

                    Console.WriteLine("Qwen        : final recovery response received.");
                    return await EnsureStructuredFinalAsync(
                        recoveryMessages,
                        recoveredContent,
                        "empty-message recovery");
                }

                Console.WriteLine("Qwen        : final response received.");
                return await EnsureStructuredFinalAsync(
                    messages,
                    assistantContent,
                    "normal final response");
            }

            messages.Add(CloneAssistantMessage(responseMessage));

            foreach (var toolCallNode in toolCalls)
            {
                var toolCall = toolCallNode?.AsObject()
                    ?? throw new InvalidOperationException("STRATA returned an invalid tool call.");

                var toolCallId = GetStringOrNull(toolCall["id"])
                    ?? throw new InvalidOperationException("STRATA tool call is missing id.");

                var function = toolCall["function"]?.AsObject()
                    ?? throw new InvalidOperationException("STRATA tool call is missing function.");

                var functionName = GetStringOrNull(function["name"])
                    ?? throw new InvalidOperationException("STRATA tool call is missing function name.");

                var functionArguments = GetArguments(function["arguments"]);
                Console.WriteLine($"Qwen tool   : {(functionName.Length <= 40 ? functionName : "<invalid>")} started.");

                var toolResult = await _tools.ExecuteAsync(functionName, functionArguments);

                messages.Add(ToolMessage(toolCallId, functionName, _imageDataUrl.Replace(toolResult.Text, "[image data removed]")));
                if (toolResult.Capture is not null)
                    pendingCaptures.Add(toolResult.Capture);
            }

            if (pendingCaptures.Count > 0)
                messages.Add(ImageMessage(pendingCaptures));
        }

        Console.WriteLine(
            $"Qwen        : tool-turn budget {_options.MaxToolTurns} exhausted; requesting final report without tools.");

        var finalMessages = messages
            .Select(message => (JsonObject)message.DeepClone())
            .ToList();

        finalMessages.Add(Message(
            "user",
            "The tool budget is exhausted. Stop investigating and return the final review now using only the evidence already collected. " +
            "Do not call tools. Do not include hidden reasoning. " +
            "The first non-empty line must be Result: PASS, Result: FAIL, or Result: BLOCKED, followed by the required sections."));

        var finalResponse = await SendAsync(
            finalMessages,
            toolDefinitions,
            JsonValue.Create("none"),
            reasoningEffort: "low",
            maxTokens: Math.Min(_options.MaxTokens, 8000));
        MarkDelivered(pendingCaptures);

        var finalContent = GetStringOrNull(finalResponse.Message["content"]);
        if (string.IsNullOrWhiteSpace(finalContent))
        {
            throw new InvalidOperationException(
                $"STRATA exhausted {_options.MaxToolTurns} tool turns and returned no final content; " +
                $"final finish_reason={finalResponse.FinishReason ?? "<null>"}.");
        }

        Console.WriteLine("Qwen        : final budget-recovery response received.");
        return await EnsureStructuredFinalAsync(
            finalMessages,
            finalContent,
            "tool-budget recovery");
    }

    private void MarkDelivered(List<VisualCapture> captures)
    {
        foreach (var capture in captures)
            _tools!.MarkDelivered(capture);

        captures.Clear();
    }

    private async Task<string> EnsureStructuredFinalAsync(
        IReadOnlyCollection<JsonObject> priorMessages,
        string content,
        string source)
    {
        if (HasRequiredResultFirstLine(content))
            return content;

        Console.WriteLine(
            $"Qwen        : {source} violated the Result-line contract; requesting one format repair.");

        var repairMessages = priorMessages
            .Select(message => (JsonObject)message.DeepClone())
            .ToList();

        repairMessages.Add(Message("assistant", content));
        repairMessages.Add(Message(
            "user",
            "Your previous final review violated the output contract because its first non-empty line was not exactly " +
            "Result: PASS, Result: FAIL, or Result: BLOCKED. Do not investigate further and do not change the findings or verdict. " +
            "Return the same review now, preserving its substance, but make the first non-empty line exactly the matching Result line. " +
            "Do not call tools and do not include hidden reasoning."));

        var repaired = await SendAsync(
            repairMessages,
            _tools!.GetDefinitions(),
            JsonValue.Create("none"),
            reasoningEffort: "low",
            maxTokens: Math.Min(_options.MaxTokens, 8000));

        var repairedContent = GetStringOrNull(repaired.Message["content"]);

        if (!string.IsNullOrWhiteSpace(repairedContent) &&
            HasRequiredResultFirstLine(repairedContent))
        {
            Console.WriteLine("Qwen        : final Result-line format repair succeeded.");
            return repairedContent;
        }

        Console.WriteLine(
            "Qwen        : final Result-line format repair did not satisfy the contract; preserving the original response for fail-safe BLOCKED classification.");

        return content;
    }

    private static bool HasRequiredResultFirstLine(string content)
    {
        var firstNonEmptyLine = content
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.Length > 0);

        return firstNonEmptyLine is
            "Result: PASS" or
            "Result: FAIL" or
            "Result: BLOCKED";
    }

    public async Task<string> RunSmokeAsync()
    {
        var tools = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "smoke_echo",
                    ["description"] = "Return the supplied smoke-test token unchanged.",
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["token"] = new JsonObject
                            {
                                ["type"] = "string",
                            },
                        },
                        ["required"] = new JsonArray("token"),
                        ["additionalProperties"] = false,
                    },
                },
            },
        };

        var messages = new List<JsonObject>
        {
            Message(
                "system",
                "You are a transport/tool-calling smoke test. Follow the requested tool protocol exactly."),
            Message(
                "user",
                $"Call smoke_echo exactly once with token '{SmokeToken}'. After receiving the tool result, reply with exactly LACUNA_QWEN_SMOKE_OK."),
        };

        var forcedToolChoice = new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = "smoke_echo",
            },
        };

        var firstEnvelope = await SendAsync(messages, tools, forcedToolChoice);
        var firstResponse = firstEnvelope.Message;
        var toolCalls = firstResponse["tool_calls"]?.AsArray();

        if (toolCalls is not { Count: 1 })
        {
            throw new InvalidOperationException(
                $"STRATA smoke test expected exactly one tool call, but received {toolCalls?.Count ?? 0}.");
        }

        var toolCall = toolCalls[0]?.AsObject()
            ?? throw new InvalidOperationException("STRATA smoke test returned an invalid tool call.");

        var toolCallId = GetStringOrNull(toolCall["id"])
            ?? throw new InvalidOperationException("STRATA smoke tool call is missing id.");

        var function = toolCall["function"]?.AsObject()
            ?? throw new InvalidOperationException("STRATA smoke tool call is missing function.");

        var functionName = GetStringOrNull(function["name"]);
        if (!string.Equals(functionName, "smoke_echo", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"STRATA smoke test expected tool smoke_echo but received '{functionName}'.");
        }

        using var argumentsDocument = JsonDocument.Parse(GetArguments(function["arguments"]));
        var token = argumentsDocument.RootElement.TryGetProperty("token", out var tokenProperty)
            ? tokenProperty.GetString()
            : null;

        if (!string.Equals(token, SmokeToken, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"STRATA smoke tool call returned unexpected token '{token}'.");
        }

        messages.Add(CloneAssistantMessage(firstResponse));
        messages.Add(ToolMessage(
            toolCallId,
            "smoke_echo",
            $"SMOKE_TOOL_OK:{SmokeToken}"));

        var secondEnvelope = await SendAsync(
            messages,
            tools,
            JsonValue.Create("none"));
        var secondResponse = secondEnvelope.Message;

        var finalContent = GetStringOrNull(secondResponse["content"]);

        if (string.IsNullOrWhiteSpace(finalContent) ||
            !finalContent.Contains("LACUNA_QWEN_SMOKE_OK", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"STRATA smoke test tool round-trip succeeded, but the final model response was unexpected: {finalContent}");
        }

        return finalContent.Trim();
    }

    public async Task<VisionCheck> CheckVisionAsync()
    {
        if (string.IsNullOrWhiteSpace(_options.ChatCompletionsUrl))
            return new VisionCheck("endpoint_unavailable", "STRATA_CHAT_COMPLETIONS_URL is missing.");

        const string functionName = "capture_screenshot";
        var fixtures = new[]
        {
            new VisualCapture("/", "mobile", "", ReadVisionFixture("a.png")),
            new VisualCapture("/", "tablet", "", ReadVisionFixture("b.png")),
        };
        var calls = new JsonArray();
        var toolMessages = new List<JsonObject>();
        foreach (var fixture in fixtures)
        {
            var id = $"call_{fixture.Viewport}";
            calls.Add(new JsonObject
            {
                ["id"] = id,
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = functionName,
                    ["arguments"] = JsonSerializer.Serialize(new { route = fixture.Route, viewport = fixture.Viewport }),
                },
            });
            toolMessages.Add(ToolMessage(id, functionName, fixture.Label));
        }

        var messages = new List<JsonObject>
        {
            Message("system", "This is a vision transport test. Follow the requested response format exactly."),
            Message("user", "Read the three digits drawn on each screenshot. Return only the digits from mobile, " +
                "one space, then the digits from tablet. If you cannot see either image, return NO_IMAGE."),
            new() { ["role"] = "assistant", ["content"] = null, ["tool_calls"] = calls },
        };
        messages.AddRange(toolMessages);
        messages.Add(ImageMessage(fixtures));
        JsonArray tools =
        [
            new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = functionName,
                    ["description"] = "Capture a viewport screenshot.",
                    ["parameters"] = VisualReview.ToolParameters(screenshot: true),
                },
            },
        ];

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(240));
        try
        {
            var response = await SendAsync(messages, tools, JsonValue.Create("none"),
                reasoningEffort: "none", maxTokens: 128, cancellationToken: timeout.Token);
            var answer = GetStringOrNull(response.Message["content"])?.Trim();
            if (string.IsNullOrEmpty(answer) || response.Message["tool_calls"] is JsonArray { Count: > 0 })
                return new VisionCheck("model_error", "STRATA did not return a final answer for the Vision smoke.");

            return answer switch
            {
                "274 839" => new VisionCheck("available", "Model read both PNG fixtures in tool-call history."),
                "NO_IMAGE" => new VisionCheck("vision_unverified", "Model reported that it could not see the fixtures."),
                _ => new VisionCheck("vision_unverified", "Model did not identify both image-only codes."),
            };
        }
        catch (StrataRequestException exception)
        {
            return new VisionCheck(exception.Status, exception.Message);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or ArgumentOutOfRangeException)
        {
            return new VisionCheck("model_error", "STRATA returned an invalid chat completion.");
        }
    }

    private static byte[] ReadVisionFixture(string name)
    {
        using var stream = typeof(StrataAgent).Assembly.GetManifestResourceStream($"LacunaAuto.QwenUiTester.VisionFixtures.{name}")
            ?? throw new InvalidOperationException("The Vision smoke fixture is missing.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private async Task<StrataResponse> SendAsync(
        IReadOnlyCollection<JsonObject> messages,
        JsonArray tools,
        JsonNode toolChoice,
        string? reasoningEffort = null,
        int? maxTokens = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new JsonObject
        {
            ["model"] = _options.Model,
            ["messages"] = new JsonArray(messages.Select(message => message.DeepClone()).ToArray()),
            ["tools"] = tools.DeepClone(),
            ["tool_choice"] = toolChoice.DeepClone(),
            ["temperature"] = 0.1,
            ["reasoning_effort"] = reasoningEffort ?? _options.ReasoningEffort,
            ["max_tokens"] = maxTokens ?? _options.MaxTokens,
            ["stream"] = false,
        };

        using var content = new StringContent(
            payload.ToJsonString(),
            Encoding.UTF8,
            "application/json");

        var startedAt = DateTimeOffset.UtcNow;

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(_options.ChatCompletionsUrl, content, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
        {
            throw new StrataRequestException("endpoint_unavailable", "STRATA chat completion was unreachable or timed out.");
        }

        using var responseLifetime = response;

        var elapsed = DateTimeOffset.UtcNow - startedAt;
        Console.WriteLine($"STRATA HTTP : {(int)response.StatusCode} in {elapsed.TotalSeconds:F1}s.");

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var status = response.StatusCode;
            var error = responseText.ToLowerInvariant();
            var explicitVisionError = error.Contains("without the vision encoder") ||
                (status is (HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity or HttpStatusCode.NotImplemented) &&
                 (error.Contains("vision") || error.Contains("image") || error.Contains("multimodal")) &&
                 (error.Contains("disabled") || error.Contains("not support") || error.Contains("unsupported") ||
                  error.Contains("unavailable") || error.Contains("no vision") || error.Contains("not enabled")));
            var classification = "model_error";
            if (explicitVisionError)
                classification = "vision_unavailable";
            else if (status is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout ||
                (status == HttpStatusCode.NotFound && !error.Contains("model")))
                classification = "endpoint_unavailable";

            // The server may echo image payloads or other sensitive request data in error responses.
            throw new StrataRequestException(classification, $"STRATA returned HTTP {(int)status} ({classification}).");
        }

        var root = JsonNode.Parse(responseText)?.AsObject()
            ?? throw new InvalidOperationException("STRATA returned invalid JSON.");

        var choice = root["choices"]?[0]?.AsObject()
            ?? throw new InvalidOperationException("STRATA response contains no choices.");

        var message = choice["message"]?.AsObject()
            ?? throw new InvalidOperationException("STRATA response contains no assistant message.");

        var finishReason = GetStringOrNull(choice["finish_reason"]);
        if (finishReason is not (null or "stop" or "length" or "tool_calls" or "content_filter" or "function_call"))
            finishReason = "unknown";
        var assistantText = GetStringOrNull(message["content"]);
        if (assistantText is not null)
            message["content"] = _imageDataUrl.Replace(assistantText, "[image data removed]");
        var contentLength = GetStringOrNull(message["content"])?.Length ?? 0;
        var reasoningLength = GetStringOrNull(message["reasoning_content"])?.Length ?? 0;
        var toolCallCount = message["tool_calls"]?.AsArray().Count ?? 0;

        var usage = root["usage"]?.AsObject();
        var promptTokens = GetIntOrNull(usage?["prompt_tokens"]);
        var completionTokens = GetIntOrNull(usage?["completion_tokens"]);
        var totalTokens = GetIntOrNull(usage?["total_tokens"]);

        Console.WriteLine(
            $"STRATA msg  : finish={finishReason ?? "<null>"}, content={contentLength} chars, " +
            $"reasoning={reasoningLength} chars, tools={toolCallCount}, " +
            $"tokens={promptTokens?.ToString() ?? "?"}/{completionTokens?.ToString() ?? "?"}/{totalTokens?.ToString() ?? "?"}.");

        return new StrataResponse(message, finishReason);
    }

    private static JsonObject CloneAssistantMessage(JsonObject responseMessage)
    {
        var assistantMessage = new JsonObject
        {
            ["role"] = "assistant",
            ["content"] = GetStringOrNull(responseMessage["content"]),
        };

        if (responseMessage["tool_calls"] is JsonArray toolCalls && toolCalls.Count > 0)
            assistantMessage["tool_calls"] = toolCalls.DeepClone();

        return assistantMessage;
    }

    private static JsonObject ToolMessage(
        string toolCallId,
        string functionName,
        string content)
    {
        return new JsonObject
        {
            ["role"] = "tool",
            ["tool_call_id"] = toolCallId,
            ["name"] = functionName,
            ["content"] = content,
        };
    }

    private static JsonObject Message(string role, string content)
    {
        return new JsonObject
        {
            ["role"] = role,
            ["content"] = content,
        };
    }

    private static JsonObject ImageMessage(IEnumerable<VisualCapture> captures)
    {
        var content = new JsonArray();
        foreach (var capture in captures)
        {
            content.Add(new JsonObject { ["type"] = "text", ["text"] = capture.Label });
            content.Add(new JsonObject
            {
                ["type"] = "image_url",
                ["image_url"] = new JsonObject { ["url"] = $"data:image/png;base64,{Convert.ToBase64String(capture.Png)}" },
            });
        }
        return new JsonObject { ["role"] = "user", ["content"] = content };
    }

    private static string? GetStringOrNull(JsonNode? node)
    {
        if (node is null)
            return null;

        return node.GetValueKind() == JsonValueKind.String
            ? node.GetValue<string>()
            : node.ToJsonString();
    }

    private static string GetArguments(JsonNode? node)
    {
        if (node is null)
            return "{}";

        return node.GetValueKind() == JsonValueKind.String
            ? node.GetValue<string>()
            : node.ToJsonString();
    }

    private static int? GetIntOrNull(JsonNode? node)
    {
        if (node is null)
            return null;

        return node is JsonValue jsonValue &&
               jsonValue.TryGetValue<int>(out var value)
            ? value
            : null;
    }

    private sealed record StrataResponse(
        JsonObject Message,
        string? FinishReason);
}
