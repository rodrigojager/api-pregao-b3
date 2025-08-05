using System.Text.Json.Serialization;

namespace TechChallenge.Models
{
    public class PageInfoGetResponseModel
    {
        [JsonPropertyName("pageNumber")] public int PageNumber { get; set; }
        [JsonPropertyName("pageSize")] public int PageSize { get; set; }
        [JsonPropertyName("totalRecords")] public int TotalRecords { get; set; }
        [JsonPropertyName("totalPages")] public int TotalPages { get; set; }
    }
    public class HeaderInfoGetResponseModel
    {
        [JsonPropertyName("date")] public string Date { get; set; } = default!;
        [JsonPropertyName("text")] public string Text { get; set; } = default!;
        [JsonPropertyName("part")] public string Part { get; set; } = default!;
        [JsonPropertyName("partAcum")] public string? PartAcum { get; set; }
        [JsonPropertyName("textReductor")] public string TextReductor { get; set; } = default!;
        [JsonPropertyName("reductor")] public string Reductor { get; set; } = default!;
        [JsonPropertyName("theoricalQty")] public string TheoricalQty { get; set; } = default!;
    }
    public sealed class ResultItemGetResponseModel
    {
        [JsonPropertyName("segment")] public string? Segment { get; set; }
        [JsonPropertyName("cod")] public string Cod { get; set; } = default!;
        [JsonPropertyName("asset")] public string Asset { get; set; } = default!;
        [JsonPropertyName("type")] public string Type { get; set; } = default!;
        [JsonPropertyName("part")] public string Part { get; set; } = default!;
        [JsonPropertyName("partAcum")] public string? PartAcum { get; set; }
        [JsonPropertyName("theoricalQty")] public string TheoricalQty { get; set; } = default!;
    }
    public sealed class PregaoB3GetResponseModel
    {
        [JsonPropertyName("page")] public PageInfoGetResponseModel Page { get; set; } = default!;
        [JsonPropertyName("header")] public HeaderInfoGetResponseModel Header { get; set; } = default!;
        [JsonPropertyName("results")] public List<ResultItemGetResponseModel> Results { get; set; } = new();
    }
    public class PregaoB3GetRequestModel
    {
        /* JSON usado na requisição GET para o endpoint do pregão da B3:

            {
              "language": "pt-br",
              "pageNumber": 1,
              "pageSize": 20,
              "index": "IBOV",
              "segment": "1"
            }
     */

        [JsonPropertyName("language")]
        public string Language { get; set; } = "pt-br";

        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; } = 1;

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; } = 99999;

        [JsonPropertyName("index")]
        public string Index { get; set; } = "IBOV";

        [JsonPropertyName("segment")]
        public string Segment { get; set; } = "1";
    }
}
