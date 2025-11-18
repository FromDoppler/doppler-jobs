using System.Threading.Tasks;

namespace OfflineConversionsJob.Services;

public interface IGoogleConversionService
{
    Task UploadConversionsToGoogle();
}