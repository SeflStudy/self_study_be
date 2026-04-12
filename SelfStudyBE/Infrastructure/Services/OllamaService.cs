using System.Net.Http.Json;

namespace Infrastructure.Services;

public class OllamaService
{
    private readonly HttpClient _httpClient;
    private const string OllamaUrl = "http://localhost:11434/api/generate";

    public OllamaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GenerateAsync(string model, string prompt, float temperature = 0.7f)
    {
        var requestBody = new
        {
            model = model,
            prompt = prompt,
            temperature = temperature,
            stream = false,
            format = "json"          
        };

        var response = await _httpClient.PostAsJsonAsync(OllamaUrl, requestBody);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Ollama error: {response.StatusCode}");
        Console.WriteLine(await response.Content.ReadAsStringAsync());       
        
        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
        
        return result?.Response ?? throw new Exception("Empty response from Ollama");
    }
}

public class OllamaResponse
{
    public string Model { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public bool Done { get; set; }
}