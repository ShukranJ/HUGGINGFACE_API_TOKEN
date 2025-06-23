using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
string hfApiToken = Environment.GetEnvironmentVariable("HUGGINGFACE_API_TOKEN");
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
string modelName = "gpt2";  // You can change this to another supported model

app.MapPost("/ask", async (HttpRequest request) =>
{
    try
    {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(body);
        var question = data?["question"] ?? "No question provided.";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hfApiToken);

        var payload = new { inputs = question };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"https://api-inference.huggingface.co/models/{modelName}", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return Results.Json(new { error = $"API returned {response.StatusCode}", details = responseBody });
        }

        var jsonDoc = JsonDocument.Parse(responseBody);
        var generatedText = jsonDoc.RootElement[0].GetProperty("generated_text").GetString();

        return Results.Json(new { reply = generatedText });
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = "Internal error", details = ex.Message });
    }
});

app.MapGet("/", () => "Welcome to HuggingFace AskMyLawBotAPI!");

app.Run();
