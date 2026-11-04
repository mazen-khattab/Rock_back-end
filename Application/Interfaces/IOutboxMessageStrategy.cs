using Core.Enums;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IOutboxMessageStrategy
    {
        OutboxMessageType MessageType { get; }
        Task ProcessAsync(string payload);
    }
}
