using System.Text.Json;
using TechChallenge.Helpers;
using TechChallenge.Models;
namespace TechChallenge.Services
{
    public class PregaoB3FetchService
    {
        private const string BaseUrl = "https://sistemaswebb3-listados.b3.com.br/indexProxy/indexCall/GetPortfolioDay/";
        private readonly HttpClient _http;
        public PregaoB3FetchService(HttpClient http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http), "HttpClient não pode ser nulo.");
        }
        public async Task<PregaoB3GetResponseModel> GetCurrentPageAsync(PregaoB3GetRequestModel request, CancellationToken ct = default)
        {
            var base64Url = Base64UrlHelper.EncodeToBase64Url(request);
            var url = $"{BaseUrl}{base64Url}";
            var json = await _http.GetStringAsync(url, ct);
            return JsonSerializer.Deserialize<PregaoB3GetResponseModel>(json)
                   ?? throw new InvalidOperationException("Falha ao desserializar resposta.");
        }

        public PregaoB3GetRequestModel BuildPregaoB3GetRequestModel(int pageNumber)
        {
            return new PregaoB3GetRequestModel() { Language = "pt-br", PageNumber = pageNumber, PageSize = 99999, Index = "IBOV", Segment = "1", };
        }

        public async Task<PregaoB3GetResponseModel> GetAllPagesAsync(PregaoB3GetRequestModel firstPageRequest, CancellationToken ct = default)
        {
            List<ResultItemGetResponseModel> allPagesData = new List<ResultItemGetResponseModel>();
            var firstPageData = await GetCurrentPageAsync(firstPageRequest, ct);
            allPagesData.AddRange(firstPageData.Results);
            var currentPage = firstPageRequest.PageNumber;
            while (currentPage < firstPageData.Page.TotalPages)
            {
                currentPage++;
                var nextPageGetRequestModel = BuildPregaoB3GetRequestModel(currentPage);
                var currentPageData = await GetCurrentPageAsync(nextPageGetRequestModel, ct);
                allPagesData.AddRange(currentPageData.Results);
            }
            return new PregaoB3GetResponseModel
            {
                Header = firstPageData.Header,
                Page = firstPageData.Page,
                Results = allPagesData
            };
        }
    }
}
