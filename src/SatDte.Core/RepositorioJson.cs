using System.Text.Json;

namespace SatDte.Core;

public interface IRepositorio : IDisposable
{
    T Leer<T>(Func<EstadoSistema, T> consulta);
    T Actualizar<T>(Func<EstadoSistema, T> operacion);
}

/// <summary>Una instancia escritora; copia transaccional y reemplazo atómico del archivo.</summary>
public sealed class RepositorioJson : IRepositorio
{
    private readonly object _mutex = new();
    private readonly string _ruta;
    private readonly FileStream _bloqueo;
    private EstadoSistema _estado;
    private static readonly JsonSerializerOptions Opciones = new() { WriteIndented = true };

    public RepositorioJson(string ruta)
    {
        _ruta = Path.GetFullPath(ruta);
        Directory.CreateDirectory(Path.GetDirectoryName(_ruta)!);
        _bloqueo = new FileStream(_ruta + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        try
        {
            _estado = File.Exists(_ruta)
                ? JsonSerializer.Deserialize<EstadoSistema>(File.ReadAllBytes(_ruta))
                    ?? throw new InvalidDataException("El archivo de estado está vacío.")
                : new EstadoSistema();
            if (_estado.Version != 1 || _estado.Contribuyentes is null || _estado.Facturas is null ||
                _estado.Descartes is null || _estado.Correlativos is null)
                throw new InvalidDataException("La estructura del archivo de estado no es compatible.");
        }
        catch { _bloqueo.Dispose(); throw; }
    }

    private static EstadoSistema Copia(EstadoSistema estado) =>
        JsonSerializer.Deserialize<EstadoSistema>(JsonSerializer.SerializeToUtf8Bytes(estado))!;

    public T Leer<T>(Func<EstadoSistema, T> consulta)
    {
        lock (_mutex) return consulta(Copia(_estado));
    }

    public T Actualizar<T>(Func<EstadoSistema, T> operacion)
    {
        lock (_mutex)
        {
            var candidato = Copia(_estado);
            var respuesta = operacion(candidato);
            var temporal = _ruta + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var archivo = new FileStream(temporal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    JsonSerializer.Serialize(archivo, candidato, Opciones);
                    archivo.Flush(flushToDisk: true);
                }
                File.Move(temporal, _ruta, overwrite: true);
                _estado = candidato;
            }
            finally { if (File.Exists(temporal)) File.Delete(temporal); }
            return respuesta;
        }
    }

    public void Dispose() => _bloqueo.Dispose();
}
