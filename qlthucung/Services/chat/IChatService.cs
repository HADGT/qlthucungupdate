using qlthucung.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace qlthucung.Services.chat
{
    public interface IChatService
    {
        Task<List<Message>> GetMessagesAsync(string userId);
    }
}
