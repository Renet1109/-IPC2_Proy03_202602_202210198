using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SatDte.Core;

public static partial class Validaciones
{
    [GeneratedRegex(@"\A[0-9]{1,20}[0-9K]\z", RegexOptions.CultureInvariant)]
    private static partial Regex PatronNit();
    [GeneratedRegex(@"(?<![0-9])(?<d>[0-9]{1,2})/(?<m>[0-9]{1,2})/(?<y>[0-9]{4})\s+(?<h>[0-9]{1,2}):(?<min>[0-9]{2})(?![0-9:])", RegexOptions.CultureInvariant)]
    private static partial Regex PatronFecha();

    public static string NormalizarNit(string nit) => nit.Trim().ToUpperInvariant();

    public static bool NitValido(string nit)
    {
        nit = NormalizarNit(nit);
        if (!PatronNit().IsMatch(nit)) return false;
        int suma = 0, peso = 2;
        // El último carácter es el verificador. El dígito de su izquierda tiene peso 2.
        for (int i = nit.Length - 2; i >= 0; i--, peso++) suma += (nit[i] - '0') * peso;
        int resultado = (11 - suma % 11) % 11;
        char esperado = resultado == 10 ? 'K' : (char)('0' + resultado);
        return nit[^1] == esperado;
    }

    public static bool FechaFactura(string texto, out DateTime fecha)
    {
        fecha = default;
        var match = PatronFecha().Match(texto);
        if (!match.Success) return false;
        try
        {
            int N(string grupo) => int.Parse(match.Groups[grupo].Value, CultureInfo.InvariantCulture);
            fecha = new DateTime(N("y"), N("m"), N("d"), N("h"), N("min"), 0, DateTimeKind.Unspecified);
            return true;
        }
        catch (ArgumentOutOfRangeException) { return false; }
    }

    public static bool Texto(JsonElement objeto, string clave, out string valor)
    {
        valor = "";
        if (!objeto.TryGetProperty(clave, out var campo) || campo.ValueKind != JsonValueKind.String) return false;
        valor = campo.GetString()!;
        return true;
    }

    public static bool Dinero(JsonElement objeto, string clave, bool positivo, out decimal valor)
    {
        valor = 0;
        return objeto.TryGetProperty(clave, out var campo) && campo.ValueKind == JsonValueKind.Number
            && campo.TryGetDecimal(out valor) && (positivo ? valor > 0 : valor >= 0)
            && decimal.Round(valor, 2, MidpointRounding.AwayFromZero) == valor;
    }

    public static decimal Iva(decimal valor) => decimal.Round(valor * 0.12m, 2, MidpointRounding.AwayFromZero);
    public static DateOnly FechaConsulta(string? texto)
    {
        if (DateOnly.TryParseExact(texto, ["yyyy-MM-dd", "dd/MM/yyyy"], CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var fecha)) return fecha;
        throw new SolicitudInvalidaException("La fecha debe tener formato yyyy-MM-dd o dd/MM/yyyy.");
    }
    public static void Rango(DateOnly inicio, DateOnly fin)
    {
        if (fin < inicio) throw new SolicitudInvalidaException("La fecha final debe ser igual o posterior a la inicial.");
    }
}
