using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using TechChallenge.Models;

namespace TechChallenge.Helpers
{
    public static class Base64UrlHelper
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
        // Recebe parametros usados pela requisição ao backend do pregão da B3 e gera a base64 do json correspondente para ser concatenado na URL ao fazer o GET request.
        public static string EncodeToBase64Url(PregaoB3GetRequestModel request)
        {
            var json = JsonSerializer.Serialize(request, _options);

            var bytes = Encoding.UTF8.GetBytes(json);

            return WebEncoders.Base64UrlEncode(bytes);
        }
    }
}
