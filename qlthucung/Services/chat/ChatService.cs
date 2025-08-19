using Microsoft.EntityFrameworkCore;
using qlthucung.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qlthucung.Services.chat
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;

        public ChatService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Message>> GetMessagesAsync(string userId)
        {
            return await _context.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .OrderBy(m => m.SentAt)
                .Select(m => new Message
                {
                    ReceiverId = m.ReceiverId,
                    SenderId = m.SenderId,
                    Content = m.Content,
                    SentAt = m.SentAt,
                    IsRead = m.IsRead
                })
                .ToListAsync();
        }
    }
}
