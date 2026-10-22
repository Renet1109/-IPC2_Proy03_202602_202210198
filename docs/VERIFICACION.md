# Evidencia de comprobación

Fecha: 22 de octubre de 2026. Plataforma de comprobación: Windows x64, SDK .NET 10.0.401 y runtime .NET 10.0.12.

## Resultado

| Grupo | Correctas | Fallidas | Alcance |
|---|---:|---:|---|
| Núcleo | 24 | 0 | NIT, calendario, montos, archivos 01-06, consultas, correlativos, concurrencia, cambio de día, persistencia y reset |
| API HTTP | 30 | 0 | Contratos, resultados del ZIP, 400, 415, PDF, SVG, matriz diaria completa, 24 solicitudes concurrentes y reinicio real |
| Frontend | 20 | 0 | Pantallas Razor, filtros, PDF, gráficas, documentación y cuatro archivos inválidos con antiforgery válido |

Registros: `resultado-core.txt`, `resultado-http.json` y `resultado-web.json`. La compilación Release se realiza con advertencias tratadas como errores y no requiere paquetes de terceros.

## Revisión visual

En el navegador se seleccionaron los archivos 01 y 02 juntos, se verificaron el tipo y las primeras filas de la vista previa y se enviaron en orden. La Web mostró 9/1/3 para el RTU y cuatro autorizaciones con códigos del día de Guatemala. Se comprobó la tabla y la gráfica del resumen de IVA del 2 de marzo de 2025.

La documentación tiene diez páginas: portada, cinco páginas de contenido y cuatro anexos. Se renderizaron sus páginas con Poppler y se revisaron los párrafos, los diagramas y las referencias. También se renderizaron los cuatro tipos de PDF producidos por la API. Se ajustó la paginación para que la nota del reporte de errores de siete días no quedara sola en otra página.

## Reproducción

```powershell
.\Probar.ps1
python .\pruebas\verificar_http.py
# Con la aplicación iniciada y los archivos 01 y 02 cargados:
python .\pruebas\verificar_web.py --url http://localhost:5000
```

Los tests del núcleo y HTTP utilizan sus propios archivos de estado. Las comprobaciones Web requieren que se carguen 01 y 02; son de lectura salvo el envío de archivos inválidos que no modifican el estado. No se ejecutó el generador de pruebas del ZIP ni se alteraron los archivos originales.

## Límites de esta comprobación

Se comprobó el funcionamiento local; no se realizó un despliegue público, una prueba de carga de producción, certificación SAT, publicación GitHub ni entrega en UEDI. La prueba del cambio de día usa IReloj con fecha controlada. El historial semanal de releases no forma parte de esta evidencia.
