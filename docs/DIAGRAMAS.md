# Diagramas editables

Los diagramas también se incluyen dibujados en los anexos de Documentacion.pdf.

## Clases

```mermaid
classDiagram
class IRepositorio {
  <<interface>>
  +Leer(consulta)
  +Actualizar(operacion)
}
class RepositorioJson {
  -EstadoSistema estado
  -FileStream bloqueo
  +Leer(consulta)
  +Actualizar(operacion)
}
class EstadoSistema {
  +Dictionary Contribuyentes
  +List Facturas
  +List Descartes
  +Dictionary Correlativos
}
class Contribuyente {
  +string NIT
  +string Nombre
}
class Factura {
  +DateTime Fecha
  +string Referencia
  +string NitEmisor
  +string NitReceptor
  +decimal Valor
  +decimal Iva
  +decimal Total
  +long CodigoAprobacion
  +List Errores
}
class ServicioDte {
  +Procesar(mensaje)
  +Inicializar()
  +Estado()
}
class ServicioConsultas {
  +Contribuyente(nit,tipo,inicio,fin)
  +Iva(fecha)
  +Rango(inicio,fin,sinIva)
  +Errores(inicio,fin)
}
class IReloj {
  <<interface>>
  +DateTimeOffset Ahora
}
class RelojGuatemala
class ClienteApi
class PageModel
RepositorioJson ..|> IRepositorio
RelojGuatemala ..|> IReloj
RepositorioJson *-- EstadoSistema
EstadoSistema *-- Contribuyente
EstadoSistema *-- Factura
ServicioDte --> IRepositorio
ServicioDte --> IReloj
ServicioConsultas --> IRepositorio
PageModel --> ClienteApi
```

## Actividad: procesar lote

```mermaid
flowchart TD
 A[Recibir JSON] --> B{Arreglo homogéneo no vacío}
 B -- No --> X[Responder 400 sin cambios]
 B -- Sí --> C[Tomar bloqueo y copiar estado]
 C --> D{Tipo RTU}
 D -- Sí --> E[Validar módulo 11 por NIT]
 E --> F[Crear o actualizar solo válidos]
 D -- No --> G[Extraer fecha y validar formato de factura]
 G --> H{Formato válido}
 H -- No --> I[Registrar descarte]
 H -- Sí --> J[Validar NIT, RTU, IVA, total y referencia]
 J --> K{Sin errores}
 K -- Sí --> L[Asignar correlativo del día de Guatemala]
 K -- No --> M[Registrar todos los errores]
 I --> N{Quedan filas}
 L --> N
 M --> N
 N -- Sí --> G
 N -- No --> O[Escribir temporal y reemplazar estado]
 F --> O
 O --> P[Confirmar copia en memoria y responder JSON]
```

## Actividad: NIT

```mermaid
flowchart TD
 A[Normalizar k a K] --> B{1 a 20 dígitos más verificador}
 B -- No --> X[Inválido]
 B -- Sí --> C[Separar verificador final]
 C --> D[Recorrer base de derecha a izquierda]
 D --> E[Sumar dígito por peso 2, 3, ...]
 E --> F[Calcular: r = mod 11 de 11 menos mod 11 de suma]
 F --> G[Convertir 10 a K]
 G --> H{Coincide con verificador}
 H -- Sí --> I[Válido]
 H -- No --> X
```

## Actividad: consultas

```mermaid
flowchart TD
 A[Recibir tipo y filtros] --> B{Fechas y parámetros válidos}
 B -- No --> X[Responder 400]
 B -- Sí --> C[Obtener instantánea del repositorio]
 C --> D[Filtrar por fecha de factura y NIT si aplica]
 D --> E[Agrupar por día o por NIT]
 E --> F[Sumar montos o contar errores]
 F --> G[Responder JSON, SVG o PDF]
```

## Despliegue

```mermaid
flowchart LR
 subgraph Equipo_local
  N[Navegador] -->|HTTP 5000| W[SatDte.Web / Razor Pages]
  W -->|HttpClient / HTTP 5001 / JSON| A[SatDte.Api / ASP.NET Core]
  P[Postman u otro cliente] -->|HTTP 5001| A
  A --> C[SatDte.Core / servicios]
  C --> R[RepositorioJson]
  R --> D[(App_Data/estado.json)]
  R --> L[estado.json.lock]
 end
```
