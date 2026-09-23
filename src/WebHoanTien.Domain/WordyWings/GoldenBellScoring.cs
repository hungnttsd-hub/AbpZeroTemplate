using System;
using System.Text.Json;

namespace WebHoanTien.WordyWings;

/// <summary>Version 1, mirrored by react/src/game/golden-bell/model.ts. Never retroactively grade legacy rounds.</summary>
public static class GoldenBellScoring
{
    public static long TimeLimitMs(JsonElement question) => (long)Math.Clamp(
        Math.Ceiling((question.GetProperty("estimatedSeconds").GetDouble() + question.GetProperty("difficulty").GetInt32()) / 5) * 5, 15, 60) * 1000;
    public static int Points(JsonElement question, long answerMs, int wrong, bool hintUsed)
    {
        var limit = TimeLimitMs(question);
        if (answerMs >= limit) return 0;
        return Math.Max(20, 100 + (int)Math.Floor(50d * Math.Max(0, limit - answerMs) / limit) - wrong * 25 - (hintUsed ? 20 : 0));
    }
}
