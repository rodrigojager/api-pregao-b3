using TechChallenge.Models;
using TechChallenge.Services;

namespace TechChallenge.Services
{
    /// <summary>
    /// Serviço principal para executar o pipeline completo do Requisito 2:
    /// 1. Scraping dos dados da B3 (tenta scraping primeiro, depois API como fallback)
    /// 2. Conversão para o modelo Negociacao
    /// 3. Geração de arquivo Parquet
    /// 4. Upload para S3 com partição diária
    /// </summary>
    public sealed class B3PipelineService
    {
        private readonly ParquetWriterService _parquetWriter;
        private readonly S3UploaderService _s3Uploader;
        private readonly PregaoB3FetchService _pregaoService;
        private readonly ILogger<B3PipelineService> _logger;

        public B3PipelineService(
            ParquetWriterService parquetWriter,
            S3UploaderService s3Uploader,
            PregaoB3FetchService pregaoService,
            ILogger<B3PipelineService> logger)
        {
            _parquetWriter = parquetWriter;
            _s3Uploader = s3Uploader;
            _pregaoService = pregaoService;
            _logger = logger;
        }

        /// <summary>
        /// Executa o pipeline completo do Requisito 2
        /// </summary>
        public async Task<PipelineResultModel> ExecutePipelineAsync(DateOnly? targetDate = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                var dataDate = targetDate ?? DateOnly.FromDateTime(DateTime.Today);
                _logger.LogInformation("Iniciando pipeline para data: {DataDate}", dataDate.ToString("dd/MM/yyyy"));

                // 1. TENTAR OBTER DADOS VIA SCRAPING PRIMEIRO
                _logger.LogInformation("Tentando obter dados via scraping da B3...");
                var scrapedData = await PregaoB3ScrapeService.ScrapeB3DataAsync();
                
                IEnumerable<Negociacao> negociacoes;
                string dataSource = "";
                
                if (scrapedData?.Success == true && scrapedData.Data?.Any() == true)
                {
                    // Sucesso no scraping - converter dados do scraping
                    _logger.LogInformation("Scraping bem-sucedido! Convertendo {Count} registros do scraping...", scrapedData.Data.Count);
                    negociacoes = ConvertFromScrapedData(scrapedData.Data, dataDate);
                    dataSource = "Scraping";
                }
                else
                {
                    // Fallback para API da B3
                    _logger.LogInformation("Scraping falhou, tentando API da B3 como fallback...");
                    var getRequestData = _pregaoService.BuildPregaoB3GetRequestModel(1);
                    var pregaoData = await _pregaoService.GetAllPagesAsync(getRequestData);

                    if (pregaoData?.Results == null || !pregaoData.Results.Any())
                    {
                        return new PipelineResultModel
                        {
                            Success = false,
                            Message = "Nenhum dado obtido nem via scraping nem via API da B3",
                            RecordsProcessed = 0,
                            DataDate = dataDate,
                            ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                        };
                    }

                    // Converter dados da API
                    _logger.LogInformation("API bem-sucedida! Convertendo {Count} registros da API...", pregaoData.Results.Count);
                    negociacoes = ConvertFromApiData(pregaoData.Results, dataDate);
                    dataSource = "API";
                }

                if (!negociacoes.Any())
                {
                    return new PipelineResultModel
                    {
                        Success = false,
                        Message = "Nenhum registro válido encontrado após conversão",
                        RecordsProcessed = 0,
                        DataDate = dataDate,
                        ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                    };
                }

                // 3. GERAR ARQUIVO PARQUET
                _logger.LogInformation("Gerando arquivo Parquet...");
                var parquetPath = await _parquetWriter.WriteAsync(negociacoes, dataDate);

                // 4. UPLOAD PARA S3 COM PARTIÇÃO DIÁRIA
                _logger.LogInformation("Fazendo upload para S3...");
                var s3Key = await _s3Uploader.UploadAsync(parquetPath, dataDate);

                // 5. LIMPEZA
                if (File.Exists(parquetPath))
                {
                    File.Delete(parquetPath);
                    _logger.LogInformation("Arquivo temporário removido: {Path}", parquetPath);
                }

                stopwatch.Stop();
                _logger.LogInformation("Pipeline concluído com sucesso! {Records} registros processados via {DataSource} em {Time}ms", 
                    negociacoes.Count(), dataSource, stopwatch.ElapsedMilliseconds);

                return new PipelineResultModel
                {
                    Success = true,
                    Message = $"Pipeline executado com sucesso via {dataSource}",
                    RecordsProcessed = negociacoes.Count(),
                    DataDate = dataDate,
                    S3Key = s3Key,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    DataSource = dataSource
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Erro durante execução do pipeline");
                
                return new PipelineResultModel
                {
                    Success = false,
                    Message = $"Erro interno: {ex.Message}",
                    RecordsProcessed = 0,
                    DataDate = targetDate ?? DateOnly.FromDateTime(DateTime.Today),
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
        }

        /// <summary>
        /// Converte os dados do scraping da B3 para o modelo Negociacao
        /// </summary>
        private static IEnumerable<Negociacao> ConvertFromScrapedData(IEnumerable<IReadOnlyList<string>> scrapedData, DateOnly dataDate)
        {
            var negociacoes = new List<Negociacao>();
            
            foreach (var row in scrapedData)
            {
                try
                {
                    if (row.Count < 2) continue; // Precisa ter pelo menos 2 colunas
                    
                    var ticker = row[0]?.Trim();
                    if (string.IsNullOrEmpty(ticker)) continue;

                    // Busca valores numéricos nas colunas
                    decimal preco = 0;
                    long quantidade = 0;
                    
                    for (int i = 1; i < row.Count; i++)
                    {
                        var value = row[i]?.Trim();
                        if (string.IsNullOrEmpty(value)) continue;
                        
                        // Tenta encontrar preço (primeiro valor numérico válido)
                        if (preco == 0 && decimal.TryParse(value.Replace("%", "").Replace(",", "."), out var precoValue))
                        {
                            preco = precoValue;
                        }
                        // Tenta encontrar quantidade (segundo valor numérico válido)
                        else if (quantidade == 0 && long.TryParse(value.Replace(".", "").Replace(",", ""), out var qtyValue))
                        {
                            quantidade = qtyValue;
                        }
                    }

                    var negociacao = new Negociacao(
                        DataPregao: dataDate,
                        Ticker: ticker,
                        Preco: preco,
                        Quantidade: quantidade
                    );

                    negociacoes.Add(negociacao);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao converter linha do scraping: {ex.Message}");
                }
            }

            return negociacoes;
        }

        /// <summary>
        /// Converte os dados da API da B3 para o modelo Negociacao
        /// </summary>
        private static IEnumerable<Negociacao> ConvertFromApiData(IEnumerable<ResultItemGetResponseModel> apiData, DateOnly dataDate)
        {
            var negociacoes = new List<Negociacao>();
            
            foreach (var item in apiData)
            {
                try
                {
                                         // Mapeamento dos campos da API para o modelo Negociacao
                     var negociacao = new Negociacao(
                         DataPregao: dataDate,
                         Ticker: item.Cod ?? "N/A",
                         Preco: ParseDecimal(item.Part),
                         Quantidade: ParseLong(item.TheoricalQty)
                     );

                    negociacoes.Add(negociacao);
                }
                                 catch (Exception ex)
                 {
                     // Log do erro mas continua processando outros registros
                     Console.WriteLine($"Erro ao converter item {item?.Cod}: {ex.Message}");
                 }
            }

            return negociacoes;
        }

        private static decimal ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            
            // Remove caracteres especiais e converte vírgula para ponto
            var cleanValue = value.Replace("R$", "").Replace(".", "").Replace(",", ".").Trim();
            
            if (decimal.TryParse(cleanValue, out var result))
                return result;
            
            return 0;
        }

        private static int ParseInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            
            // Remove caracteres especiais
            var cleanValue = value.Replace(".", "").Replace(",", "").Trim();
            
            if (int.TryParse(cleanValue, out var result))
                return result;
            
            return 0;
        }

        private static long ParseLong(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            
            // Remove caracteres especiais
            var cleanValue = value.Replace(".", "").Replace(",", "").Trim();
            
            if (long.TryParse(cleanValue, out var result))
                return result;
            
            return 0;
        }
    }

    /// <summary>
    /// Modelo de resultado do pipeline
    /// </summary>
    public class PipelineResultModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int RecordsProcessed { get; set; }
        public DateOnly? DataDate { get; set; }
        public string? S3Key { get; set; }
        public long ProcessingTimeMs { get; set; }
        public string? DataSource { get; set; }
    }
} 