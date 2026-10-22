namespace SatDte.Core;

public sealed record Contribuyente(string NIT, string Nombre);

public sealed class Factura
{
    public DateTime Fecha { get; set; }
    public string Referencia { get; set; } = "";
    public string NitEmisor { get; set; } = "";
    public string NitReceptor { get; set; } = "";
    public decimal Valor { get; set; }
    public decimal Iva { get; set; }
    public decimal Total { get; set; }
    public long? CodigoAprobacion { get; set; }
    public DateTimeOffset RecibidaEn { get; set; }
    public List<string> Errores { get; set; } = [];
}

public sealed record Descarte(int Posicion, string Referencia, string Motivo, DateTimeOffset RecibidaEn);

public sealed class EstadoSistema
{
    public int Version { get; set; } = 1;
    public Dictionary<string, Contribuyente> Contribuyentes { get; set; } = [];
    public List<Factura> Facturas { get; set; } = [];
    public List<Descarte> Descartes { get; set; } = [];
    public Dictionary<string, long> Correlativos { get; set; } = [];
}

public static class ErroresFactura
{
    public const string EmisorInvalido = "NitEmisorInvalido";
    public const string ReceptorInvalido = "NitReceptorInvalido";
    public const string EmisorInexistente = "NitEmisorInexistente";
    public const string ReceptorInexistente = "NitReceptorInexistente";
    public const string Iva = "IvaMalCalculado";
    public const string Total = "TotalMalCalculado";
    public const string Referencia = "ReferenciaDuplicada";
}

public sealed class SolicitudInvalidaException(string mensaje) : Exception(mensaje);

public interface IReloj { DateTimeOffset Ahora { get; } }
public sealed class RelojGuatemala : IReloj
{
    public DateTimeOffset Ahora => DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-6));
}

public sealed record DiaContribuyente(string Fecha, int CantidadFacturas, decimal MontoTotal);
public sealed record ConsultaContribuyente(string Nit, string Nombre, string Tipo, string Inicio, string Fin,
    IReadOnlyList<DiaContribuyente> Dias, int CantidadFacturas, decimal TotalFacturado, IReadOnlyList<Factura> Facturas);
public sealed record MovimientoIva(string Nit, string Nombre, decimal IvaEmitido, decimal IvaRecibido);
public sealed record ResumenIvaResultado(string Fecha, IReadOnlyList<MovimientoIva> Movimientos,
    decimal TotalIvaEmitido, decimal TotalIvaRecibido);
public sealed record DiaRango(string Fecha, int CantidadFacturas, decimal ValorAutorizado);
public sealed record ResumenRangoResultado(string Inicio, string Fin, bool SinIva, IReadOnlyList<DiaRango> Dias, decimal TotalGeneral);
public sealed record DiaErrores(string Fecha, int FacturasRecibidas, int NitEmisorInvalido, int NitReceptorInvalido,
    int NitEmisorInexistente, int NitReceptorInexistente, int IvaMalCalculado, int TotalMalCalculado,
    int ReferenciaDuplicada, int FacturasSinErrores, int Emisores, int Receptores);
public sealed record ReporteErroresResultado(string Inicio, string Fin, IReadOnlyList<DiaErrores> Dias, int TotalRecibidas,
    int TotalSinErrores, int TotalRechazadas, string Nota);
public sealed record EstadoResumen(int Contribuyentes, int FacturasRecibidas, int FacturasAutorizadas,
    int FacturasRechazadas, int DescartadasPorFormato, decimal TotalAutorizado);
