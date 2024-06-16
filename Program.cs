using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

string askOpenAICompat(string question, string? model = null, string? endpoint = null, string? apiKey = null)
{
    model = string.IsNullOrEmpty(model) ? "llama3" : model;
    endpoint = string.IsNullOrEmpty(endpoint) ? "http://127.0.0.1:11434/v1" : endpoint;

    Console.WriteLine($"model: {model}, endpoint: {endpoint}\nquestion: {question}");
    // System.Console.WriteLine($"apiKey: {apiKey}");

    var options = new OpenAI.OpenAIClientOptions
    {
        Endpoint = new Uri(endpoint)
    };

    // ? catch errors and use generateResponse w/ a meaningful message;
    var client = new ChatClient(model, apiKey ?? "whatever", options);
    var response = client.CompleteChat(question);
    var completionText = response.Value.Content[0].Text;
    return buildAzureOpenAIResponse(completionText);
}

string buildAzureOpenAIResponse(string completionText)
{
    const string template = """
{"choices":[{"content_filter_results":{"hate":{"filtered":false,"severity":"safe"},"protected_material_code":{"filtered":false,"detected":false},"protected_material_text":{"filtered":false,"detected":false},"self_harm":{"filtered":false,"severity":"safe"},"sexual":{"filtered":false,"severity":"safe"},"violence":{"filtered":false,"severity":"safe"}},"finish_reason":"stop","index":0,"logprobs":null,"message":{"content":"I'm sorry, but I'm not sure what you mean by \"Program\". Are you asking what type of shell you are currently using? If so, you can use the following command to find out:\n\n```\necho $0\n```\n\nThis will print the name of the current shell you are using.","role":"assistant"}}],"created":1711408935,"id":"chatcmpl-foo","model":"gpt-35-turbo","object":"chat.completion","prompt_filter_results":[{"prompt_index":0,"content_filter_results":{"hate":{"filtered":false,"severity":"safe"},"jailbreak":{"filtered":false,"detected":false},"self_harm":{"filtered":false,"severity":"safe"},"sexual":{"filtered":false,"severity":"safe"},"violence":{"filtered":false,"severity":"safe"}}}],"system_fingerprint":null,"usage":{"completion_tokens":62,"prompt_tokens":359,"total_tokens":421}}
""";

    // modify json:
    var json = JsonNode.Parse(template).AsObject();
    json["choices"][0]["message"]["content"] = completionText;

    return json.ToString();
}

// todo pass model name param
app.MapPost("/answer", async (HttpContext context, string? model, string? endpoint, [FromHeader(Name = "api-key")] string? apiKey) =>
{
    using var reader = new StreamReader(context.Request.Body);
    string json = await reader.ReadToEndAsync();

    var jsonObject = JsonNode.Parse(json);
    var messages = jsonObject["messages"].AsArray();
    var lastMessage = messages[messages.Count - 1].AsObject();
    var question = lastMessage["content"].AsValue().ToString();
    return askOpenAICompat(question, model: model, endpoint: endpoint, apiKey: apiKey);
});

// TESTING ENDPOINTS:
app.MapGet("/program", () =>
{
    return askOpenAICompat("what is a program?");
});

app.MapPost("/static", (HttpContext context) =>
{
    context.Response.Headers.TryAdd("Content-Type", "application/json");

    return """
{"choices":[{"content_filter_results":{"hate":{"filtered":false,"severity":"safe"},"protected_material_code":{"filtered":false,"detected":false},"protected_material_text":{"filtered":false,"detected":false},"self_harm":{"filtered":false,"severity":"safe"},"sexual":{"filtered":false,"severity":"safe"},"violence":{"filtered":false,"severity":"safe"}},"finish_reason":"stop","index":0,"logprobs":null,"message":{"content":"I'm sorry, but I'm not sure what you mean by \"Program\". Are you asking what type of shell you are currently using? If so, you can use the following command to find out:\n\n```\necho $0\n```\n\nThis will print the name of the current shell you are using.","role":"assistant"}}],"created":1711408935,"id":"chatcmpl-foo","model":"gpt-35-turbo","object":"chat.completion","prompt_filter_results":[{"prompt_index":0,"content_filter_results":{"hate":{"filtered":false,"severity":"safe"},"jailbreak":{"filtered":false,"detected":false},"self_harm":{"filtered":false,"severity":"safe"},"sexual":{"filtered":false,"severity":"safe"},"violence":{"filtered":false,"severity":"safe"}}}],"system_fingerprint":null,"usage":{"completion_tokens":62,"prompt_tokens":359,"total_tokens":421}}
""";
});

app.Run();
