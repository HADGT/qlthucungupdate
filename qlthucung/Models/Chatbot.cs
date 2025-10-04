using System.Collections.Generic;

namespace qlthucung.Models
{
    public class Chatbot
    {
        public class ChatRequest
        {
            public string UserInput { get; set; } = string.Empty;
            public string? JwtToken { get; set; }
        };

        public class ChatResponse
        {
            public string Response { get; set; } = string.Empty;
            public string? Intent { get; set; }
            public double? Confidence { get; set; }
            public Dictionary<string, object>? HotelInfo { get; set; }
            public Dictionary<string, object>? BookingInfo { get; set; }
            public string? ExtractedRequirements { get; set; }
        };
    }
}
