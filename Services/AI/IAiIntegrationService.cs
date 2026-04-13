using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.HealthDashboard;

namespace SeasonsCare.Api.Services.AI
{
    public interface IAiIntegrationService
    {
        Task<AiGeneratedInsightDto> GenerateHealthInsightAsync(HealthInsightPromptInput input);
    }
}
