using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SatDte.Web.Servicios;

namespace SatDte.Web.Pages;
public sealed record ResultadoArchivo(string Nombre, bool Correcto, string Mensaje);
public sealed class EnviarModel(ClienteApi api) : PageModel
{
    [BindProperty] public List<IFormFile> Archivos { get; set; } = [];
    public List<ResultadoArchivo> Resultados { get; } = [];
    public string? Error { get; set; }
    public async Task OnPostAsync()
    {
        if (Archivos.Count is < 1 or > 20) { Error = "Selecciona de 1 a 20 archivos JSON."; return; }
        foreach (var archivo in Archivos)
        {
            if (!Path.GetExtension(archivo.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase) || archivo.Length is < 1 or > 5 * 1024 * 1024)
            { Resultados.Add(new(archivo.FileName, false, "Debe ser un archivo .json no vacío de hasta 5 MB.")); continue; }
            try
            {
                using var stream = archivo.OpenReadStream();
                Resultados.Add(new(archivo.FileName, true, await api.Procesar(stream)));
            }
            catch (Exception ex) { Resultados.Add(new(archivo.FileName, false, ClienteApi.Mensaje(ex))); }
        }
    }
}
