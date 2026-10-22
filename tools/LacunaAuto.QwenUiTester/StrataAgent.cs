using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LacunaAuto.QwenUiTester;

internal sealed class StrataAgent : IDisposable
{
    private const string SmokeToken = "LACUNA_QWEN_SMOKE";

    private readonly RunnerOptions _options;
    private readonly ToolRegistry? _tools;
    private readonly HttpClient _httpClient;

    public StrataAgent(RunnerOptions options, ToolRegistry? tools = null)
    {
        _options = options;
        _tools = tools;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(30),
        };

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

        for (var turn = 1; turn <= _options.MaxToolTurns; turn++)
        {
            Console.WriteLine($"Qwen turn   : {turn}/{_options.MaxToolTurns} - sending request...");

            var response = await SendAsync(
                messages,
                _tools.GetDefinitions(),
                JsonValue.Create("auto"));

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
                    return recoveredContent;
                }

                Console.WriteLine("Qwen        : final response received.");
                return assistantContent;
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
                Console.WriteLine($"Qwen tool   : {functionName} started.");

                var toolResult = await _tools.ExecuteAsync(functionName, functionArguments);

                Console.WriteLine($"Qwen tool   : {functionName} completed.");
                messages.Add(ToolMessage(toolCallId, functionName, toolResult));
            }
        }

        throw new InvalidOperationException(
            $"STRATA exceeded the configured maximum of {_options.MaxToolTurns} tool turns.");
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

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private async Task<StrataResponse> SendAsync(
        IReadOnlyCollection<JsonObject> messages,
        JsonArray tools,
        JsonNode toolChoice,
        string? reasoningEffort = null,
        int? maxTokens = null)
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

        using var response = await _httpClient.PostAsync(
            _options.ChatCompletionsUrl,
            content);

        var elapsed = DateTimeOffset.UtcNow - startedAt;
        Console.WriteLine($"STRATA HTTP : {(int)response.StatusCode} in {elapsed.TotalSeconds:F1}s.");

        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"STRATA returned HTTP {(int)response.StatusCode}: {responseText}");
        }

        var root = JsonNode.Parse(responseText)?.AsObject()
            ?? throw new InvalidOperationException("STRATA returned invalid JSON.");

        var choice = root["choices"]?[0]?.AsObject()
            ?? throw new InvalidOperationException("STRATA response contains no choices.");

        var message = choice["message"]?.AsObject()
            ?? throw new InvalidOperationException("STRATA response contains no assistant message.");

        var finishReason = GetStringOrNull(choice["finish_reason"]);
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
