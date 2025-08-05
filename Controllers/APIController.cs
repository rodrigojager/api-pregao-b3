using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TechChallenge.Models;
using TechChallenge.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace TechChallenge.Controllers
{
    [ApiController]
    [Route("api/[action]")]
    [SwaggerTag("API para operações relacionadas aos dados da B3")]
    public class APIController : Controller
    {
        /// <summary>
        /// Busca dados de pregão da B3 via API oficial
        /// </summary>
        /// <returns>Dados completos do pregão em formato JSON</returns>
        /// <response code="200">Dados do pregão retornados com sucesso</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Buscar dados de pregão da B3",
            Description = "Recupera dados de pregão da B3 através da API oficial, incluindo informações de negociação, volumes e preços",
            OperationId = "GetPregaoB3Data",
            Tags = new[] { "Dados B3" }
        )]
        [SwaggerResponse(200, "Dados do pregão retornados com sucesso", typeof(string))]
        [SwaggerResponse(500, "Erro interno do servidor")]
        public async Task<IActionResult> GetPregaoB3DataAsync() {
            var PregaoB3Service = new PregaoB3FetchService(new HttpClient());
            var getRequestData = PregaoB3Service.BuildPregaoB3GetRequestModel(1);
            var pregaoB3Data = await PregaoB3Service.GetAllPagesAsync(getRequestData);
            return Ok(JsonSerializer.Serialize<PregaoB3GetResponseModel>(pregaoB3Data));
        }

        /// <summary>
        /// Executa scraping de dados da B3 via web scraping
        /// </summary>
        /// <returns>Dados obtidos via scraping em formato JSON</returns>
        /// <response code="200">Dados obtidos com sucesso via scraping</response>
        /// <response code="400">Erro durante o processo de scraping</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Executar scraping de dados da B3",
            Description = "Realiza web scraping do site da B3 para obter dados de pregão que podem não estar disponíveis via API oficial",
            OperationId = "GetPregaoB3ScrapedData",
            Tags = new[] { "Scraping B3" }
        )]
        [SwaggerResponse(200, "Dados obtidos com sucesso via scraping", typeof(string))]
        [SwaggerResponse(400, "Erro durante o processo de scraping")]
        public async Task<IActionResult> GetPregaoB3ScrapedDataAsync()
        {
            var scrapedData = await PregaoB3ScrapeService.ScrapeB3DataAsync();
            if (scrapedData == null || !scrapedData.Success)
            {
                return BadRequest(new { Message = scrapedData.Message });
            }
            return Ok(JsonSerializer.Serialize<ScrapeResultModel>(scrapedData));
        }

        /// <summary>
        /// Executa o pipeline completo de processamento de dados da B3
        /// </summary>
        /// <param name="pipelineService">Serviço de pipeline injetado via DI</param>
        /// <param name="date">Data específica para processamento (opcional, formato: yyyy-MM-dd)</param>
        /// <returns>Resultado da execução do pipeline</returns>
        /// <response code="200">Pipeline executado com sucesso</response>
        /// <response code="400">Erro durante a execução do pipeline</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Executar pipeline completo de dados da B3",
            Description = "Executa o pipeline completo que inclui: scraping de dados → conversão para formato Parquet → upload para AWS S3. Se nenhuma data for fornecida, processa a data atual.",
            OperationId = "ExecutePipeline",
            Tags = new[] { "Pipeline B3" }
        )]
        [SwaggerResponse(200, "Pipeline executado com sucesso")]
        [SwaggerResponse(400, "Erro durante a execução do pipeline")]
        public async Task<IActionResult> ExecutePipelineAsync([FromServices] B3PipelineService pipelineService, [FromQuery] string? date = null)
        {
            DateOnly? targetDate = null;
            if (!string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out var parsedDate))
            {
                targetDate = parsedDate;
            }

            var result = await pipelineService.ExecutePipelineAsync(targetDate);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }

    }
}
