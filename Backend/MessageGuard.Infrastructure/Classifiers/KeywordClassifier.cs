using MessageGuard.Application.Ports;
using MessageGuard.Domain;
using MessageGuard.Domain.Enums;

namespace MessageGuard.Infrastructure.Classifiers;

public class KeywordClassifier : IMessageClassifier
{
    private static readonly string[] SpamWords   = ["oferta", "gratis", "compra", "premio"];
    private static readonly string[] ThreatWords = ["matar", "amenaza", "bomba", "explotar"];
    private static readonly string[] UrgentWords = ["urgente", "emergencia", "socorro", "ayuda ya"];

    public Task<ClassificationResult> ClassifyAsync(string content, CancellationToken ct = default)
    {
        var lower = content.ToLowerInvariant();

        Classification choice;
        if (ThreatWords.Any(lower.Contains))      choice = Classification.Threat;
        else if (SpamWords.Any(lower.Contains))   choice = Classification.Spam;
        else if (UrgentWords.Any(lower.Contains)) choice = Classification.Urgent;
        else                                       choice = Classification.Normal;

        // Clasificador determinista: siempre 100% seguro en su elección
        var probs = new Dictionary<string, double>
        {
            ["spam"]   = choice == Classification.Spam   ? 1.0 : 0.0,
            ["threat"] = choice == Classification.Threat ? 1.0 : 0.0,
            ["urgent"] = choice == Classification.Urgent ? 1.0 : 0.0,
            ["normal"] = choice == Classification.Normal ? 1.0 : 0.0,
        };

        return Task.FromResult(new ClassificationResult(choice, Confidence: 1.0, probs));
    }
}
