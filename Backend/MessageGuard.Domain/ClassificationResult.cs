using MessageGuard.Domain.Enums;

namespace MessageGuard.Domain;

public record ClassificationResult(
    Classification Classification,
    double Confidence,
    IReadOnlyDictionary<string, double> Probabilities);
