using RabbitMQ.Client.Events;

namespace MessageGuard.Infrastructure.Workers;

internal static class RetryHelper
{
    private const int MaxRetries = 3;

    // RabbitMQ agrega el header x-death cada vez que un mensaje es dead-lettered.
    // Cada entrada tiene un campo "count" que indica cuántas veces murió en esa cola.
    public static int GetDeathCount(BasicDeliverEventArgs ea)
    {
        if (ea.BasicProperties.Headers is null || !ea.BasicProperties.Headers.TryGetValue("x-death", out var raw))
            return 0;

        if (raw is not List<object> deaths || deaths.Count == 0)
            return 0;

        if (deaths[0] is not IDictionary<string, object> first)
            return 0;

        return first.TryGetValue("count", out var count) ? Convert.ToInt32(count) : 0;
    }

    public static bool ShouldSendToDeadLetter(BasicDeliverEventArgs ea)
        => GetDeathCount(ea) >= MaxRetries;
}
