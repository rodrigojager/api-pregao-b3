using Parquet.Serialization;
using TechChallenge.Models;

namespace TechChallenge.Services
{
    /// <summary>Gera arquivo Parquet em pasta temporária.</summary>
    public sealed class ParquetWriterService
    {
        private readonly string _tempFolder;

        public ParquetWriterService(IConfiguration cfg)
        {
            _tempFolder = cfg["Parquet:TempFolder"] ?? Path.GetTempPath();
            Directory.CreateDirectory(_tempFolder);
        }

        public async Task<string> WriteAsync(IEnumerable<Negociacao> linhas,
                                             DateOnly partDate,
                                             CancellationToken ct = default)
        {
            string fileName = $"b3_{partDate:yyyyMMdd}.parquet";
            string fullPath = Path.Combine(_tempFolder, fileName);

            await using var fs = File.Create(fullPath, 1 << 16,
                                             FileOptions.Asynchronous | FileOptions.SequentialScan);

            await ParquetSerializer.SerializeAsync(linhas, fs, cancellationToken: ct);
            return fullPath;
        }
    }
}
