using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MessageGuard.Application.Ports;
using MessageGuard.Domain;
using MessageGuard.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MessageGuard.Infrastructure.Classifiers;

public class JevClassifier(
    HttpClient http,
    IConfiguration configuration,
    ILogger<JevClassifier> logger) : IMessageClassifier
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<ClassificationResult> ClassifyAsync(string content, CancellationToken ct = default)
    {
        var apiKey = configuration["Jev:ApiKey"]
            ?? throw new InvalidOperationException("Jev:ApiKey no está configurada.");

        var request = new JevRequest(
            State: content,
            Model: "jev-latest",
            Questions: new Dictionary<string, JevQuestion>
            {
                ["classification"] = new JevQuestion(
                    Type: "choice",
                    Instructions: "Classify this help request message",
                    Criteria: new Dictionary<string, string>
                    {
                        ["spam"]   = "Promotional, unsolicited or irrelevant content",
                        ["threat"] = "Contains threatening language, violence or intent to cause harm",
                        ["urgent"] = "Emergency situation that requires immediate attention",
                        ["normal"] = "Regular help request that should be processed normally"
                    }
                )
            }
        );

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.typesafe.ai/v1/systemone");
        req.Headers.Authorization = new("Bearer", apiKey);
        req.Content = JsonContent.Create(request, options: JsonOpts);

        HttpResponseMessage res;
        try
        {
            res = await http.SendAsync(req, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Jev no respondió. El mensaje queda en revisión manual.");
            throw;
        }

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            logger.LogError("Jev devolvió {Status}: {Body}", res.StatusCode, body);
            throw new HttpRequestException($"Jev respondió con {res.StatusCode}");
        }

        var response = await res.Content.ReadFromJsonAsync<JevResponse>(JsonOpts, ct)
            ?? throw new InvalidOperationException("Jev devolvió una respuesta vacía.");

        var answer = response.Answers["classification"];
        var choice = answer.Choice ?? throw new InvalidOperationException("Jev no devolvió clasificación.");

        logger.LogInformation("Jev clasificó como '{Choice}' con confianza {Confidence:P0}", choice, answer.Confidence);

        var classification = choice.ToLowerInvariant() switch
        {
            "spam"   => Classification.Spam,
            "threat" => Classification.Threat,
            "urgent" => Classification.Urgent,
            _        => Classification.Normal
        };

        IReadOnlyDictionary<string, double> probs = answer.Probabilities
            ?? new Dictionary<string, double> { [choice] = answer.Confidence };

        return new ClassificationResult(classification, answer.Confidence, probs);
    }
}

// DTOs internos — solo viven en este archivo
record JevRequest(string State, string Model, Dictionary<string, JevQuestion> Questions);

record JevQuestion(string Type, string Instructions, Dictionary<string, string> Criteria);

record JevResponse(
    string Model,
    Dictionary<string, JevAnswer> Answers,
    JevUsage Usage);

record JevAnswer(
    string Type,
    [property: JsonPropertyName("choice")] string? Choice,
    double Confidence,
    Dictionary<string, double>? Probabilities);

record JevUsage(int InputTokens, int OutputTokens);
