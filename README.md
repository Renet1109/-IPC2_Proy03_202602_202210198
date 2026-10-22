# Proyecto 3 - SAT / DTE

**William René Toledo Corado · 202210198 · Sección B**
IPC2 · Universidad de San Carlos de Guatemala · Segundo semestre 2026.

Solución en C# con dos aplicaciones: una API HTTP y un frontend Razor Pages que la consume con `HttpClient`. Incluye RTU, autorización de DTE, validaciones, persistencia JSON, consultas, gráficas SVG, exportación PDF, documentación y pruebas reproducibles.

## Ejecutar en Windows

**Versión compilada:** si solo quieres utilizar el programa, extrae el ZIP `SAT_DTE_Windows_202210198.zip` y abre `Iniciar.cmd` con doble clic. Esa versión necesita el runtime de ASP.NET Core 10 x64, ya instalado en este equipo, y no necesita el SDK ni compilar. `Detener.cmd` cierra ambos servicios y conserva los datos.

**Código fuente:** en esta carpeta, `Iniciar.cmd` compila y abre el programa. También puedes ejecutar los comandos siguientes.

Requisito: **SDK de .NET 10**, no solo el runtime. No se necesitan paquetes NuGet externos, base de datos, Node ni Python para ejecutar la aplicación.

Abre PowerShell en esta carpeta y ejecuta:

```powershell
.\Iniciar.ps1
```

Si la política de PowerShell impide ejecutar el archivo, puedes abrir una terminal para este proceso con:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Iniciar.ps1
```

El navegador se abre en **http://localhost:5000** después de comprobar que los dos servicios están listos. La API está en **http://localhost:5001**. Mantén la terminal abierta; Ctrl+C o `Detener.cmd` detienen ambos servicios y conservan los datos. Los registros están en `logs/`. Puedes compilar por separado con `./Compilar.ps1` y arrancar después con `./Iniciar.ps1 -SinCompilar`. Si los puertos están ocupados, utiliza por ejemplo `./Iniciar.ps1 -PuertoWeb 5010 -PuertoApi 5011`. El iniciador conecta automáticamente la Web con el puerto elegido para la API.

El script de compilación usa MSBuild del SDK instalado y evita depender de la comprobación de workloads opcionales. `global.json` admite las bandas de características de .NET 10. También puedes abrir `Proyecto3.slnx` en un IDE compatible con .NET 10.

## Primera prueba

1. En **Enviar mensaje**, selecciona `pruebas/entrada/01_rtu_completo.json` y luego `02_facturas_marzo_graficas.json`. Comprueba el orden en la vista previa y procesa los mensajes.
2. El RTU debe informar **9 nuevos, 1 actualizado y 3 inválidos**. Las facturas deben producir **4 aprobaciones**.
3. En **Peticiones y reportes**, consulta NIT `999999K`, emitidas, del `2025-03-01` al `2025-03-02`: **Q 9,672.32**. Recibidas: **Q 9,258.06**.
4. En el resumen de IVA, selecciona `2025-03-02`. En rango, usa `2025-03-01` a `2025-03-02`: **Q 18,930.38** con IVA y **Q 16,902.13** sin IVA.
5. Descarga los PDF. Carga después los archivos 03, 04 y 06. Los archivos 05 deben mostrar mensajes de error sin provocar una pantalla 500.
6. Inicializa desde Inicio para repetir la prueba desde cero. La inicialización elimina todos los datos del simulador.

Los archivos originales del ZIP están copiados **sin cambios** en `pruebas/entrada`. El generador incluido procede del ZIP; no se ejecuta durante las pruebas del proyecto.

## Organización

```text
src/SatDte.Core/        Entidades, contratos, repositorio y servicios de negocio
src/SatDte.Api/         Endpoints HTTP, JSON, PDF y gráficas
src/SatDte.Web/         Páginas Razor y cliente HTTP
pruebas/SatDte.Tests/   24 pruebas del núcleo, sin librerías de testing externas
pruebas/verificar_http.py  30 comprobaciones sobre una API temporal y aislada
pruebas/entrada/        Contenido original de pruebas_C.zip
docs/                  Ensayo, diagramas, API, decisiones y evidencia
```

Los datos se guardan en `src/SatDte.Api/App_Data/estado.json`. Las escrituras son transaccionales dentro de una instancia de API y se confirman con reemplazo del archivo temporal. Un bloqueo impide que dos instancias escriban el mismo archivo. Si el estado está corrupto, el arranque falla y conserva el archivo para revisión.

## Pruebas

```powershell
.\Probar.ps1
# Opcional: Python 3.10 o superior, solo biblioteca estándar.
python .\pruebas\verificar_http.py
```

Las pruebas HTTP inician su propia API con un puerto libre y un archivo nuevo. No reinicializan la API que tengas abierta. Los resultados locales se escriben bajo `pruebas/App_Data`, excluido de Git.

La comprobación entregada obtuvo **24/24 pruebas del núcleo, 30/30 pruebas HTTP y 20/20 comprobaciones del frontend**. Consulta `docs/VERIFICACION.md` y los registros incluidos. La revisión del navegador comprobó la carga múltiple, las vistas previas y la presentación de resultados. Con 01 y 02 cargados, puedes ejecutar `python pruebas/verificar_web.py` para repetir las comprobaciones del frontend.

## Configuración

- API: `src/SatDte.Api/appsettings.json`, puerto y `Datos:Archivo`.
- Web: `src/SatDte.Web/appsettings.json`, `Api:BaseUrl`, estudiante y ruta del PDF de documentación.
- Variables de entorno equivalentes: `Datos__Archivo` y `Api__BaseUrl`.
- Para cambiar puertos manualmente, pasa `--urls http://localhost:PUERTO` al ejecutar cada DLL y actualiza la URL de la API en la Web.

La configuración incluida escucha en localhost. Para un despliegue público se necesita una configuración de alojamiento, TLS y acceso apropiada; este paquete está preparado para la demostración local del proyecto.

## Generar la versión compilada

```powershell
.\Empaquetar.ps1
```

Se crea `dist/SAT_DTE_Windows_202210198.zip`, con la aplicación publicada, iniciador, documentación y archivos de ejemplo. El destino debe ser nuevo: el script no sobrescribe una distribución que pueda contener datos. También puedes elegirlo con `-Destino 'C:\ruta\nueva'`.

## Documentación y entrega

Lee `docs/API.md`, `docs/DECISIONES.md`, `docs/DIAGRAMAS.md` y `docs/ENTREGA.md`. El ensayo está en `docs/Documentacion.pdf`, con fuente editable en `docs/ENSAYO.md`. La plantilla específica de ensayo del curso no estaba entre los archivos recibidos: revisa portada, estilo y referencias antes de entregarlo.

El nombre solicitado por el enunciado es **IPC2_Proy03_202602_202210198**. El repositorio proporcionado es [Renet1109/-IPC2_Proy03_202602_202210198](https://github.com/Renet1109/-IPC2_Proy03_202602_202210198), cuyo nombre contiene un guion adicional al inicio. Revisa ese detalle antes de entregar.

## Recuperación e historial

El 22 de octubre de 2026 se recuperaron los archivos desde los ZIP de la entrega anterior y se verificaron contra sus originales. Los respaldos originales están en `respaldos/`; la aplicación compilada recuperada está en `dist/SAT_DTE_Windows_202210198/`. Ambas carpetas se conservan localmente y están excluidas de Git. Para compilar los cambios del código fuente, usa `Iniciar.cmd` en la raíz.

El historial de recuperación se organiza en cinco partes: núcleo y persistencia, API HTTP, frontend Razor Pages, pruebas y compilación, y documentación e iniciadores. Consulta `git log --oneline --reverse` y `docs/REVISION_ENUNCIADO.md` para revisar los cambios y su relación con el enunciado.

Los commits de recuperación corresponden al trabajo de esta fecha. Los cuatro releases semanales exigidos por el curso son un requisito adicional que no se acredita con estos commits. La entrega en UEDI y la configuración del colaborador se realizan desde las cuentas correspondientes.
