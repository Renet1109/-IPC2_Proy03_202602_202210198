using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SatDte.Core;
using SatDte.Web.Servicios;

namespace SatDte.Web.Pages;
public sealed class PeticionesModel(ClienteApi api) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Tipo { get; set; } = "contribuyente";
    [BindProperty(SupportsGet = true)] public string? Nit { get; set; }
    [BindProperty(SupportsGet = true)] public string Sentido { get; set; } = "emitidas";
    [BindProperty(SupportsGet = true)] public string Inicio { get; set; } = DateTimeOffset.UtcNow.AddHours(-6).ToString("yyyy-MM-01");
    [BindProperty(SupportsGet = true)] public string Fin { get; set; } = DateTimeOffset.UtcNow.AddHours(-6).ToString("yyyy-MM-dd");
    [BindProperty(SupportsGet = true)] public string Fecha { get; set; } = DateTimeOffset.UtcNow.AddHours(-6).ToString("yyyy-MM-dd");
    [BindProperty(SupportsGet = true)] public bool SinIva { get; set; }
    [BindProperty(SupportsGet = true)] public bool Buscar { get; set; }
    public string? Error { get; set; }
    public ConsultaContribuyente? Consulta { get; set; }
    public ResumenIvaResultado? Iva { get; set; }
    public ResumenRangoResultado? Rango { get; set; }
    public ReporteErroresResultado? Errores { get; set; }
    public bool HayResultado => Consulta is not null || Iva is not null || Rango is not null || Errores is not null;
    public string Query => $"nit={Uri.EscapeDataString(Nit ?? "")}&tipo={Uri.EscapeDataString(Sentido ?? "emitidas")}&inicio={Uri.EscapeDataString(Inicio ?? "")}&fin={Uri.EscapeDataString(Fin ?? "")}&fecha={Uri.EscapeDataString(Fecha ?? "")}&sinIva={SinIva.ToString().ToLowerInvariant()}";
    public async Task OnGetAsync()
    {
        if (Tipo is not ("contribuyente" or "iva" or "rango" or "errores")) { Error = "Selecciona una petición válida."; return; }
        if (!Buscar) return;
        if (!ModelState.IsValid) { Error = "Revisa los parámetros de la consulta."; return; }
        try
        {
            switch (Tipo)
            {
                case "contribuyente": Consulta = await api.Obtener<ConsultaContribuyente>("ConsultaDatos?" + Query); break;
                case "iva": Iva = await api.Obtener<ResumenIvaResultado>("ResumenIva?" + Query); break;
                case "rango": Rango = await api.Obtener<ResumenRangoResultado>("ResumenRango?" + Query); break;
                case "errores": Errores = await api.Obtener<ReporteErroresResultado>("ReporteErrores?" + Query); break;
            }
        }
        catch (Exception ex) { Error = ClienteApi.Mensaje(ex); }
    }
    public async Task<IActionResult> OnGetPdfAsync()
    {
        try
        {
            var pdf = await api.Descargar($"Reportes/{Uri.EscapeDataString(Tipo)}.pdf?{Query}");
            return File(pdf.Datos, "application/pdf", $"reporte-{Tipo}.pdf");
        }
        catch (Exception ex) { Error = ClienteApi.Mensaje(ex); return Page(); }
    }
    public async Task<IActionResult> OnGetGraficaAsync()
    {
        try
        {
            string query = Query.Replace("tipo=" + Uri.EscapeDataString(Sentido), "tipo=" + Uri.EscapeDataString(Tipo), StringComparison.Ordinal);
            var svg = await api.Descargar("Grafica?" + query);
            return File(svg.Datos, "image/svg+xml");
        }
        catch (Exception ex) { return new ContentResult { StatusCode = 400, ContentType = "text/plain", Content = ClienteApi.Mensaje(ex) }; }
    }
}
