# Versión compilada para Windows

El paquete `SAT_DTE_Windows_202210198.zip` contiene los dos servicios publicados para Windows x64. Permite usar el programa sin compilar. Requiere el runtime de ASP.NET Core 10 x64 o el SDK .NET 10, presente en el equipo donde se verificó.

1. Extrae todo el ZIP a una carpeta donde puedas guardar archivos.
2. Abre `Iniciar.cmd` con doble clic. Espera a que aparezca “Programa listo”. Se abre el navegador automáticamente.
3. Usa la Web en `http://localhost:5000`; la API está en `http://localhost:5001`.
4. Mantén abierta la ventana del iniciador. `Detener.cmd` o Ctrl+C cierran ambos servicios.
5. Al volver a iniciar, los datos se recuperan de `aplicacion/Api/App_Data/estado.json`.

El programa se entrega vacío. Carga primero `pruebas/entrada/01_rtu_completo.json` y después `02_facturas_marzo_graficas.json`. Las consultas de ejemplo usan el NIT `999999K` y el rango del 1 al 2 de marzo de 2025. La documentación también se descarga desde Ayuda.

Si los puertos están ocupados, el iniciador lo informa sin detener procesos ajenos. Para elegir otros puertos:

```powershell
.\Iniciar.ps1 -PuertoWeb 5010 -PuertoApi 5011
```

La conexión del frontend a la API se ajusta al puerto elegido. Los registros de ambos servicios se guardan en `logs/`. El botón Inicializar de la Web elimina los datos del simulador; detener el programa los conserva.

## Comprobación de la distribución

Se verificó el ZIP extraído en una carpeta nueva con espacios en su nombre, con los mismos accesos CMD que utilizará el usuario. Se comprobó que no hubiera datos precargados, se enviaron los archivos 01 y 02, se consultaron las pantallas y se descargaron los PDF. Se ejecutaron las 20 comprobaciones del frontend sobre esa distribución.

Las 12 comprobaciones del iniciador y la distribución pasaron: inicio de ambos servicios, estado vacío, carga original, CSS y JavaScript publicados, documentación accesible, consultas Web, detección de segundo inicio, cierre, reapertura, persistencia y liberación de puertos. El registro está en `resultado-paquete.json`. La comprobación HTTP del paquete se registra en `resultado-http-paquete.json`.

El ZIP del código fuente conserva la solución C#, las pruebas y `Empaquetar.ps1` para reproducir la distribución. La versión compilada es una forma adicional de ejecutar el mismo programa.
