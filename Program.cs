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

app.MapGet("/test", () =>
{
    const string url = "http://127.0.0.1:11434/v1";
    var options = new OpenAI.OpenAIClientOptions
    {
        Endpoint = new Uri(url)
    };

    var client = new ChatClient("llama3", "whatever-key", options);
    var response = client.CompleteChat("What is the program?");
    return response.Value.Content;
});

app.MapPost("/fake/{*rest}", (HttpContext context) =>
{
    context.Response.Headers.TryAdd("Content-Type", "application/json");

    return """
{"choices":[{"content_filter_results":{"hate":{"filtered":false,"severity":"safe"},"protected_material_code":{"filtered":false,"detected":false},"protected_material_text":{"filtered":false,"detected":false},"self_harm":{"filtered":false,"severity":"safe"},"sexual":{"filtered":false,"severity":"safe"},"violence":{"filtered":false,"severity":"safe"}},"finish_reason":"stop","index":0,"logprobs":null,"message":{"content":"I'm sorry, but I'm not sure what you mean by \"Program\". Are you asking what type of shell you are currently using? If so, you can use the following command to find out:\n\n```\necho $0\n```\n\nThis will print the name of the current shell you are using.","role":"assistant"}}],"created":1711408935,"id":"chatcmpl-foo","model":"gpt-35-turbo","object":"chat.completion","prompt_filter_results":[{"prompt_index":0,"content_filter_results":{"hate":{"filtered":false,"severity":"safe"},"jailbreak":{"filtered":false,"detected":false},"self_harm":{"filtered":false,"severity":"safe"},"sexual":{"filtered":false,"severity":"safe"},"violence":{"filtered":false,"severity":"safe"}}}],"system_fingerprint":null,"usage":{"completion_tokens":62,"prompt_tokens":359,"total_tokens":421}}
""";
});

app.Run();
