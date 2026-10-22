using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SatDte.Core;
using SatDte.Web.Servicios;

namespace SatDte.Web.Pages;
public sealed class IndexModel(ClienteApi api) : PageModel
{
    public EstadoResumen? Estado { get; set; }
    public string? Error { get; set; }
    [TempData] public string? Aviso { get; set; }
    public async Task OnGetAsync()
    {
        try { Estado = await api.Obtener<EstadoResumen>("Estado"); }
        catch (Exception ex) { Error = ClienteApi.Mensaje(ex); }
    }
    public async Task<IActionResult> OnPostInicializarAsync()
    {
        try { await api.Inicializar(); Aviso = "El sistema está vacío y listo para recibir nuevos mensajes."; return RedirectToPage(); }
        catch (Exception ex) { Error = ClienteApi.Mensaje(ex); return Page(); }
    }
}
