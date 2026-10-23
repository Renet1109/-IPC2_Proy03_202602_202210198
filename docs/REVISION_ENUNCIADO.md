# Revisión del enunciado y recuperación

Fecha: 22 de octubre de 2026.

Se leyeron las nueve páginas de `[IPC2]Proyecto_3_2S2026-v1.pdf`, incluidos los diagramas de arquitectura, y `LEEME.md` del archivo `pruebas_C.zip` proporcionado por el estudiante.

Estudiante: William René Toledo Corado. Carné: 202210198. Sección: B.

## Relación entre requisitos y código recuperado

| Requisito | Página | Implementación |
|---|---|---|
| C#, POO, mensajes JSON y expresiones regulares | 1, 7-8 | `src/SatDte.Core/Modelos.cs`, `ServicioDte.cs` y `Validaciones.cs` |
| Validar NIT, RTU, fechas, IVA, total y referencia única | 2-4 | `Validaciones.cs` y `ServicioDte.cs` |
| Código de autorización con fecha y correlativo diario | 2 | `ServicioDte.cs`, `IReloj` y `EstadoSistema.Correlativos` |
| Persistencia en archivos | 3, 6 | `RepositorioJson.cs`, con copia transaccional y bloqueo de escritura |
| API HTTP consumible desde Postman | 5-7 | `src/SatDte.Api/Program.cs` y `pruebas/SAT-DTE.postman_collection.json` |
| Frontend Razor Pages separado de la API | 5-6 | `src/SatDte.Web/`, cliente `HttpClient` y página `Enviar` |
| Consultas por NIT, IVA y rango; estadísticas de errores | 3, 5-6 | `ServicioConsultas.cs` y página `Peticiones` |
| Gráficas y descarga de reportes PDF | 6 | `Reportes.cs`, `GraficaSvg` y manejadores de `Peticiones` |
| Información del estudiante y documentación | 6 | Página `Ayuda` y `docs/Documentacion.pdf` |
| Inicializar sin datos | 6 | `POST /Inicializar` y página `Index` |
| Ensayo y diagramas de clases, actividades y despliegue | 8 | `docs/ENSAYO.md`, `DIAGRAMAS.md` y `Documentacion.pdf` |
| Conservar los mensajes originales | 8 | Los once archivos de `pruebas/entrada/` coinciden byte a byte con el ZIP recibido |

El ensayo final está adaptado a la plantilla de IPC2 adjuntada posteriormente. Tiene seis páginas de contenido y cuatro anexos, con resumen de 156 palabras, abstract de 158, introducción de 111 y cuatro referencias APA. Se entrega en Word y PDF; la plantilla permanece sin cambios en `plantillas/Ensayo_IPC2.docx`.

## Historial de recuperación

Se recuperaron 72 archivos del paquete de código y 58 de la distribución de Windows, comprobando cada archivo contra su ZIP y su copia original. La carpeta actual estaba vacía al comenzar la recuperación. Se mantienen los ZIP originales sin cambios en `respaldos/`.

Los cinco commits agrupan trabajo concreto: núcleo y persistencia, API, frontend, pruebas y compilación, y documentación e iniciadores. Registran la recuperación en la fecha actual. No se modifican fechas para simular avances anteriores.

## Comprobación y entrega

La compilación Release y las 24 pruebas del núcleo pasaron en la nueva ubicación. La API recuperada pasó las 30 comprobaciones HTTP, incluidos los resultados esperados del ZIP, concurrencia, persistencia al reiniciar, PDF, SVG y entradas inválidas. El frontend pasó sus 20 comprobaciones después de actualizar el nombre del estudiante. En total son 74 comprobaciones correctas. Se revisaron visualmente las diez páginas de documentación y los cuatro tipos de reporte PDF. Los resultados reproducibles se consultan en `docs/VERIFICACION.md` y se pueden repetir con los comandos del README.

El enunciado exige un repositorio llamado `IPC2_Proy03_202602_202210198`, cuatro releases semanales, agregar a `EliezerGG` como colaborador de la sección B y entregar en UEDI un texto con la URL del repositorio. Se prepararon cuatro versiones basadas en el código recuperado y el ensayo final, documentadas en `RELEASES.md`; se publican con fecha real y no acreditan avances de semanas anteriores. El enlace proporcionado contiene un guion inicial adicional: `Renet1109/-IPC2_Proy03_202602_202210198`. La revisión detectó además que la presentación de fechas del reporte diario usa ISO en lugar de dd/mm/yyyy. El nombre, ese formato de presentación, el acceso del auxiliar y la entrega en UEDI siguen pendientes de resolver.
