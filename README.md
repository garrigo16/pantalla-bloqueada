# Pantalla bloqueada

Mini app para Windows con una simulación visual de pantalla de bloqueo y configuración desde la bandeja del sistema. El equipo sigue desbloqueado: esta app no sustituye el bloqueo de seguridad de Windows.

## Instalar o compartir

Descarga **Iniciar.exe** o **PantallaBloqueada.zip** desde [la última versión](https://github.com/garrigo16/pantalla-bloqueada/releases/latest). Abre el instalador: prepara la app para tu usuario, aplica los valores iniciales y la deja activa. No necesita permisos de administrador. El ZIP también incluye el desinstalador.

Puedes borrar el instalador después. La copia instalada y sus ajustes quedan en `%LOCALAPPDATA%\PantallaBloqueada`.

## Configuración

Abre el configurador desde el icono del área de notificación. Permite elegir la pantalla con el fondo, identificar monitores, cambiar la imagen, mostrar el reloj, ajustar el tiempo antes de pasar a negro y elegir el inicio con Windows. La combinación de teclado para alternar la simulación no aparece en la pantalla simulada.

Los valores iniciales son una pantalla con fondo durante **20 segundos**, el resto en negro y un movimiento horizontal de **un píxel cada cinco minutos**, alternando sentidos mientras la simulación esté activa. Las pantallas se pintan de negro, sin apagarlas físicamente. La actividad periódica y la petición de energía se detienen al salir de la simulación.

## Activación automática por inactividad

En el configurador, **Activar automáticamente por inactividad** permite elegir cuántos segundos sin teclado ni ratón deben pasar antes de iniciar la simulación. Está activado inicialmente a **300 segundos (5 minutos)** y se puede desactivar con la casilla.

El contador solo actúa mientras la simulación está desactivada. No interrumpe el configurador ni actúa sobre el bloqueo real de Windows. Después de salir manualmente espera a que vuelvas a usar el PC antes de iniciar otro periodo de inactividad, para evitar una reapertura inmediata. El tiempo para pasar el fondo a negro sigue siendo un ajuste independiente.

## Actualizaciones

- La app busca nuevas versiones estables de este repositorio cada **12 horas** mientras está en ejecución.
- **Buscar actualizaciones** comprueba inmediatamente e indica si ya está instalada la última versión.
- Cuando hay una versión nueva, aparece el aviso y se habilita **Actualizar a…**. La instalación requiere pulsar ese botón.
- La descarga se limita al instalador de este repositorio y se verifica con el SHA-256 publicado por GitHub antes de ejecutarla.
- Se conservan los ajustes y la preferencia de inicio con Windows. La app se reinicia al terminar.
- Sin conexión, la búsqueda muestra el error y permite reintentarlo. No requiere una cuenta ni un token de GitHub para los usuarios de la app.

Para pasar desde una copia antigua sin actualizador, abre una vez el instalador de esta versión. Las siguientes actualizaciones se realizan desde la app.

## Bromas y easter eggs

En **Bromas y easter eggs** puedes elegir Todos (inicial), Sarcasmo suave, Mascota y animaciones o Discreto. El despertar breve del fondo tiene una casilla independiente: una tecla lo muestra cuatro segundos y vuelve a negro.

Las reacciones aparecen solo en la pantalla elegida y se retiran solas, sin reiniciar el plazo para pasar a negro. No tienen sonido ni guardan texto. Los contadores y logros viven en memoria hasta la siguiente simulación. El [catálogo de guiños](docs/EASTER_EGGS.md) contiene los gestos, por si quieres destripar la sorpresa.

## Compilar y comprobar

Windows 10/11 con .NET Framework 4.8 y PowerShell. Sin paquetes externos.

```powershell
./scripts/Build.ps1
./scripts/Test.ps1
```

Los instaladores y el ZIP se generan en `artifacts/`. Las pruebas comprueban versiones, redirecciones, integridad, búsquedas, conservación de ajustes, recuperación ante fallos, inactividad, secuencias de easter eggs y filtros de entrada. No simulan actividad de ratón ni crean ventanas de bloqueo durante la integración continua. Los temporales de pruebas se retiran al terminar.

## Publicar una versión

1. Cambia `Current` y `Assembly` en `src/Shared/VersionInfo.cs` y actualiza `docs/RELEASE_NOTES.md`.
2. Al subir el cambio de versión a `main` se publica automáticamente. También puedes ejecutar **Actions → Publicar versión → Run workflow** con la etiqueta correspondiente, por ejemplo `v1.3.0`, o enviar una etiqueta nueva `v*` a Git.
3. El flujo compila y comprueba los instaladores, prepara un borrador con los archivos y después publica la versión estable. No sobrescribe versiones ya publicadas.

Los ajustes, las imágenes personales, la caché de actualizaciones y los ejecutables generados quedan fuera del repositorio. Los binarios se distribuyen como archivos de las versiones.

## Retirar la aplicación

Ejecuta **Desinstalar.exe** desde el paquete o desde la carpeta instalada. Retira los archivos de la app y su entrada de inicio automático; conserva otros archivos que hayas dejado en esa carpeta.

Los ejecutables no tienen firma digital. Las políticas del equipo pueden limitar su ejecución o la generación de actividad; el programa no cambia esas políticas ni impide un bloqueo manual de Windows.

Autor: Miquel Garrigo.
