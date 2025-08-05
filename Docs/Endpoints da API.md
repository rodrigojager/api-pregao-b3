# Endpoints da API

## Visão Geral

Esta documentação descreve todos os endpoints disponíveis no `APIController` da aplicação TechChallenge. A API fornece funcionalidades para obter e processar dados de pregão da B3 (Bolsa de Valores do Brasil). Tem 2 endpoints acessíveis pelo método *GET* para obter os dados do pregão (GetPregaoB3Data e GetPregaoB3ScrapedData) e um endpoint para executar o pipeline completo de processamento de dados da B3, que inclui: obtenção de dados (scraping e caso não consiga, via `API` da B3), conversão para modelo interno de dados, geração de arquivo Parquet e upload para AWS S3 com partição diária. Os passos posteriores não são gerenciados pela API e sim por uma funçao AWS Lambda que é disparada assim que os dados brutos chegam no bucket S3. Essa função Lambda chama um job ETL no AWS Glue, executa várias transformações e salvam os dados em formato parquet numa pasta chamada `refined`, particionados por data e pela abreviação da ação do pregão. Esses dados são catalogados no Glue Catalog e entra em um banco de dados disponível no Athena.

---

## GetPregaoB3DataAsync

Busca dados de pregão da B3 através da API da B3, recuperando informações completas de negociação, volumes e preços de todos os ativos do índice IBOV.
Essa API da B3 foi descoberta após análise das requisições feitas pela página da própria B3 que monta um json como a seguir para solicitar as informações e faz um encode dessas informações para Base64:

```json
{
    "language": "pt-br",
    "pageNumber": 1,
    "pageSize": 20,
    "index": "IBOV",
    "segment": "1"
}
```
Esse json ao virar uma string em Base64, é concatenado ao final da url https://sistemaswebb3-listados.b3.com.br/indexProxy/indexCall/GetPortfolioDay/ e usado em uma requisição `GET`. Foi criado um método que tenta solicitar uma quantidade extremamente grande de dados por vez (o limite de INT em algumas linguagens), para que venha tudo em uma única página, mas se existirem mais páginas, ocorre um loop até o fim, adicionando os resultados em uma lista.

### Detalhes Técnicos
- **Método HTTP**: `GET`
- **Rota**: `/api/GetPregaoB3Data`
- **Content-Type**: `application/json`

### Parâmetros de Entrada
Nenhum parâmetro é necessário. O endpoint utiliza configurações padrão:
- **Índice**: IBOV
- **Segmento**: 1
- **Idioma**: pt-br
- **Tamanho da página**: 99999 registros

### Parâmetros de Saída
**Sucesso (200 OK)**:
```json
{
  "page": {
    "pageNumber": 1,
    "pageSize": 99999,
    "totalRecords": 1234,
    "totalPages": 1
  },
  "header": {
    "date": "2024-01-15",
    "text": "Dados do Pregão",
    "part": "100.00",
    "partAcum": "100.00",
    "textReductor": "Redutor",
    "reductor": "1.0000",
    "theoricalQty": "123456789"
  },
  "results": [
    {
      "segment": "1",
      "cod": "PETR4",
      "asset": "PETROBRAS PN",
      "type": "AÇÃO",
      "part": "15.23",
      "partAcum": "15.23",
      "theoricalQty": "12345678"
    }
  ]
}
```

**Erro (500 Internal Server Error)**:
```json
{
  "message": "Erro interno do servidor"
}
```

### Funcionamento Interno

1. **Inicialização do Serviço**: Cria uma instância do `PregaoB3FetchService` com `HttpClient`
2. **Construção da Requisição**: Utiliza `BuildPregaoB3GetRequestModel(1)` para criar o modelo de requisição
3. **Busca de Dados**: Chama `GetAllPagesAsync()` que:
   - Faz requisição para a API oficial da B3: `https://sistemaswebb3-listados.b3.com.br/indexProxy/indexCall/GetPortfolioDay/`
   - Codifica os parâmetros em Base64URL
   - Busca todas as páginas automaticamente (paginação)
   - Combina todos os resultados em uma única resposta
4. **Serialização**: Converte o resultado para JSON
5. **Retorno**: Retorna os dados completos do pregão

### Serviços Utilizados
- **PregaoB3FetchService**: Responsável pela comunicação com a API oficial da B3
- **Base64UrlHelper**: Para codificação dos parâmetros da requisição

---

## GetPregaoB3ScrapedDataAsync

Executa web scraping do site oficial da B3 para obter dados de pregão que podem não estar disponíveis através da API oficial. Utiliza Playwright para automatizar a navegação e extração de dados.

### Detalhes Técnicos
- **Método HTTP**: `GET`
- **Rota**: `/api/GetPregaoB3ScrapedData`
- **Content-Type**: `application/json`

### Parâmetros de Entrada
Nenhum parâmetro é necessário.

### Parâmetros de Saída
**Sucesso (200 OK)**:
```json
{
  "success": true,
  "message": "Sucesso",
  "data": [
    ["PETR4", "15.23", "1.2%", "12345678", "15.45"],
    ["VALE3", "67.89", "0.8%", "9876543", "68.12"]
  ],
  "totalRecords": 1234,
  "date": "15/01/2024"
}
```

**Erro (400 Bad Request)**:
```json
{
  "message": "Erro durante o processo de scraping"
}
```

### Funcionamento Interno

1. **Inicialização do Playwright**: Cria uma instância do navegador Chromium em modo headless
2. **Navegação**: Acessa a URL: `https://sistemaswebb3-listados.b3.com.br/indexPage/day/IBOV?language=pt-br`
3. **Aguardar Carregamento**: Espera a tabela de dados carregar completamente
4. **Extração de Metadados**: 
   - Obtém o número total de páginas
   - Extrai a data do pregão
5. **Processamento por Página**:
   - Para cada página:
     - Extrai todas as linhas da tabela
     - Captura valores de todas as colunas
     - Navega para a próxima página até completar
6. **Limpeza de Dados**: Remove espaços em branco e formata os dados
7. **Retorno**: Retorna os dados estruturados com metadados

### Serviços Utilizados
- **PregaoB3ScrapeService**: Serviço estático que executa o web scraping
- **Playwright**: Framework para automação de navegador

### Seletores CSS Utilizados
- **Tabela**: `table.table.table-responsive-sm.table-responsive-md`
- **Linhas**: `tbody > tr`
- **Colunas**: `td`
- **Paginação**: `ul.ngx-pagination`
- **Data**: `form.ng-untouched.ng-pristine.ng-valid > h2`

---

## ExecutePipelineAsync

Executa o pipeline completo de processamento de dados da B3, que inclui: obtenção de dados (primeiro tenta via scraping, mas em caso de erro, utiliza a API), conversão para modelo interno, geração de arquivo Parquet e upload para AWS S3 com partição diária.

### Detalhes Técnicos
- **Método HTTP**: `POST`
- **Rota**: `/api/ExecutePipeline`
- **Content-Type**: `application/json`

### Parâmetros de Entrada

**Query Parameter**:
- **date** (opcional): Data específica para processamento no formato `yyyy-MM-dd`
  - Exemplo: `?date=2024-01-15`
  - Se não fornecido, utiliza a data atual

**Dependency Injection**:
- **B3PipelineService**: Serviço injetado via DI container

### Parâmetros de Saída
**Sucesso (200 OK)**:
```json
{
  "success": true,
  "message": "Pipeline executado com sucesso via Scraping",
  "recordsProcessed": 1234,
  "dataDate": "2024-01-15",
  "s3Key": "raw/dt=2024-01-15/b3_20240115.parquet",
  "processingTimeMs": 15420,
  "dataSource": "Scraping"
}
```

**Erro (400 Bad Request)**:
```json
{
  "success": false,
  "message": "Nenhum dado obtido nem via scraping nem via API da B3",
  "recordsProcessed": 0,
  "dataDate": "2024-01-15",
  "processingTimeMs": 15420
}
```

### Funcionamento Interno

1. **Obtenção de Dados (Estratégia de Fallback)**
   - **Primeira tentativa**: Web scraping via `PregaoB3ScrapeService.ScrapeB3DataAsync()`
   - **Fallback**: Se scraping falhar, utiliza API oficial via `PregaoB3FetchService`

2. **Conversão de Dados**
   - **Dados do Scraping**: Converte tabela HTML para modelo `Negociacao`
   - **Dados da API**: Converte resposta JSON para modelo `Negociacao`
   - **Mapeamento**:
     - `DataPregao`: Data fornecida ou atual
     - `Ticker`: Código do ativo
     - `Preco`: Preço de fechamento
     - `Quantidade`: Volume negociado

3. **Geração de Arquivo Parquet**
   - **Serviço**: `ParquetWriterService`
   - **Localização**: Pasta temporária configurada
   - **Nome do arquivo**: `b3_YYYYMMDD.parquet`
   - **Formato**: Apache Parquet otimizado para big data

4. **Upload para AWS S3**
   - **Serviço**: `S3UploaderService`
   - **Bucket**: Configurado via `Aws:Bucket`
   - **Estrutura de partição**: `raw/dt=YYYY-MM-DD/`
   - **Chave S3**: `raw/dt=2024-01-15/b3_20240115.parquet`

5. **Limpeza**
   - Remove arquivo temporário local após upload bem-sucedido

### Serviços Utilizados
- **B3PipelineService**: Orquestrador principal do pipeline
- **PregaoB3ScrapeService**: Web scraping da B3
- **PregaoB3FetchService**: API oficial da B3
- **ParquetWriterService**: Geração de arquivos Parquet
- **S3UploaderService**: Upload para AWS S3

## Modelo de Dados

- **Negociacao**
```csharp
public readonly record struct Negociacao(
    DateOnly DataPregao,    // Data do pregão
    string Ticker,          // Código do ativo (ex: PETR4)
    decimal Preco,          // Preço de fechamento
    long Quantidade         // Volume negociado
);
```

- **PipelineResultModel**
```csharp
public class PipelineResultModel
{
    public bool Success { get; set; }           // Status da execução
    public string Message { get; set; }         // Mensagem descritiva
    public int RecordsProcessed { get; set; }   // Número de registros processados
    public DateOnly? DataDate { get; set; }     // Data dos dados
    public string? S3Key { get; set; }          // Chave do arquivo no S3
    public long ProcessingTimeMs { get; set; }  // Tempo de processamento
    public string? DataSource { get; set; }     // Fonte dos dados (Scraping/API)
}
```
