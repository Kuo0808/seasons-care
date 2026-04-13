namespace SeasonsCare.Api.Config.OpenAI
{
    public class OpenAiOptions
    {
        public string ApiKey { get; set; } = string.Empty;

        public string Model { get; set; } = "gpt-4o-mini";
    }
}
