using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();


string buildAzureOpenAIResponse(string completionText)
{
    // FYI confirmed can remove choices[].content_filter_results but not prompt_filter_results ... though the code could change, see: https://github.com/microsoft/terminal/blob/938b3ec2f2f5e1ba37a951dfdee078b1a7a40394/src/cascadia/QueryExtension/ExtensionPalette.cpp#L326-L355
    // PRN map additional fields that I am ignoring for now like token #s, times, etc: b/c it doesn't really affect win term
    // FYI can remove all under prompt_filter_results[0].content_filter_results except jailbreak must remain
    const string template = """
{"choices":[{"content_filter_results":{"hate":{"filtered":false,"severity":"safe"},"protected_material_code":{"filtered":false,"detected":false},"protected_material_text":{"filtered":false,"detected":false},"self_harm":{"filtered":false,"severity":"safe"},"sexual":{"filtered":false,"severity":"safe"},"violence":{"filtered":false,"severity":"safe"}},"finish_reason":"stop","index":0,"logprobs":null,"message":{"content":"I'm sorry, but I'm not sure what you mean by \"Program\". Are you asking what type of shell you are currently using? If so, you can use the following command to find out:\n\n```\necho $0\n```\n\nThis will print the name of the current shell you are using.","role":"assistant"}}],"created":1711408935,"id":"chatcmpl-foo","model":"gpt-35-turbo","object":"chat.completion","prompt_filter_results":[{"prompt_index":0,"content_filter_results":{"hate":{"filtered":false,"severity":"safe"},"jailbreak":{"filtered":false,"detected":false},"self_harm":{"filtered":false,"severity":"safe"},"sexual":{"filtered":false,"severity":"safe"},"violence":{"filtered":false,"severity":"safe"}}}],"system_fingerprint":null,"usage":{"completion_tokens":62,"prompt_tokens":359,"total_tokens":421}}
""";

    // modify json:
    var json = JsonNode.Parse(template).AsObject();
    json["choices"][0]["message"]["content"] = completionText;

    return json.ToString();
}

app.MapPost("/answer", async (HttpContext context, string? model, string? backend, [FromHeader(Name = "api-key")] string? apiKey) =>
{
    using var reader = new StreamReader(context.Request.Body);
    string json = await reader.ReadToEndAsync();

    var jsonObject = JsonNode.Parse(json);
    var jsonMessages = jsonObject["messages"].AsArray();

    // FYI probably could deserialize jsonObject using ChatCompletionOptions class? it seems to match (messages => chat messages, params)
    ChatMessage toChatMessage(JsonNode message)
    {
        var role = message["role"].AsValue().ToString();
        var content = message["content"].AsValue().ToString();
        System.Console.WriteLine($"role: {role}, content: {content}");
        if (role == "user")
        {
            return new UserChatMessage(content);
        }
        else if (role == "assistant")
        {
            return new AssistantChatMessage(content);
        }
        else if (role == "system")
        {
            return new SystemChatMessage(content);
        }
        else
        {
            throw new Exception("Invalid role: " + role + " with content: " + content);
        }
    }

    var chatMessages = jsonMessages.Select(toChatMessage).ToList();

    // FYI one diff w/ azure openai is that you deploy a model and name the deployment and pass a URL to the completion endpoint that has the deploy name in it... therefore you never pass a model paramter b/c that is set per deployment... whereas with openai API you pass model as a param in the request body
    model = string.IsNullOrEmpty(model) ? "llama3" : model;
    backend = string.IsNullOrEmpty(backend) ? "http://127.0.0.1:11434/v1" : backend;
    Console.WriteLine($"model: {model}, backend: {backend}");
    // System.Console.WriteLine($"apiKey: {apiKey}");

    var options = new OpenAI.OpenAIClientOptions
    {
        Endpoint = new Uri(backend)
    };

    // ? catch errors and use generateResponse w/ a meaningful message;
    var client = new ChatClient(model, apiKey ?? "whatever", options);

    // "max_tokens":800,"temperature":0.7,"frequency_penalty":0,"presence_penalty":0,"top_p":0.95,"stop":"None"
    var chatOptions = new ChatCompletionOptions
    {
        MaxTokens = 150, // 800 passed
        Temperature = 0.7f,
        TopP = 0.95f,
        PresencePenalty = 0,
        FrequencyPenalty = 0,
    };
    var response = client.CompleteChat(chatMessages, options: chatOptions);
    var completionText = response.Value.Content[0].Text;
    return buildAzureOpenAIResponse(completionText);

});




// TESTING ENDPOINTS:
app.MapGet("/program", (HttpContext context) =>
{
    context.Response.Headers.TryAdd("Content-Type", "application/json");

    return askOpenAICompat("what is a program?");
});

app.MapPost("/static", (HttpContext context) =>
{
    context.Response.Headers.TryAdd("Content-Type", "application/json");

    return """
{"choices":[{"content_filter_results":{"hate":{"filtered":false,"severity":"safe"},"protected_material_code":{"filtered":false,"detected":false},"protected_material_text":{"filtered":false,"detected":false},"self_harm":{"filtered":false,"severity":"safe"},"sexual":{"filtered":false,"severity":"safe"},"violence":{"filtered":false,"severity":"safe"}},"finish_reason":"stop","index":0,"logprobs":null,"message":{"content":"I'm sorry, but I'm not sure what you mean by \"Program\". Are you asking what type of shell you are currently using? If so, you can use the following command to find out:\n\n```\necho $0\n```\n\nThis will print the name of the current shell you are using.","role":"assistant"}}],"created":1718508935,"id":"chatcmpl-id1","model":"gpt-35-turbo","object":"chat.completion","prompt_filter_results":[{"prompt_index":0,"content_filter_results":{"hate":{"filtered":false,"severity":"safe"},"jailbreak":{"filtered":false,"detected":false},"self_harm":{"filtered":false,"severity":"safe"},"sexual":{"filtered":false,"severity":"safe"},"violence":{"filtered":false,"severity":"safe"}}}],"system_fingerprint":null,"usage":{"completion_tokens":62,"prompt_tokens":359,"total_tokens":421}}
""";
});

string askOpenAICompat(string question, string? model = null, string? backend = null, string? apiKey = null)
{
    model = string.IsNullOrEmpty(model) ? "llama3" : model;
    backend = string.IsNullOrEmpty(backend) ? "http://127.0.0.1:11434/v1" : backend;

    Console.WriteLine($"model: {model}, backend: {backend}\nquestion: {question}");
    // System.Console.WriteLine($"apiKey: {apiKey}");

    var options = new OpenAI.OpenAIClientOptions
    {
        Endpoint = new Uri(backend)
    };

    // ? catch errors and use generateResponse w/ a meaningful message;
    var client = new ChatClient(model, apiKey ?? "whatever", options);
    var response = client.CompleteChat(question);
    var completionText = response.Value.Content[0].Text;
    return buildAzureOpenAIResponse(completionText);
}

app.Run();
