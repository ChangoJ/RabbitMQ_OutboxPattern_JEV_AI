using MessageGuard.Application.Ports;
using MessageGuard.Domain;
using MessageGuard.Domain.Enums;

namespace MessageGuard.Tests.Application.Fakes;

public class FakeClassifier : IMessageClassifier
{
    private readonly Classification _result;

    public FakeClassifier(Classification result) => _result = result;

    public Task<ClassificationResult> ClassifyAsync(string content, CancellationToken ct = default)
    {
        var probs = new Dictionary<string, double>
        {
            ["spam"]   = _result == Classification.Spam   ? 1.0 : 0.0,
            ["threat"] = _result == Classification.Threat ? 1.0 : 0.0,
            ["urgent"] = _result == Classification.Urgent ? 1.0 : 0.0,
            ["normal"] = _result == Classification.Normal ? 1.0 : 0.0,
        };
        return Task.FromResult(new ClassificationResult(_result, Confidence: 1.0, probs));
    }
}
