using System.Globalization;
using System.Text.Json;

namespace SatDte.Core;

public sealed class ServicioDte(IRepositorio repositorio, IReloj reloj)
{
    public object Procesar(JsonElement mensaje)
    {
        if (mensaje.ValueKind != JsonValueKind.Array || mensaje.GetArrayLength() == 0)
            throw new SolicitudInvalidaException("El mensaje debe ser un arreglo JSON no vacío.");
        var filas = mensaje.EnumerateArray().ToArray();
        if (filas.Any(f => f.ValueKind != JsonValueKind.Object))
            throw new SolicitudInvalidaException("Cada elemento del arreglo debe ser un objeto JSON.");
        // Validación del lote completo antes de modificar el RTU o asignar un correlativo.
        bool EsRtu(JsonElement f) => f.TryGetProperty("NIT", out _) && f.TryGetProperty("nombre", out _);
        bool EsFactura(JsonElement f) => f.TryGetProperty("TIEMPO", out _) || f.TryGetProperty("REFERENCIA", out _)
            || f.TryGetProperty("NIT_EMISOR", out _);
        if (filas.All(f => EsRtu(f) && !EsFactura(f)))
        {
            if (filas.Any(f => !Validaciones.Texto(f, "NIT", out _) || !Validaciones.Texto(f, "nombre", out var n)
                || string.IsNullOrWhiteSpace(n)))
                throw new SolicitudInvalidaException("NIT y nombre deben ser textos; el nombre no puede estar vacío.");
            return repositorio.Actualizar(estado => ActualizarRtu(estado, filas));
        }
        if (filas.All(f => EsFactura(f) && !EsRtu(f)))
            return repositorio.Actualizar(estado => Autorizar(estado, filas));
        throw new SolicitudInvalidaException("El arreglo debe contener solo contribuyentes (NIT, nombre) o solo facturas (TIEMPO, REFERENCIA, ...).");
    }

    private static object ActualizarRtu(EstadoSistema estado, JsonElement[] filas)
    {
        int nuevos = 0, actualizados = 0, invalidos = 0;
        foreach (var fila in filas)
        {
            string nit = Validaciones.NormalizarNit(fila.GetProperty("NIT").GetString()!);
            if (!Validaciones.NitValido(nit)) { invalidos++; continue; }
            if (estado.Contribuyentes.ContainsKey(nit)) actualizados++; else nuevos++;
            estado.Contribuyentes[nit] = new(nit, fila.GetProperty("nombre").GetString()!.Trim());
        }
        // Las claves del diccionario conservan exactamente las mayúsculas exigidas por el contrato.
        return new Dictionary<string, int> { ["CONTRIBUYENTES_NUEVOS"] = nuevos,
            ["CONTRIBUYENTES_ACTUALIZADOS"] = actualizados, ["NITS_INVALIDOS"] = invalidos };
    }

    private object Autorizar(EstadoSistema estado, JsonElement[] filas)
    {
        var referencias = estado.Facturas.Where(f => f.CodigoAprobacion.HasValue)
            .Select(f => f.Referencia).ToHashSet(StringComparer.Ordinal);
        var aprobaciones = new List<object>();
        var rechazos = new List<object>();
        var descartes = new List<Descarte>();
        var contadores = new Dictionary<string, int>();
        int posicion = 0;
        foreach (var fila in filas)
        {
            posicion++;
            if (!IntentarLeer(fila, out var factura, out var motivo))
            {
                Validaciones.Texto(fila, "REFERENCIA", out var referencia);
                var descarte = new Descarte(posicion, referencia, motivo, reloj.Ahora);
                descartes.Add(descarte);
                estado.Descartes.Add(descarte);
                continue;
            }
            var f = factura!;
            f.RecibidaEn = reloj.Ahora;
            if (!Validaciones.NitValido(f.NitEmisor)) f.Errores.Add(ErroresFactura.EmisorInvalido);
            else if (!estado.Contribuyentes.ContainsKey(f.NitEmisor)) f.Errores.Add(ErroresFactura.EmisorInexistente);
            if (!Validaciones.NitValido(f.NitReceptor)) f.Errores.Add(ErroresFactura.ReceptorInvalido);
            else if (!estado.Contribuyentes.ContainsKey(f.NitReceptor)) f.Errores.Add(ErroresFactura.ReceptorInexistente);
            if (f.Iva != Validaciones.Iva(f.Valor)) f.Errores.Add(ErroresFactura.Iva);
            if (f.Total != f.Valor + f.Iva) f.Errores.Add(ErroresFactura.Total);
            if (referencias.Contains(f.Referencia)) f.Errores.Add(ErroresFactura.Referencia);
            if (f.Errores.Count == 0)
            {
                string dia = reloj.Ahora.ToOffset(TimeSpan.FromHours(-6)).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                long correlativo = estado.Correlativos.GetValueOrDefault(dia) + 1;
                if (correlativo > 99_999_999) throw new SolicitudInvalidaException("Se agotaron los correlativos del día. El lote no fue guardado.");
                estado.Correlativos[dia] = correlativo;
                f.CodigoAprobacion = long.Parse(dia + correlativo.ToString("D8", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
                referencias.Add(f.Referencia);
                aprobaciones.Add(new { referencia = f.Referencia, codigoAprobacion = f.CodigoAprobacion.Value });
            }
            else
            {
                foreach (string error in f.Errores) contadores[error] = contadores.GetValueOrDefault(error) + 1;
                rechazos.Add(new { posicion, referencia = f.Referencia, errores = f.Errores.ToArray() });
            }
            estado.Facturas.Add(f);
        }
        return new
        {
            resultadoAutorizaciones = new
            {
                cantidadAutorizacionesAprobadas = aprobaciones.Count,
                cantidadAutorizacionesRechazadasPorNitEmisorInexistente = contadores.GetValueOrDefault(ErroresFactura.EmisorInexistente),
                cantidadAutorizacionesRechazadasPorNitReceptorInexistente = contadores.GetValueOrDefault(ErroresFactura.ReceptorInexistente),
                cantidadAutorizacionesRechazadasPorNitEmisorInvalido = contadores.GetValueOrDefault(ErroresFactura.EmisorInvalido),
                cantidadAutorizacionesRechazadasPorNitReceptorInvalido = contadores.GetValueOrDefault(ErroresFactura.ReceptorInvalido),
                cantidadAutorizacionesRechazadasPorIvaMalCalculado = contadores.GetValueOrDefault(ErroresFactura.Iva),
                cantidadAutorizacionesRechazadasPorTotalMalCalculado = contadores.GetValueOrDefault(ErroresFactura.Total),
                cantidadAutorizacionesRechazadasPorReferenciaDuplicada = contadores.GetValueOrDefault(ErroresFactura.Referencia),
                cantidadAutorizacionesRechazadas = rechazos.Count,
                cantidadDescartadasPorFormato = descartes.Count,
                detalleAprobaciones = aprobaciones,
                detalleRechazos = rechazos,
                detalleDescartes = descartes
            }
        };
    }

    private static bool IntentarLeer(JsonElement fila, out Factura? factura, out string motivo)
    {
        factura = null;
        motivo = "Faltan campos obligatorios o tienen un tipo incorrecto.";
        if (!Validaciones.Texto(fila, "TIEMPO", out var tiempo) ||
            !Validaciones.Texto(fila, "REFERENCIA", out var referencia) ||
            !Validaciones.Texto(fila, "NIT_EMISOR", out var emisor) ||
            !Validaciones.Texto(fila, "NIT_RECEPTOR", out var receptor)) return false;
        if (!Validaciones.FechaFactura(tiempo, out var fecha))
        { motivo = "TIEMPO debe contener una fecha real y una hora de 00:00 a 23:59."; return false; }
        if (string.IsNullOrWhiteSpace(referencia) || referencia.Length > 40)
        { motivo = "REFERENCIA debe tener de 1 a 40 caracteres."; return false; }
        if (!Validaciones.Dinero(fila, "VALOR", true, out var valor) ||
            !Validaciones.Dinero(fila, "IVA", false, out var iva) ||
            !Validaciones.Dinero(fila, "TOTAL", true, out var total))
        { motivo = "Los montos deben ser numéricos con hasta dos decimales; VALOR y TOTAL positivos, IVA no negativo."; return false; }
        try { _ = checked(valor + iva); _ = Validaciones.Iva(valor); }
        catch (OverflowException) { motivo = "Los montos exceden la capacidad del tipo decimal."; return false; }
        factura = new() { Fecha = fecha, Referencia = referencia, NitEmisor = Validaciones.NormalizarNit(emisor),
            NitReceptor = Validaciones.NormalizarNit(receptor), Valor = valor, Iva = iva, Total = total };
        motivo = "";
        return true;
    }

    public void Inicializar() => repositorio.Actualizar(estado =>
    {
        estado.Contribuyentes.Clear(); estado.Facturas.Clear(); estado.Descartes.Clear(); estado.Correlativos.Clear();
        return true;
    });

    public EstadoResumen Estado() => repositorio.Leer(e => new EstadoResumen(e.Contribuyentes.Count,
        e.Facturas.Count, e.Facturas.Count(f => f.CodigoAprobacion.HasValue),
        e.Facturas.Count(f => !f.CodigoAprobacion.HasValue), e.Descartes.Count,
        e.Facturas.Where(f => f.CodigoAprobacion.HasValue).Sum(f => f.Total)));

    public IReadOnlyList<Contribuyente> Contribuyentes() => repositorio.Leer(e =>
        e.Contribuyentes.Values.OrderBy(c => c.NIT, StringComparer.Ordinal).ToArray());
}
