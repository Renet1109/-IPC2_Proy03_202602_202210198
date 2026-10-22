using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SatDte.Web.Pages;
public sealed class AyudaModel(IConfiguration configuracion, IWebHostEnvironment entorno) : PageModel
{
    public string Nombre => configuracion["Estudiante:Nombre"] ?? "";
    public string Carnet => configuracion["Estudiante:Carnet"] ?? "";
    public string Seccion => configuracion["Estudiante:Seccion"] ?? "";
    public IActionResult OnGetDocumento()
    {
        var ruta = Path.GetFullPath(Path.Combine(entorno.ContentRootPath, configuracion["DocumentacionPath"] ?? "../../docs/Documentacion.pdf"));
        return System.IO.File.Exists(ruta) ? PhysicalFile(ruta, "application/pdf", "Documentacion.pdf") : NotFound("La documentación no está disponible en la ruta configurada.");
    }
}
