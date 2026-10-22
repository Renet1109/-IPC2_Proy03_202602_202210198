# Archivos de prueba — Proyecto 3 (SAT / DTE)

Todos los valores esperados de abajo los calculó `generar_pruebas.py` con un oráculo independiente (no usa el código del API). Si cambias los archivos, vuelve a correr el script y regenera este documento.

## Orden de carga

| # | Archivo | Qué prueba |
|---|---|---|
| 0 | `POST /Inicializar` | Estado limpio (o el botón Inicializar de la Web) |
| 1 | `01_rtu_completo.json` | RTU con NITs válidos (incluye K y 21 caracteres), inválidos, vacío y una actualización |
| 2 | `02_facturas_marzo_graficas.json` | 4 facturas válidas en 2 día(s) de marzo 2025, para tablas y gráficas |
| 3 | `03_facturas_todos_los_errores.json` | Un caso por cada tipo de error, un multi-error y un descarte (requiere 1 y 2) |
| 4 | `04_facturas_bordes_formato.json` | Formatos de TIEMPO, decimales extremos, NIT con K y de 21 caracteres |
| 5 | `05a` a `05d` | JSON inválidos: el API responde 400 `{ "error": ... }` y la Web muestra el mensaje |
| 6 | `06_carga_100_facturas.json` | Carga de volumen (100 facturas) |

Las fechas de cada archivo están separadas (marzo, abril, mayo, junio a diciembre) para que los números de una consulta no cambien al cargar los demás archivos.

## Cargar con curl

```bash
H='Content-Type: application/json'
curl -s -X POST localhost:5001/Inicializar
for f in 01_rtu_completo 02_facturas_marzo_graficas 03_facturas_todos_los_errores 04_facturas_bordes_formato; do
  echo "== $f"; curl -s -X POST localhost:5001/Procesar -H "$H" --data-binary @$f.json; echo
done
# carga de volumen (opcional):
curl -s -X POST localhost:5001/Procesar -H "$H" --data-binary @06_carga_100_facturas.json
# codigos de hoy en Guatemala (UTC-6) en macOS:
date -u -v-6H +%Y%m%d
```

En la Web: **Enviar mensaje**, selecciona varios archivos a la vez (se procesan en el orden en que los elijas; pon el 01 antes que las facturas).

## Resultados esperados

`HOY` = fecha de hoy en Guatemala en formato `yyyymmdd`. Los códigos son `HOY` + correlativo de 8 dígitos.

### 01 — RTU

`CONTRIBUYENTES_NUEVOS`: **9** · `CONTRIBUYENTES_ACTUALIZADOS`: **1** · `NITS_INVALIDOS`: **3**

(7378106 aparece dos veces: la segunda cuenta como actualizado; `5123456`, `12A4` y el vacío son inválidos.)

### 02 — Facturas de marzo

Aprobadas: **4** (códigos `HOY00000001` a `HOY00000004`), sin rechazos ni descartes.

### 03 — Todos los errores

| Campo de la respuesta | Esperado |
|---|---|
| `cantidadAutorizacionesAprobadas` | 3 (E01, E02, E14; códigos `HOY00000005` a `HOY00000007`) |
| `...RechazadasPorNitEmisorInexistente` | 2 (E03, E12) |
| `...RechazadasPorNitReceptorInexistente` | 2 (E04, E12) |
| `...RechazadasPorNitEmisorInvalido` | 2 (E05, E11) |
| `...RechazadasPorNitReceptorInvalido` | 1 (E06) |
| `...RechazadasPorIvaMalCalculado` | 2 (E07, E11) |
| `...RechazadasPorTotalMalCalculado` | 2 (E08, E11) |
| `...RechazadasPorReferenciaDuplicada` | 2 (E09 repite G0001 de la BD; E10 repite E01 del mismo mensaje) |
| `cantidadAutorizacionesRechazadas` | 10 (facturas distintas; E11 y E12 cuentan una sola vez) |
| `cantidadDescartadasPorFormato` | 1 (E13, TIEMPO sin fecha) |

### 04 — Bordes de formato

Aprobadas: **11** · descartadas por formato: **2** · rechazadas: **0**

| Ref | Caso | Esperado |
|---|---|---|
| F01 | `Guatemala, 05/06/2025 08:05 hrs.` | aprobada |
| F02 | `Ciudad de Guatemala, 5/6/2025 9:05` (un dígito, sin «hrs.») | aprobada |
| F03 | `01/07/2025 00:00` (sin lugar, medianoche) | aprobada |
| F04 | `31/12/2025 23:59` | aprobada |
| F05 | `29/02/2024` (año bisiesto) | aprobada |
| F06 | `29/02/2025` (fecha inexistente) | descartada |
| F07 | fecha sin hora | descartada |
| F08 | VALOR 0.04 → IVA 0.00 | aprobada |
| F09 | VALOR 99999999.99 → IVA 12000000.00, TOTAL 111999999.99 | aprobada |
| F10 | emisor `6K` (verificador K) | aprobada |
| F11 | emisor de 21 caracteres (`123456789012345678908`) | aprobada |
| F12 | REFERENCIA de exactamente 40 caracteres | aprobada |
| F13 | receptor en minúscula `6k` | aprobada (se normaliza a `6K`); si sale rechazada, es un hallazgo |

### 05 — JSON inválidos

Los cuatro archivos deben dar **HTTP 400** con `{ "error": "..." }` en el API, y la Web debe mostrar el mensaje sin error 500.

### 06 — Carga de volumen

Aprobadas: **100**, códigos contiguos y distintos.

## Consultas esperadas (tras cargar 01 y 02)

Rango: **01/03/2025 a 02/03/2025**. Estos números no cambian al cargar los archivos 03 a 06.

### `ConsultaDatos` NIT 999999K, emitidas

| Fecha | Facturas | Monto |
|---|---|---|
| 01/03/2025 | 1 | 5061.13 |
| 02/03/2025 | 1 | 4611.19 |
| **Total** | **2** | **9672.32** |

### `ConsultaDatos` NIT 999999K, recibidas

| Fecha | Facturas | Monto |
|---|---|---|
| 01/03/2025 | 1 | 5597.50 |
| 02/03/2025 | 1 | 3660.56 |
| **Total** | **2** | **9258.06** |

### `ResumenIva` fecha 02/03/2025

| NIT | IVA emitido | IVA recibido |
|---|---|---|
| 1000004 | 0.00 | 494.06 |
| 8338817 | 392.20 | 0.00 |
| 999999K | 494.06 | 392.20 |

### `ResumenRango` 01/03/2025 a 02/03/2025

| Fecha | Total | Sin IVA |
|---|---|---|
| 01/03/2025 | 10658.63 | 9516.64 |
| 02/03/2025 | 8271.75 | 7385.49 |
| **Total general** | **18930.38** | **16902.13** |

### `ReporteErrores` 01/04/2025 a 07/04/2025 (tras cargar 03)

| Fecha | Recibidas | Emisor inválido | Receptor inválido | Emisor inexistente | Receptor inexistente | IVA mal | TOTAL mal | Ref. duplicada | Sin errores | Emisores | Receptores |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 01/04/2025 | 2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 2 | 2 |
| 02/04/2025 | 2 | 0 | 0 | 1 | 1 | 0 | 0 | 0 | 0 | 2 | 2 |
| 03/04/2025 | 2 | 1 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 2 |
| 04/04/2025 | 2 | 0 | 0 | 0 | 0 | 1 | 1 | 0 | 0 | 1 | 1 |
| 05/04/2025 | 2 | 0 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 1 | 1 |
| 06/04/2025 | 2 | 1 | 0 | 1 | 1 | 1 | 1 | 0 | 0 | 2 | 2 |
| 07/04/2025 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 1 | 1 |

## Pruebas manuales en la Web

1. **Enviar mensaje**: elige `01` y `02` a la vez y mira la vista previa (tipo detectado, cantidad, primeras filas); envía.
2. Repite con `05a` a `05d`: mensaje de error, nunca pantalla de error del servidor.
3. **Peticiones → Consultar contribuyente**: `999999K`, emitidas y recibidas, `2025-03-01` a `2025-03-02`.
4. **Resumen de IVA** del `2025-03-02` y **Resumen por rango** con Total y Sin IVA; abre las gráficas y descarga los PDF.
5. **Reporte de errores** del `2025-04-01` al `2025-04-07` después de cargar `03`.
6. **Inicializar** y comprueba que las consultas vuelven a salir vacías.

## Regenerar
```bash
python3 generar_pruebas.py ./pruebas_corto --perfil corto --dias 2 --min-por-dia 2 --max-por-dia 3 --carga 100 --semilla 2026
```
