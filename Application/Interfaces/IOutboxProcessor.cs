using Hangfire;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IOutboxProcessor
    {
        [DisableConcurrentExecution(timeoutInSeconds: 60)]
        [AutomaticRetry(Attempts = 0, LogEvents = false)]
        Task ProcessPendingMessagesAsync();
    }
}
