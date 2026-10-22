namespace SatDte.Core;

public sealed class ServicioConsultas(IRepositorio repositorio)
{
    private static string F(DateOnly fecha) => fecha.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    private static bool EnRango(Factura f, DateOnly inicio, DateOnly fin)
    { var fecha = DateOnly.FromDateTime(f.Fecha); return fecha >= inicio && fecha <= fin; }

    public ConsultaContribuyente Contribuyente(string nit, string tipo, DateOnly inicio, DateOnly fin)
    {
        Validaciones.Rango(inicio, fin);
        nit = Validaciones.NormalizarNit(nit);
        if (!Validaciones.NitValido(nit)) throw new SolicitudInvalidaException("El NIT de la consulta es inválido.");
        tipo = tipo.ToLowerInvariant();
        if (tipo is not ("emitidas" or "recibidas")) throw new SolicitudInvalidaException("tipo debe ser emitidas o recibidas.");
        return repositorio.Leer(e =>
        {
            if (!e.Contribuyentes.TryGetValue(nit, out var contribuyente))
                throw new SolicitudInvalidaException("El NIT no existe en el RTU.");
            var facturas = e.Facturas.Where(f => f.CodigoAprobacion.HasValue && EnRango(f, inicio, fin) &&
                (tipo == "emitidas" ? f.NitEmisor == nit : f.NitReceptor == nit)).OrderBy(f => f.Fecha).ToArray();
            var dias = facturas.GroupBy(f => DateOnly.FromDateTime(f.Fecha)).OrderBy(g => g.Key)
                .Select(g => new DiaContribuyente(F(g.Key), g.Count(), g.Sum(f => f.Total))).ToArray();
            return new ConsultaContribuyente(nit, contribuyente.Nombre, tipo, F(inicio), F(fin), dias,
                facturas.Length, facturas.Sum(f => f.Total), facturas);
        });
    }

    public ResumenIvaResultado Iva(DateOnly fecha) => repositorio.Leer(e =>
    {
        var facturas = e.Facturas.Where(f => f.CodigoAprobacion.HasValue && DateOnly.FromDateTime(f.Fecha) == fecha).ToArray();
        var nits = facturas.SelectMany(f => new[] { f.NitEmisor, f.NitReceptor }).Distinct().OrderBy(n => n, StringComparer.Ordinal);
        var movimientos = nits.Select(n => new MovimientoIva(n, e.Contribuyentes.GetValueOrDefault(n)?.Nombre ?? n,
            facturas.Where(f => f.NitEmisor == n).Sum(f => f.Iva), facturas.Where(f => f.NitReceptor == n).Sum(f => f.Iva))).ToArray();
        return new ResumenIvaResultado(F(fecha), movimientos, movimientos.Sum(m => m.IvaEmitido), movimientos.Sum(m => m.IvaRecibido));
    });

    public ResumenRangoResultado Rango(DateOnly inicio, DateOnly fin, bool sinIva)
    {
        Validaciones.Rango(inicio, fin);
        return repositorio.Leer(e =>
        {
            var dias = e.Facturas.Where(f => f.CodigoAprobacion.HasValue && EnRango(f, inicio, fin))
                .GroupBy(f => DateOnly.FromDateTime(f.Fecha)).OrderBy(g => g.Key)
                .Select(g => new DiaRango(F(g.Key), g.Count(), g.Sum(f => sinIva ? f.Valor : f.Total))).ToArray();
            return new ResumenRangoResultado(F(inicio), F(fin), sinIva, dias, dias.Sum(d => d.ValorAutorizado));
        });
    }

    public ReporteErroresResultado Errores(DateOnly inicio, DateOnly fin)
    {
        Validaciones.Rango(inicio, fin);
        return repositorio.Leer(e =>
        {
            var dias = e.Facturas.Where(f => EnRango(f, inicio, fin)).GroupBy(f => DateOnly.FromDateTime(f.Fecha)).OrderBy(g => g.Key)
                .Select(g =>
                {
                    int C(string error) => g.Count(f => f.Errores.Contains(error));
                    return new DiaErrores(F(g.Key), g.Count(), C(ErroresFactura.EmisorInvalido), C(ErroresFactura.ReceptorInvalido),
                        C(ErroresFactura.EmisorInexistente), C(ErroresFactura.ReceptorInexistente), C(ErroresFactura.Iva),
                        C(ErroresFactura.Total), C(ErroresFactura.Referencia), g.Count(f => f.CodigoAprobacion.HasValue),
                        g.Select(f => f.NitEmisor).Distinct().Count(), g.Select(f => f.NitReceptor).Distinct().Count());
                }).ToArray();
            return new ReporteErroresResultado(F(inicio), F(fin), dias, dias.Sum(d => d.FacturasRecibidas),
                dias.Sum(d => d.FacturasSinErrores), dias.Sum(d => d.FacturasRecibidas - d.FacturasSinErrores),
                "Una factura puede tener varios errores. Los descartes por formato no se agrupan por fecha; se consultan en Estado y en la respuesta del lote. Emisores y receptores distintos incluyen los intentos rechazados.");
        });
    }
}
