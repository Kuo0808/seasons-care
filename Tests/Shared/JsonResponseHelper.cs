using System.Text.Json;

namespace SeasonsCare.Api.Tests.Shared;

public static class JsonResponseHelper
{
    public static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}
