using System.Net.Http.Headers;
using System.Text.Json;

namespace SatDte.Web.Servicios;

public sealed class ClienteApi(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public async Task<T> Obtener<T>(string ruta)
    {
        using var respuesta = await http.GetAsync(ruta);
        await Verificar(respuesta);
        return await respuesta.Content.ReadFromJsonAsync<T>(Json) ?? throw new InvalidOperationException("La API devolvió una respuesta vacía.");
    }
    public async Task<(byte[] Datos, string Tipo)> Descargar(string ruta)
    {
        using var respuesta = await http.GetAsync(ruta);
        await Verificar(respuesta);
        return (await respuesta.Content.ReadAsByteArrayAsync(), respuesta.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
    }
    public async Task<string> Procesar(Stream contenido)
    {
        using var cuerpo = new StreamContent(contenido);
        cuerpo.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var respuesta = await http.PostAsync("Procesar", cuerpo);
        await Verificar(respuesta);
        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return JsonSerializer.Serialize(json.RootElement, new JsonSerializerOptions { WriteIndented = true });
    }
    public async Task Inicializar()
    {
        using var respuesta = await http.PostAsync("Inicializar", null);
        await Verificar(respuesta);
    }
    private static async Task Verificar(HttpResponseMessage respuesta)
    {
        if (respuesta.IsSuccessStatusCode) return;
        string texto = await respuesta.Content.ReadAsStringAsync();
        try
        {
            using var json = JsonDocument.Parse(texto);
            if (json.RootElement.TryGetProperty("error", out var error)) texto = error.GetString() ?? texto;
        }
        catch (JsonException) { texto = "Respuesta HTTP " + (int)respuesta.StatusCode; }
        throw new InvalidOperationException(texto);
    }
    public static string Mensaje(Exception ex) => ex switch
    {
        HttpRequestException => "No fue posible conectar con la API. Verifica que esté ejecutándose y revisa Api:BaseUrl.",
        TaskCanceledException => "La API tardó demasiado en responder. Comprueba su estado antes de repetir el envío.",
        _ => ex.Message
    };
}
