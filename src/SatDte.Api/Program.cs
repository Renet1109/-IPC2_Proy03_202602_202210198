using System.Text.Json;
using SatDte.Core;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 10 * 1024 * 1024);
builder.Services.AddSingleton<IReloj, RelojGuatemala>();
builder.Services.AddSingleton<IRepositorio>(_ => new RepositorioJson(Path.Combine(builder.Environment.ContentRootPath,
    builder.Configuration["Datos:Archivo"] ?? "App_Data/estado.json")));
builder.Services.AddSingleton<ServicioDte>();
builder.Services.AddSingleton<ServicioConsultas>();
var app = builder.Build();
// Carga inmediata: un archivo corrupto nunca se reemplaza silenciosamente por un estado vacío.
_ = app.Services.GetRequiredService<IRepositorio>();
app.Use(async (contexto, siguiente) =>
{
    try { await siguiente(); }
    catch (Exception ex) when (ex is SolicitudInvalidaException or JsonException or BadHttpRequestException)
    {
        contexto.Response.StatusCode = ex is BadHttpRequestException bh ? bh.StatusCode : 400;
        await contexto.Response.WriteAsJsonAsync(new { error = ex is JsonException ? "JSON malformado. Verifica comas, comillas y corchetes." : ex.Message });
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        app.Logger.LogError(ex, "No se pudo acceder a la persistencia");
        contexto.Response.StatusCode = 503;
        await contexto.Response.WriteAsJsonAsync(new { error = "No fue posible guardar o leer los datos. El lote no fue confirmado." });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error al atender la solicitud");
        contexto.Response.StatusCode = 500;
        await contexto.Response.WriteAsJsonAsync(new { error = "Ocurrió un error interno. Revisa el registro del servidor." });
    }
});

app.MapGet("/", () => Results.Ok(new { servicio = "SAT / DTE - Proyecto 3 IPC2", estudiante = "202210198",
    endpoints = new[] { "POST /Procesar", "POST /Inicializar", "GET /Estado", "GET /Contribuyentes", "GET /ConsultaDatos", "GET /ResumenIva", "GET /ResumenRango", "GET /ReporteErrores", "GET /Grafica", "GET /Reportes/{tipo}.pdf" } }));
app.MapGet("/salud", () => Results.Ok(new { estado = "disponible" }));
app.MapPost("/Procesar", async (HttpRequest solicitud, ServicioDte servicio) =>
{
    if (!solicitud.HasJsonContentType()) return Results.Json(new { error = "Usa Content-Type: application/json." }, statusCode: 415);
    using var documento = await JsonDocument.ParseAsync(solicitud.Body, new JsonDocumentOptions { MaxDepth = 32 }, solicitud.HttpContext.RequestAborted);
    return Results.Json(servicio.Procesar(documento.RootElement));
});
app.MapPost("/Inicializar", (ServicioDte servicio) => { servicio.Inicializar(); return Results.Ok(new { mensaje = "Sistema inicializado. Todos los datos fueron eliminados." }); });
app.MapGet("/Estado", (ServicioDte servicio) => servicio.Estado());
app.MapGet("/Contribuyentes", (ServicioDte servicio) => servicio.Contribuyentes());

static string Param(HttpRequest r, string clave, string? defecto = null) => r.Query[clave].FirstOrDefault()
    ?? defecto ?? throw new SolicitudInvalidaException($"Falta el parámetro {clave}.");
static DateOnly Fecha(HttpRequest r, string clave) => Validaciones.FechaConsulta(Param(r, clave));
static bool SinIva(HttpRequest r)
{
    if (bool.TryParse(Param(r, "sinIva", "false"), out bool valor)) return valor;
    throw new SolicitudInvalidaException("sinIva debe ser true o false.");
}
static ConsultaContribuyente Consulta(HttpRequest r, ServicioConsultas s) => s.Contribuyente(Param(r, "nit"), Param(r, "tipo", "emitidas"), Fecha(r, "inicio"), Fecha(r, "fin"));

app.MapGet("/ConsultaDatos", (HttpRequest r, ServicioConsultas s) => Consulta(r, s));
app.MapGet("/ResumenIva", (HttpRequest r, ServicioConsultas s) => s.Iva(Fecha(r, "fecha")));
app.MapGet("/ResumenRango", (HttpRequest r, ServicioConsultas s) => s.Rango(Fecha(r, "inicio"), Fecha(r, "fin"), SinIva(r)));
app.MapGet("/ReporteErrores", (HttpRequest r, ServicioConsultas s) => s.Errores(Fecha(r, "inicio"), Fecha(r, "fin")));
app.MapGet("/Reportes/{tipo}.pdf", (string tipo, HttpRequest r, ServicioConsultas s) =>
{
    byte[] pdf = tipo.ToLowerInvariant() switch
    {
        "contribuyente" => Reportes.Contribuyente(Consulta(r, s)),
        "iva" => Reportes.Iva(s.Iva(Fecha(r, "fecha"))),
        "rango" => Reportes.Rango(s.Rango(Fecha(r, "inicio"), Fecha(r, "fin"), SinIva(r))),
        "errores" => Reportes.Errores(s.Errores(Fecha(r, "inicio"), Fecha(r, "fin"))),
        _ => throw new SolicitudInvalidaException("Reporte desconocido. Usa contribuyente, iva, rango o errores.")
    };
    return Results.File(pdf, "application/pdf", $"reporte-{tipo}.pdf");
});
app.MapGet("/Grafica", (HttpRequest r, ServicioConsultas s) =>
{
    string svg;
    switch (Param(r, "tipo"))
    {
        case "iva":
            var iva = s.Iva(Fecha(r, "fecha"));
            svg = GraficaSvg.Crear("IVA por NIT - " + iva.Fecha, iva.Movimientos.Select(m => new Barra(m.Nit, m.IvaEmitido, m.IvaRecibido)).ToArray(), "IVA emitido", "IVA recibido");
            break;
        case "rango":
            var rango = s.Rango(Fecha(r, "inicio"), Fecha(r, "fin"), SinIva(r));
            svg = GraficaSvg.Crear("Valor autorizado por fecha", rango.Dias.Select(d => new Barra(d.Fecha, d.ValorAutorizado)).ToArray(), rango.SinIva ? "Valor sin IVA" : "Total con IVA");
            break;
        default: throw new SolicitudInvalidaException("La gráfica debe ser de tipo iva o rango.");
    }
    return Results.Text(svg, "image/svg+xml");
});
app.Run();
