using System.Globalization;
using System.Net;
using System.Text;

namespace SatDte.Core;

/// <summary>Exportación PDF sin dependencias de terceros; texto seleccionable y paginación.</summary>
public static class PdfSimple
{
    private static string Escape(string texto) => new(texto.Select(c => c is >= ' ' and <= '\u00ff' ? c : ' ').ToArray());
    private static string Literal(string texto) => Escape(texto).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    public static byte[] Crear(string titulo, IEnumerable<string> contenido)
    {
        var lineas = contenido.SelectMany(l => Partir(l, 92)).ToList();
        if (lineas.Count == 0) lineas.Add("No hay datos para los filtros indicados.");
        var paginas = lineas.Chunk(57).ToArray();
        var objetos = new List<byte[]>();
        byte[] B(string s) => Encoding.Latin1.GetBytes(s);
        objetos.Add(B("<< /Type /Catalog /Pages 2 0 R >>"));
        objetos.Add(B($"<< /Type /Pages /Count {paginas.Length} /Kids [{string.Join(" ", Enumerable.Range(0, paginas.Length).Select(i => $"{5 + i * 2} 0 R"))}] >>"));
        objetos.Add(B("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"));
        objetos.Add(B("<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>"));
        for (int i = 0; i < paginas.Length; i++)
        {
            int streamId = 6 + i * 2;
            objetos.Add(B($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {streamId} 0 R >>"));
            var texto = new StringBuilder("0.08 0.15 0.25 rg\n");
            texto.Append($"BT /F1 16 Tf 42 798 Td ({Literal(titulo)}) Tj ET\n");
            texto.Append("BT /F1 9 Tf 42 780 Td (SAT / DTE - IPC2 - William René Toledo Corado - 202210198) Tj ET\n");
            texto.Append("0.2 0.3 0.4 RG 42 769 m 553 769 l S\n");
            texto.Append("BT /F2 9 Tf 42 749 Td 12 TL\n");
            foreach (string linea in paginas[i]) texto.Append($"({Literal(linea)}) Tj T*\n");
            texto.Append("ET\n");
            texto.Append($"BT /F1 9 Tf 42 35 Td (Página {i + 1} de {paginas.Length} | Montos en quetzales | Simulador académico) Tj ET\n");
            byte[] datos = B(texto.ToString());
            objetos.Add(B($"<< /Length {datos.Length} >>\nstream\n").Concat(datos).Concat(B("endstream")).ToArray());
        }
        using var salida = new MemoryStream();
        void Escribir(string texto) => salida.Write(B(texto));
        Escribir("%PDF-1.4\n%âãÏÓ\n");
        var offsets = new List<long> { 0 };
        for (int i = 0; i < objetos.Count; i++)
        {
            offsets.Add(salida.Position);
            Escribir($"{i + 1} 0 obj\n"); salida.Write(objetos[i]); Escribir("\nendobj\n");
        }
        long xref = salida.Position;
        Escribir($"xref\n0 {objetos.Count + 1}\n0000000000 65535 f \n");
        foreach (long offset in offsets.Skip(1)) Escribir(offset.ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");
        Escribir($"trailer\n<< /Size {objetos.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return salida.ToArray();
    }

    private static IEnumerable<string> Partir(string linea, int ancho)
    {
        if (linea.Length == 0) { yield return ""; yield break; }
        while (linea.Length > ancho)
        {
            int corte = linea.LastIndexOf(' ', ancho, ancho);
            if (corte < ancho / 2) corte = ancho;
            yield return linea[..corte]; linea = linea[corte..].TrimStart();
        }
        yield return linea;
    }
}

public static class Reportes
{
    private static string Q(decimal n) => n.ToString("N2", CultureInfo.InvariantCulture);
    public static byte[] Contribuyente(ConsultaContribuyente r)
    {
        var l = new List<string> { $"NIT: {r.Nit} - {r.Nombre}", $"Facturas {r.Tipo}. Rango inclusivo: {r.Inicio} a {r.Fin}", "",
            "FECHA          FACTURAS           MONTO TOTAL", new('-', 62) };
        l.AddRange(r.Dias.Select(d => $"{d.Fecha,-14} {d.CantidadFacturas,8} {Q(d.MontoTotal),21}"));
        l.AddRange([new('-', 62), $"TOTAL: {r.CantidadFacturas} facturas | Q {Q(r.TotalFacturado)}", "", "DETALLE DE FACTURAS AUTORIZADAS"]);
        foreach (var f in r.Facturas)
            l.AddRange([$"{f.Fecha:dd/MM/yyyy HH:mm} | Ref: {f.Referencia}", $"Emisor {f.NitEmisor} | Receptor {f.NitReceptor}",
                $"Valor Q {Q(f.Valor)} | IVA Q {Q(f.Iva)} | Total Q {Q(f.Total)}", $"Autorización: {f.CodigoAprobacion}", ""]);
        return PdfSimple.Crear("Consulta de contribuyente", l);
    }
    public static byte[] Iva(ResumenIvaResultado r)
    {
        var l = new List<string> { $"Fecha de las facturas: {r.Fecha}", "Solo facturas autorizadas. IVA emitido y recibido por NIT.", "",
            "NIT                       IVA EMITIDO          IVA RECIBIDO", new('-', 74) };
        l.AddRange(r.Movimientos.Select(m => $"{m.Nit,-22} {Q(m.IvaEmitido),19} {Q(m.IvaRecibido),21}"));
        l.AddRange([new('-', 74), $"TOTAL EMITIDO: Q {Q(r.TotalIvaEmitido)} | TOTAL RECIBIDO: Q {Q(r.TotalIvaRecibido)}", ""]);
        l.AddRange(r.Movimientos.Select(m => $"{m.Nit}: {m.Nombre}"));
        return PdfSimple.Crear("Resumen de IVA por fecha y NIT", l);
    }
    public static byte[] Rango(ResumenRangoResultado r)
    {
        var l = new List<string> { $"Rango inclusivo: {r.Inicio} a {r.Fin}", r.SinIva ? "Valor autorizado SIN IVA." : "Valor autorizado TOTAL (incluye IVA).", "",
            "FECHA          FACTURAS         VALOR AUTORIZADO", new('-', 65) };
        l.AddRange(r.Dias.Select(d => $"{d.Fecha,-14} {d.CantidadFacturas,8} {Q(d.ValorAutorizado),24}"));
        l.AddRange([new('-', 65), $"TOTAL GENERAL: Q {Q(r.TotalGeneral)}"]);
        return PdfSimple.Crear("Resumen por rango de fechas", l);
    }
    public static byte[] Errores(ReporteErroresResultado r)
    {
        var l = new List<string> { $"Rango inclusivo: {r.Inicio} a {r.Fin}",
            $"Recibidas: {r.TotalRecibidas} | Sin errores: {r.TotalSinErrores} | Rechazadas: {r.TotalRechazadas}", "" };
        foreach (var d in r.Dias)
            l.AddRange([$"FECHA: {d.Fecha} | Recibidas: {d.FacturasRecibidas} | Sin errores: {d.FacturasSinErrores}",
                $"NIT emisor inválido: {d.NitEmisorInvalido} | NIT receptor inválido: {d.NitReceptorInvalido}",
                $"NIT emisor inexistente: {d.NitEmisorInexistente} | NIT receptor inexistente: {d.NitReceptorInexistente}",
                $"IVA mal calculado: {d.IvaMalCalculado} | TOTAL mal calculado: {d.TotalMalCalculado}",
                $"REFERENCIA duplicada: {d.ReferenciaDuplicada}", $"Emisores distintos: {d.Emisores} | Receptores distintos: {d.Receptores}", ""]);
        l.Add(r.Nota);
        return PdfSimple.Crear("Reporte de facturas y errores", l);
    }
}

public sealed record Barra(string Etiqueta, decimal Primero, decimal Segundo = 0);
public static class GraficaSvg
{
    public static string Crear(string titulo, IReadOnlyList<Barra> filas, string serie1, string? serie2 = null)
    {
        string E(string s) => WebUtility.HtmlEncode(s);
        string N(double n) => n.ToString("0.##", CultureInfo.InvariantCulture);
        int alto = Math.Max(200, 110 + filas.Count * (serie2 is null ? 56 : 76));
        decimal max = filas.Count == 0 ? 1 : Math.Max(1, filas.Max(f => Math.Max(f.Primero, f.Segundo)));
        var s = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1000 {alto}\" role=\"img\" aria-labelledby=\"titulo\"><title id=\"titulo\">{E(titulo)}</title><rect width=\"1000\" height=\"{alto}\" fill=\"#fff\"/><g font-family=\"Arial,sans-serif\" fill=\"#18253b\"><text x=\"24\" y=\"30\" font-size=\"20\" font-weight=\"bold\">{E(titulo)}</text><rect x=\"24\" y=\"48\" width=\"12\" height=\"12\" fill=\"#176bce\"/><text x=\"43\" y=\"59\" font-size=\"14\">{E(serie1)}</text>");
        if (serie2 is not null) s.Append($"<rect x=\"300\" y=\"48\" width=\"12\" height=\"12\" fill=\"#df7b36\"/><text x=\"319\" y=\"59\" font-size=\"14\">{E(serie2)}</text>");
        if (filas.Count == 0) s.Append("<text x=\"24\" y=\"130\" font-size=\"16\">Sin datos en la fecha o rango indicado.</text>");
        for (int i = 0; i < filas.Count; i++)
        {
            int y = 93 + i * (serie2 is null ? 56 : 76);
            s.Append($"<text x=\"24\" y=\"{y + 16}\" font-size=\"13\">{E(filas[i].Etiqueta)}</text>");
            void Dibujar(decimal valor, int yy, string color)
            {
                double ancho = (double)(valor / max * 610);
                s.Append($"<rect x=\"230\" y=\"{yy}\" width=\"{N(ancho)}\" height=\"22\" rx=\"3\" fill=\"{color}\"/><text x=\"{N(240 + ancho)}\" y=\"{yy + 16}\" font-size=\"13\">Q {valor.ToString("N2", CultureInfo.InvariantCulture)}</text>");
            }
            Dibujar(filas[i].Primero, y, "#176bce");
            if (serie2 is not null) Dibujar(filas[i].Segundo, y + 27, "#df7b36");
        }
        return s.Append("</g></svg>").ToString();
    }
}
