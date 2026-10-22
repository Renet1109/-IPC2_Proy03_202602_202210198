# Contrato de la API

Base local: `http://localhost:5001`. JSON de entrada con `Content-Type: application/json`. Los arreglos son obligatorios; un arreglo vacío, un objeto suelto, un JSON malformado o una estructura desconocida recibe HTTP 400 y `{"error":"..."}`. Un tipo de contenido incorrecto recibe 415. La API acepta hasta 10 MB por solicitud y la Web limita cada archivo a 5 MB.

## Rutas

| Método | Ruta | Parámetros / entrada | Respuesta |
|---|---|---|---|
| GET | `/salud` | Ninguno | Disponibilidad |
| GET | `/` | Ninguno | Identificación y rutas |
| POST | `/Procesar` | Arreglo de RTU o de facturas | Contrato RTU o `resultadoAutorizaciones` |
| POST | `/Inicializar` | Sin cuerpo | Borra RTU, facturas, descartes y correlativos |
| GET | `/Estado` | Ninguno | Recuentos y total autorizado |
| GET | `/Contribuyentes` | Ninguno | NIT y nombres del RTU |
| GET | `/ConsultaDatos` | `nit`, `tipo=emitidas|recibidas`, `inicio`, `fin` | Días, total y detalle de facturas |
| GET | `/ResumenIva` | `fecha` | IVA emitido y recibido por NIT |
| GET | `/ResumenRango` | `inicio`, `fin`, `sinIva=true|false` | Valor autorizado por fecha y total |
| GET | `/ReporteErrores` | `inicio`, `fin` | Recuentos diarios de todos los errores |
| GET | `/Grafica` | `tipo=iva` y `fecha`, o `tipo=rango`, `inicio`, `fin`, `sinIva` | SVG |
| GET | `/Reportes/contribuyente.pdf` | Mismos filtros de ConsultaDatos | PDF paginado |
| GET | `/Reportes/iva.pdf` | `fecha` | PDF |
| GET | `/Reportes/rango.pdf` | `inicio`, `fin`, `sinIva` | PDF |
| GET | `/Reportes/errores.pdf` | `inicio`, `fin` | PDF |

Las fechas de consulta admiten `yyyy-MM-dd` o `dd/MM/yyyy`. El frontend usa el primer formato. El rango es inclusivo. Se devuelven días con movimientos, en orden ascendente. Las consultas financieras y el detalle incluyen solo facturas autorizadas; el reporte de errores también considera las rechazadas.

## Ejemplo RTU

```json
[
  {"NIT":"7378106","nombre":"Empresa ABC, S.A."},
  {"NIT":"8338817","nombre":"Comercial XYZ, S.A."}
]
```

```json
{"CONTRIBUYENTES_NUEVOS":2,"CONTRIBUYENTES_ACTUALIZADOS":0,"NITS_INVALIDOS":0}
```

## Ejemplo de factura

```json
[{"TIEMPO":"Guatemala, 01/03/2025 10:00 hrs.","REFERENCIA":"DEMO-01","NIT_EMISOR":"7378106","NIT_RECEPTOR":"8338817","VALOR":100.00,"IVA":12.00,"TOTAL":112.00}]
```

La respuesta conserva el contenedor `resultadoAutorizaciones`, las cantidades de aprobadas e inexistentes y `detalleAprobaciones`, como en el enunciado. Se añaden los demás contadores del LEEME y los detalles de rechazos y descartes. `codigoAprobacion` es un entero de 64 bits, no una cadena.

Los errores son `NitEmisorInvalido`, `NitReceptorInvalido`, `NitEmisorInexistente`, `NitReceptorInexistente`, `IvaMalCalculado`, `TotalMalCalculado` y `ReferenciaDuplicada`. Una factura puede acumular varios; el total de rechazadas la cuenta una sola vez. Un NIT inválido no se cuenta también como inexistente.

## Comprobar desde PowerShell

```powershell
Invoke-RestMethod -Method Post -Uri http://localhost:5001/Inicializar
Invoke-RestMethod -Method Post -Uri http://localhost:5001/Procesar -ContentType 'application/json; charset=utf-8' -InFile .\pruebas\entrada\01_rtu_completo.json
Invoke-RestMethod -Method Post -Uri http://localhost:5001/Procesar -ContentType 'application/json; charset=utf-8' -InFile .\pruebas\entrada\02_facturas_marzo_graficas.json
Invoke-RestMethod 'http://localhost:5001/ResumenRango?inicio=2025-03-01&fin=2025-03-02&sinIva=false'
```

La colección `pruebas/SAT-DTE.postman_collection.json` incluye mensajes originales y consultas listas para importar en Postman. Su variable `baseUrl` permite cambiar el servidor.
