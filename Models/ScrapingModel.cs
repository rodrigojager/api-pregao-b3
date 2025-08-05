using System.Text.Json.Serialization;

namespace TechChallenge.Models
{
    public class ScrapeResultModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public IReadOnlyList<IReadOnlyList<string>>? Data { get; set; }
        public string Date { get; set; }
        public int TotalRecords { get; set; }
    }
    
}
