using Amazon.S3;
using Amazon.S3.Transfer;

namespace TechChallenge.Services
{
    /// <summary>Faz upload do Parquet para s3://{bucket}/raw/dt=YYYY-MM-DD/...</summary>
    public sealed class S3UploaderService
    {
        private readonly IAmazonS3 _s3;
        private readonly string _bucket;          // ← campo visível em toda a classe

        public S3UploaderService(IAmazonS3 s3, IConfiguration configuration)
        {
            _s3 = s3;
            _bucket = configuration["Aws:Bucket"]
                      ?? throw new InvalidOperationException("Aws:Bucket não configurado");
        }

        public async Task<string> UploadAsync(string localFile, DateOnly partDate,
                                              CancellationToken ct = default)
        {
            var key = $"raw/dt={partDate:yyyy-MM-dd}/{Path.GetFileName(localFile)}";

            var transfer = new TransferUtility(_s3);
            await transfer.UploadAsync(localFile, _bucket, key, ct);

            return key;
        }
    }
}
