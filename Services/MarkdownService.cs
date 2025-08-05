using Markdig;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Text;

namespace TechChallenge.Services
{
    /// <summary>Converte Markdown para HTML e lista arquivos .md na pasta Docs.</summary>
    public class MarkdownService
    {
        private readonly string _docsPath;
        private readonly MarkdownPipeline _pipeline;
        private readonly ILogger<MarkdownService> _logger;

        public MarkdownService(string docsPath, ILogger<MarkdownService> logger = null)
        {
            _docsPath = docsPath;
            _logger = logger;
            _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            
            _logger?.LogInformation("MarkdownService inicializado com caminho: {DocsPath}", _docsPath);
            _logger?.LogInformation("Pasta Docs existe: {Exists}", Directory.Exists(_docsPath));
            
            if (Directory.Exists(_docsPath))
            {
                var files = Directory.GetFiles(_docsPath, "*.md");
                _logger?.LogInformation("Arquivos .md encontrados: {Count}", files.Length);
                foreach (var file in files)
                {
                    _logger?.LogInformation("Arquivo encontrado: {File}", Path.GetFileName(file));
                }
            }
        }

        // Retorna lista de nomes de arquivo (sem extensão) ordenados alfabeticamente.
        public IEnumerable<string> ListDocs()
        {
            if (!Directory.Exists(_docsPath))
            {
                _logger?.LogWarning("Pasta Docs não encontrada: {DocsPath}", _docsPath);
                return Enumerable.Empty<string>();
            }

            try
            {
                // Força encoding UTF-8 para leitura dos nomes dos arquivos
                var files = Directory.GetFiles(_docsPath, "*.md")
                                    .Select(filePath => 
                                    {
                                        var fileName = Path.GetFileNameWithoutExtension(filePath);
                                        // Tenta corrigir encoding se necessário
                                        if (fileName.Contains("├") || fileName.Contains("з") || fileName.Contains("г"))
                                        {
                                            _logger?.LogWarning("Nome de arquivo com encoding incorreto detectado: {FileName}", fileName);
                                            // Tenta encontrar o arquivo correto
                                            var correctName = FindCorrectFileName(filePath);
                                            if (!string.IsNullOrEmpty(correctName))
                                            {
                                                _logger?.LogInformation("Nome corrigido: {CorrectName}", correctName);
                                                return correctName;
                                            }
                                        }
                                        return fileName;
                                    })
                                    .OrderBy(n => n)
                                    .ToList();
                
                _logger?.LogInformation("Lista de documentos encontrados: {Count} arquivos", files.Count);
                foreach (var file in files)
                {
                    _logger?.LogInformation("Nome do arquivo: {FileName}", file);
                }
                return files;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao listar documentos");
                return Enumerable.Empty<string>();
            }
        }

        private string FindCorrectFileName(string filePath)
        {
            try
            {
                // Lê o arquivo com encoding UTF-8 e tenta extrair informações
                var content = System.IO.File.ReadAllText(filePath, Encoding.UTF8);
                
                // Se o arquivo tem um título no markdown, usa ele
                var lines = content.Split('\n');
                foreach (var line in lines)
                {
                    if (line.StartsWith("# "))
                    {
                        var title = line.Substring(2).Trim();
                        if (!string.IsNullOrEmpty(title))
                        {
                            return title;
                        }
                    }
                }
                
                // Se não encontrou título, usa o nome do arquivo original
                return Path.GetFileNameWithoutExtension(filePath);
            }
            catch
            {
                return Path.GetFileNameWithoutExtension(filePath);
            }
        }

        // Lê o arquivo de nome dado (sem extensão) e devolve HTML pronto.
        public string RenderHtml(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                _logger?.LogWarning("Nome do documento é nulo ou vazio");
                return "<p>Nome do documento não especificado.</p>";
            }

            var file = Path.Combine(_docsPath, $"{name}.md");
            _logger?.LogInformation("Tentando carregar arquivo: {File}", file);
            
            if (!System.IO.File.Exists(file))
            {
                _logger?.LogWarning("Arquivo não encontrado: {File}", file);
                return $"<p>Documento '{name}' não encontrado.</p><p>Caminho tentado: {file}</p>";
            }

            try
            {
                var md = System.IO.File.ReadAllText(file, Encoding.UTF8);
                _logger?.LogInformation("Arquivo carregado com sucesso: {File}, tamanho: {Size} caracteres", file, md.Length);
                return Markdown.ToHtml(md, _pipeline);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao ler arquivo: {File}", file);
                return $"<p>Erro ao ler documento '{name}': {ex.Message}</p>";
            }
        }
    }
}
