using GenerativeAI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using qlthucung.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace qlthucung.Services.chat
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _geminiApiKey;
        private readonly string _baseApiUrl;
        private readonly GenerativeModel client;

        public ChatService(AppDbContext context, IConfiguration configuration, HttpClient httpClient)
        {
            _context = context;
            _configuration = configuration;
            _httpClient = httpClient;
            _geminiApiKey = configuration["GoogleAI:ApiKey"] ?? throw new Exception("Missing Gemini API key");
            _baseApiUrl = "http://localhost:5172";
            client = new GenerativeModel(_geminiApiKey, "gemini-1.5-flash");
        }

        public void API_link_web(ChatService service)
        {
            string base_api_url = service._baseApiUrl;
            string sanpham_api_url = base_api_url + "/SanPham";
            string dichvu_api_url = base_api_url + "/DichVu";
            string booking_api_url = base_api_url + "/booking/create";
            string booking_list_url = base_api_url + "/booking/list";
            string gemini_api_key = service._geminiApiKey;
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
