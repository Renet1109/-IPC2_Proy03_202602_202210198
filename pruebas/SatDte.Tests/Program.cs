using System.Text.Json;
using SatDte.Core;

int pasaron = 0, fallaron = 0;
void Probar(string nombre, Action prueba)
{
    try { prueba(); pasaron++; Console.WriteLine("OK   " + nombre); }
    catch (Exception ex) { fallaron++; Console.WriteLine("FAIL " + nombre + ": " + ex.Message); }
}
void Igual<T>(T esperado, T actual)
{ if (!EqualityComparer<T>.Default.Equals(esperado, actual)) throw new Exception($"Esperado {esperado}; obtenido {actual}"); }
void Verdad(bool valor) { if (!valor) throw new Exception("Condición falsa"); }
void Invalida(Action accion)
{ try { accion(); } catch (SolicitudInvalidaException) { return; } throw new Exception("Se esperaba SolicitudInvalidaException"); }
var entrada = Path.GetFullPath(args.FirstOrDefault() ?? "pruebas/entrada");
var ruta = Path.Combine(Path.GetFullPath(args.ElementAtOrDefault(1) ?? Path.GetTempPath()), "SatDte-tests-" + Guid.NewGuid().ToString("N"), "estado.json");
var reloj = new RelojPrueba { Ahora = new DateTimeOffset(2026, 10, 22, 23, 59, 0, TimeSpan.FromHours(-6)) };
JsonElement Mensaje(string s) { using var d = JsonDocument.Parse(s); return d.RootElement.Clone(); }
JsonElement Respuesta(object o) => JsonSerializer.SerializeToElement(o);
JsonElement Factura(string referencia) => Mensaje("[{\"TIEMPO\":\"01/03/2025 10:00\",\"REFERENCIA\":\"" + referencia + "\",\"NIT_EMISOR\":\"7378106\",\"NIT_RECEPTOR\":\"8338817\",\"VALOR\":100,\"IVA\":12,\"TOTAL\":112}]");
using (var repo = new RepositorioJson(ruta))
{
    var servicio = new ServicioDte(repo, reloj);
    var consulta = new ServicioConsultas(repo);
    JsonElement Cargar(string nombre) => Respuesta(servicio.Procesar(Mensaje(File.ReadAllText(Path.Combine(entrada, nombre)))));
    Probar("NIT: módulo 11, K, base de 20 dígitos y minúsculas", () =>
    { foreach (var n in new[] { "7378106", "8338817", "6K", "6k", "123456789012345678908" }) Verdad(Validaciones.NitValido(n)); });
    Probar("NIT: verificador, letras y longitudes inválidas", () =>
    { foreach (var n in new[] { "5123456", "12A4", "", "K", "12345678901234567890008" }) Verdad(!Validaciones.NitValido(n)); });
    Probar("Fechas: bisiesto, adornos, hora y calendario", () =>
    {
        Verdad(Validaciones.FechaFactura("Antigua, 29/02/2024 9:05 hrs.", out _));
        foreach (var f in new[] { "29/02/2025 10:00", "01/01/2025", "01/01/2025 24:00", "01/01/2025 12:60", "31/04/2025 10:00" }) Verdad(!Validaciones.FechaFactura(f, out _));
    });
    Probar("IVA decimal: montos extremos", () => { Igual(0m, Validaciones.Iva(.04m)); Igual(1.21m, Validaciones.Iva(10.05m)); Igual(12000000m, Validaciones.Iva(99999999.99m)); });
    Probar("01 RTU: 9 nuevos, 1 actualizado, 3 inválidos", () =>
    {
        var r = Cargar("01_rtu_completo.json"); Igual(9, r.GetProperty("CONTRIBUYENTES_NUEVOS").GetInt32());
        Igual(1, r.GetProperty("CONTRIBUYENTES_ACTUALIZADOS").GetInt32()); Igual(3, r.GetProperty("NITS_INVALIDOS").GetInt32());
        Verdad(servicio.Contribuyentes().Single(c => c.NIT == "7378106").Nombre.Contains("actualizada"));
    });
    Probar("02 Marzo: 4 aprobaciones y correlativos", () =>
    {
        var r = Cargar("02_facturas_marzo_graficas.json").GetProperty("resultadoAutorizaciones");
        Igual(4, r.GetProperty("cantidadAutorizacionesAprobadas").GetInt32());
        Igual(2026102200000001L, r.GetProperty("detalleAprobaciones")[0].GetProperty("codigoAprobacion").GetInt64());
    });
    Probar("Consulta emitidas: Q 9672.32 y 2 facturas", () => { var r = consulta.Contribuyente("999999K", "emitidas", new(2025, 3, 1), new(2025, 3, 2)); Igual(9672.32m, r.TotalFacturado); Igual(2, r.CantidadFacturas); Igual(5061.13m, r.Dias[0].MontoTotal); });
    Probar("Consulta recibidas: Q 9258.06", () => Igual(9258.06m, consulta.Contribuyente("999999K", "recibidas", new(2025, 3, 1), new(2025, 3, 2)).TotalFacturado));
    Probar("Resumen IVA: movimientos y conservación", () =>
    {
        var r = consulta.Iva(new(2025, 3, 2)); Igual(3, r.Movimientos.Count); Igual(886.26m, r.TotalIvaEmitido); Igual(r.TotalIvaEmitido, r.TotalIvaRecibido);
        var n = r.Movimientos.Single(m => m.Nit == "999999K"); Igual(494.06m, n.IvaEmitido); Igual(392.20m, n.IvaRecibido);
    });
    Probar("Resumen rango: con y sin IVA", () =>
    { Igual(18930.38m, consulta.Rango(new(2025, 3, 1), new(2025, 3, 2), false).TotalGeneral); Igual(16902.13m, consulta.Rango(new(2025, 3, 1), new(2025, 3, 2), true).TotalGeneral); });
    Probar("03 Errores: contadores independientes y sin doble conteo", () =>
    {
        var r = Cargar("03_facturas_todos_los_errores.json").GetProperty("resultadoAutorizaciones");
        foreach (var (campo, esperado) in new[] { ("cantidadAutorizacionesAprobadas", 3), ("cantidadAutorizacionesRechazadas", 10), ("cantidadDescartadasPorFormato", 1),
            ("cantidadAutorizacionesRechazadasPorNitEmisorInexistente", 2), ("cantidadAutorizacionesRechazadasPorNitReceptorInexistente", 2),
            ("cantidadAutorizacionesRechazadasPorNitEmisorInvalido", 2), ("cantidadAutorizacionesRechazadasPorNitReceptorInvalido", 1),
            ("cantidadAutorizacionesRechazadasPorIvaMalCalculado", 2), ("cantidadAutorizacionesRechazadasPorTotalMalCalculado", 2), ("cantidadAutorizacionesRechazadasPorReferenciaDuplicada", 2) }) Igual(esperado, r.GetProperty(campo).GetInt32());
    });
    Probar("Reporte errores: 13 recibidas, 3 sin errores, 10 rechazadas", () =>
    { var r = consulta.Errores(new(2025, 4, 1), new(2025, 4, 7)); Igual(13, r.TotalRecibidas); Igual(3, r.TotalSinErrores); Igual(10, r.TotalRechazadas); Igual(2, r.Dias[4].ReferenciaDuplicada); Igual(2, r.Dias[5].Emisores); });
    Probar("04 Bordes: 11 aprobadas, 2 descartadas, 0 rechazadas", () =>
    { var r = Cargar("04_facturas_bordes_formato.json").GetProperty("resultadoAutorizaciones"); Igual(11, r.GetProperty("cantidadAutorizacionesAprobadas").GetInt32()); Igual(2, r.GetProperty("cantidadDescartadasPorFormato").GetInt32()); Igual(0, r.GetProperty("cantidadAutorizacionesRechazadas").GetInt32()); });
    Probar("05 Vacío, objeto y estructura desconocida: rechazo sin mutación", () =>
    { var antes = servicio.Estado(); foreach (var nombre in new[] { "05b_arreglo_vacio.json", "05c_objeto_suelto.json", "05d_sin_llaves_conocidas.json" }) Invalida(() => Cargar(nombre)); Igual(antes, servicio.Estado()); });
    Probar("Lote mixto: transacción completa", () =>
    { var antes = servicio.Estado(); Invalida(() => servicio.Procesar(Mensaje("[{\"NIT\":\"1000004\",\"nombre\":\"X\"},{\"TIEMPO\":\"X\"}]"))); Igual(antes, servicio.Estado()); });
    Probar("06 Volumen: 100 aprobadas y 118 códigos únicos contiguos", () =>
    {
        var r = Cargar("06_carga_100_facturas.json").GetProperty("resultadoAutorizaciones"); Igual(100, r.GetProperty("cantidadAutorizacionesAprobadas").GetInt32());
        var codigos = repo.Leer(e => e.Facturas.Where(f => f.CodigoAprobacion.HasValue).Select(f => f.CodigoAprobacion!.Value).Order().ToArray());
        Igual(118, codigos.Distinct().Count()); Igual(2026102200000118L, codigos[^1]);
        for (int i = 1; i < codigos.Length; i++) Igual(codigos[i - 1] + 1, codigos[i]);
    });
    Probar("Concurrencia: 20 solicitudes independientes sin colisiones", () =>
    { Parallel.For(0, 20, i => servicio.Procesar(Factura("CONC-" + i))); Igual(138, servicio.Estado().FacturasAutorizadas); });
    Probar("Concurrencia: una referencia compartida se autoriza una sola vez", () =>
    { Parallel.For(0, 8, _ => servicio.Procesar(Factura("COMPARTIDA"))); Igual(139, servicio.Estado().FacturasAutorizadas); });
    Probar("Cambio de día en Guatemala: correlativo vuelve a uno", () =>
    {
        reloj.Ahora = reloj.Ahora.AddMinutes(2); var r = Respuesta(servicio.Procesar(Factura("NUEVO-DIA")));
        Igual(2026102300000001L, r.GetProperty("resultadoAutorizaciones").GetProperty("detalleAprobaciones")[0].GetProperty("codigoAprobacion").GetInt64());
    });
    Probar("Rechazo de parámetros inválidos", () =>
    { Invalida(() => consulta.Rango(new(2025, 3, 2), new(2025, 3, 1), false)); Invalida(() => consulta.Contribuyente("12A4", "emitidas", new(2025, 1, 1), new(2025, 12, 1))); Invalida(() => Validaciones.FechaConsulta("ayer")); });
    Probar("PDF y SVG: generación de reportes", () =>
    { var pdf = Reportes.Rango(consulta.Rango(new(2025, 3, 1), new(2025, 3, 2), false)); Verdad(System.Text.Encoding.ASCII.GetString(pdf).StartsWith("%PDF-1.4")); var svg = System.Xml.Linq.XDocument.Parse(GraficaSvg.Crear("<Título>", [new("NIT & K", 10)], "Valor")); Igual("<Título>", svg.Root!.Elements().First().Value); });
    Probar("Una segunda instancia no puede escribir el mismo archivo", () =>
    { try { using var otro = new RepositorioJson(ruta); } catch (IOException) { return; } throw new Exception("No se bloqueó el archivo"); });
}
using (var repo = new RepositorioJson(ruta))
{
    var servicio = new ServicioDte(repo, reloj);
    Probar("Persistencia después de reabrir: siguiente código del mismo día", () =>
    { var r = Respuesta(servicio.Procesar(Factura("TRAS-REINICIO"))); Igual(2026102300000002L, r.GetProperty("resultadoAutorizaciones").GetProperty("detalleAprobaciones")[0].GetProperty("codigoAprobacion").GetInt64()); });
    Probar("Inicializar borra RTU, facturas, descartes y correlativos", () =>
    { servicio.Inicializar(); Igual(new EstadoResumen(0, 0, 0, 0, 0, 0), servicio.Estado()); Igual(0, repo.Leer(e => e.Correlativos.Count)); });
}
Console.WriteLine($"\nResultado: {pasaron} correctas; {fallaron} fallidas.");
return fallaron == 0 ? 0 : 1;

sealed class RelojPrueba : IReloj { public DateTimeOffset Ahora { get; set; } }
