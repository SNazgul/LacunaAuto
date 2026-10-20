using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

namespace LacunaAuto.QwenUiTester;

internal sealed class StrataAgent : IDisposable
{
    private readonly RunnerOptions _options;
    private readonly ToolRegistry _tools;
    private readonly HttpClient _httpClient;

    public StrataAgent(RunnerOptions options, ToolRegistry tools)
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
        var messages = new List<JsonObject>
        {
            Message("system", systemPrompt),
            Message("user", userPrompt),
        };

        for (var turn = 1; turn <= _options.MaxToolTurns; turn++)
        {
            var payload = new JsonObject
            {
                ["model"] = _options.Model,
                ["messages"] = new JsonArray(messages.Select(message => message.DeepClone()).ToArray()),
                ["tools"] = _tools.GetDefinitions(),
                ["tool_choice"] = "auto",
                ["temperature"] = 0.1,
                ["max_tokens"] = _options.MaxTokens,
                ["stream"] = false,
            };

            using var content = new StringContent(
                payload.ToJsonString(),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.PostAsync(
                $"{_options.StrataBaseUrl}/v1/chat/completions",
                content);

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

            var responseMessage = choice["message"]?.AsObject()
                ?? throw new InvalidOperationException("STRATA response contains no assistant message.");

            var assistantContent = GetStringOrNull(responseMessage["content"]);
            var toolCalls = responseMessage["tool_calls"]?.AsArray();

            var assistantMessage = new JsonObject
            {
                ["role"] = "assistant",
                ["content"] = assistantContent,
            };

            if (toolCalls is { Count: > 0 })
                assistantMessage["tool_calls"] = toolCalls.DeepClone();

            messages.Add(assistantMessage);

            if (toolCalls is not { Count: > 0 })
            {
                if (string.IsNullOrWhiteSpace(assistantContent))
                    throw new InvalidOperationException("STRATA returned neither content nor tool calls.");

                return assistantContent;
            }

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
                var toolResult = await _tools.ExecuteAsync(functionName, functionArguments);

                messages.Add(new JsonObject
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = toolCallId,
                    ["name"] = functionName,
                    ["content"] = toolResult,
                });
            }
        }

        throw new InvalidOperationException(
            $"STRATA exceeded the configured maximum of {_options.MaxToolTurns} tool turns.");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
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

        return node.GetValueKind() == System.Text.Json.JsonValueKind.String
            ? node.GetValue<string>()
            : node.ToJsonString();
    }

    private static string GetArguments(JsonNode? node)
    {
        if (node is null)
            return "{}";

        return node.GetValueKind() == System.Text.Json.JsonValueKind.String
            ? node.GetValue<string>()
            : node.ToJsonString();
    }
}
