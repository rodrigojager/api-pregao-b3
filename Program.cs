using Amazon.Extensions.NETCore.Setup;
using Amazon.S3;
using Swashbuckle.AspNetCore.SwaggerUI;
using TechChallenge.Services;

var builder = WebApplication.CreateBuilder(args);

//--------------------------------------------------------------------------
// 1) SERVIÇOS
//--------------------------------------------------------------------------

builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddScoped<S3UploaderService>();
builder.Services.AddScoped<ParquetWriterService>();
builder.Services.AddScoped<PregaoB3FetchService>();
builder.Services.AddScoped<B3PipelineService>();
// Registrar MarkdownService com caminho da pasta Docs
builder.Services.AddScoped<MarkdownService>(provider =>
{
    var env = provider.GetRequiredService<IWebHostEnvironment>();
    var logger = provider.GetRequiredService<ILogger<MarkdownService>>();
    var docsPath = Path.Combine(env.ContentRootPath, "Docs");
    return new MarkdownService(docsPath, logger);
});
builder.Services.AddHttpClient();

// MVC + API
builder.Services.AddControllersWithViews();

// SWAGGER / OPENAPI  (NUGET: Swashbuckle.AspNetCore)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
});

// CORS
const string CorsPolicy = "FrontendCors";
builder.Services.AddCors(opts =>
{
    opts.AddPolicy(CorsPolicy, p => p
        // *** EM PRODUÇÃO TROQUE POR .WithOrigins("https://app.seudominio.com") ***
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()
    );
});

var app = builder.Build();

//--------------------------------------------------------------------------
// 2) PIPELINE DE MIDDLEWARE
//--------------------------------------------------------------------------

//if (app.Environment.IsDevelopment())
//{
    // PÁGINA DE ERRO + SWAGGER NO DEV
    app.UseDeveloperExceptionPage();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
        c.ConfigObject.AdditionalItems["requestSnippetsEnabled"] = true;
        // ABRE DIRETO NA RAIZ /swagger
        c.RoutePrefix = "swagger";
        // 1) SE DESEJAR SUBSTITUIR O INDEX TOTAL:
        c.IndexStream = () => File.OpenRead("wwwroot/swagger/index.html");

        // 2) CASO USE O INDEX PADRÃO MAS QUER SÃ INJETAR CSS/JS:
        c.InjectStylesheet("/swagger/custom-swagger.css");
        c.InjectJavascript("/swagger/custom-swagger.js");
        // TÍTULO DA ABA
        c.DocumentTitle = "API DOCS";
        // OCULTA VERSÃO NO RODAPÉ, ETC.
        c.ConfigObject.AdditionalItems["displayRequestDuration"] = true;
    });
//}
//else
//{
//    app.UseExceptionHandler("/Home/Error");
//    app.UseHsts();
//}

// HTTPS + ARQUIVOS ESTÁTICOS
app.UseHttpsRedirection();
app.UseStaticFiles();

// ROTEAMENTO
app.UseRouting();

// CORS
app.UseCors(CorsPolicy);

//--------------------------------------------------------------------------
// 3) ENDPOINTS
//--------------------------------------------------------------------------

// ROTAS MVC CONVENCIONAIS
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Docs}/{action=Index}/{id?}")
    .WithStaticAssets();

// ATRIBUTO ROUTING API CONTROLLERS
app.MapControllers();

// ESTÁTICOS VIA Manifests (se estiver usando)
app.MapStaticAssets();

app.Run();

