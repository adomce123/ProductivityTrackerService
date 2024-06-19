using System.Threading.Tasks;

namespace ProductivityTrackerService.Core.Interfaces
{
    public interface ISharedCache
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value);
    }
}
