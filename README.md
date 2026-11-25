# Drifft Analyzer

<img src="assets/bao.png" width="96" alt="Bao dormilón, icono de la aplicación">

Mini app para Windows con una simulación visual de pantalla de bloqueo y configuración desde la bandeja del sistema. El equipo sigue desbloqueado: esta app no sustituye el bloqueo de seguridad de Windows.

## Instalar o compartir

Descarga **Iniciar.exe** o **DrifftAnalyzer.zip** desde [la última versión](https://github.com/garrigo16/pantalla-bloqueada/releases/latest). Abre Iniciar.exe: comprueba e instala el paquete completo para tu usuario y deja la app activa. No necesita permisos de administrador. El ZIP incluye el instalador completo y el desinstalador; así Iniciar.exe puede instalar sin volver a descargar el paquete. También puedes abrir directamente **DrifftAnalyzer-Setup.exe**.

Puedes borrar el instalador después. El proceso de Windows se llama `DrifftAnalyzer.exe`. La copia instalada y sus ajustes quedan en `%LOCALAPPDATA%\PantallaBloqueada`; se mantiene esa ubicación interna para conservar la configuración de versiones anteriores.

## Configuración

Abre el configurador desde el icono del área de notificación. Permite elegir la pantalla con el fondo, identificar monitores, cambiar la imagen, mostrar el reloj, ajustar el tiempo antes de pasar a negro y elegir el inicio con Windows. La combinación de teclado para alternar la simulación no aparece en la pantalla simulada.

Los valores iniciales son una pantalla con fondo durante **20 segundos**, el resto en negro y un movimiento horizontal de **un píxel cada cinco minutos**, alternando sentidos mientras la simulación esté activa. Las pantallas se pintan de negro, sin apagarlas físicamente. La actividad periódica y la petición de energía se detienen al salir de la simulación.

**Tema** permite alternar entre **Clásico**, el predeterminado con el fondo habitual, los easter eggs y el pinball de gatos, y **Pokémon**, con el claro animado y Pinball NS. En Pokémon, el claro aparece en el monitor elegido con un fundido tras **5 segundos en negro**. Puedes cambiar la espera. Con 0 segundos, empieza al pasar a negro. Si mantienes el fondo visible indefinidamente, el claro no aparece.

El paisaje se incluye en el instalador. Los sprites se descargan desde SpriteCollab durante la primera activación y se guardan en la caché local para usos posteriores. Si la primera descarga no está disponible, el paisaje aparece igualmente.

## Atajos de teclado

En **Configurar → Atajos de teclado** puedes elegir una combinación compartida para activar y salir, o una distinta para cada acción. Los cambios se aplican al guardar, sin reiniciar. Se mantiene el atajo inicial en instalaciones anteriores. Si una reserva está ocupada o falla el guardado, continúan funcionando los atajos anteriores.

El selector oculta las combinaciones que usa Windows. Al cargar ajustes antiguos, la app retira Shift de los atajos configurados y recupera **Win + Ñ** si una combinación deja de estar disponible.

El **pinball del tema seleccionado** se abre añadiendo **Shift al atajo de salida**. Shift queda reservado para esa función. Ninguna combinación de salida se muestra en las pantallas simuladas.

Mientras la simulación tiene el foco, se filtran Alt + Tab y la tecla Win. Los atajos configurados tienen prioridad. Win + L sigue realizando el bloqueo real y Ctrl + Alt + Supr sigue a cargo de Windows.

Si el teclado no tiene Ñ o una combinación no se puede registrar al arrancar, la app abre el configurador para elegir otra y mantiene desactivada la simulación hasta disponer de los atajos.

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

## El rincón del gato

La adaptación local 1.6 incorpora el proyecto Godot Cat Pinball, con sus tres capítulos, enemigos y física originales, sobre nuestro fondo de booknook. Incluye lluvia animada, parpadeos simples y dobles del gato negro y un halo cálido que cambia suavemente en la lámpara. **Ctrl izquierdo y Ctrl derecho** accionan las palancas; mantén **Espacio** y suéltalo para lanzar, **P** pausa y **Esc** vuelve a la simulación.

La campaña y la interfaz están traducidas al español de España. Durante la historia, **Espacio completa el texto que se está escribiendo**; después de soltarlo, la siguiente pulsación avanza una diapositiva. Mantener la tecla no salta diálogos. La partida se pausa al perder el foco.

El atajo de salida conserva la prioridad durante la partida. Solo aparece en la pantalla elegida y las demás siguen negras. El juego no tiene sonido y termina su proceso al salir; también vuelve a la simulación tras un minuto sin interacción. El proceso de Godot queda vinculado a Drifft Analyzer para cerrarse si la app termina inesperadamente.

El juego completo está incluido en el repositorio y en los instaladores, con los créditos originales conservados. [Detalles de integración](docs/CAT_PINBALL.md) y [avisos de terceros](THIRD_PARTY_NOTICES.md).

## Pinball NS

En **Configurar → Tema → Pokémon**, elige la ROM de tu cartucho de Pokémon Pinball USA/Australia con vibración. También se admite una ROM de Pokémon Pinball Generations 1.6. El claro está disponible aunque dejes vacío ese ajuste.

La primera partida descarga y verifica [Pinball NS 2.9 para Windows](https://github.com/ReiniShari/Pinball-NS/releases/tag/v2.9). La aplicación usa la ROM desde su ubicación original. Pinball NS guarda la partida junto a ella. **Ctrl izquierdo y Ctrl derecho** accionan las palancas, **Espacio** lanza, **P** pausa y **Esc** vuelve a la simulación.

## Compilar y comprobar

Windows 10/11 de 64 bits con .NET Framework 4.8 y PowerShell. El juego utiliza Godot 4.7.2, incluido en el paquete; el usuario no necesita instalarlo por separado. La compilación descarga y verifica el motor y prepara el juego desde sus fuentes.

```powershell
./scripts/Build.ps1
./scripts/Test.ps1
```

Los instaladores y el ZIP se generan en `artifacts/`. Las pruebas comprueban versiones, redirecciones, integridad, búsquedas, conservación de ajustes, recuperación ante fallos, inactividad, campaña, tipografía, secuencias de easter eggs y filtros de entrada. No simulan actividad de ratón ni crean ventanas de bloqueo durante la integración continua. Los temporales de pruebas se retiran al terminar.

Si una actualización falla, se conserva la versión anterior y el botón permite reintentar. El diagnóstico queda localmente en `%LOCALAPPDATA%\PantallaBloqueada\actualizador.log`, sin rutas, ajustes ni datos personales. Si una copia antigua ya quedó atascada, abrir el nuevo Iniciar.exe o el instalador completo permite recuperarla conservando sus ajustes.

## Publicar una versión

1. Cambia `Current` y `Assembly` en `src/Shared/VersionInfo.cs` y actualiza `docs/RELEASE_NOTES.md`.
2. Al subir el cambio de versión a `main` se publica automáticamente. También puedes ejecutar **Actions → Publicar versión → Run workflow** con la etiqueta correspondiente, por ejemplo `v1.3.0`, o enviar una etiqueta nueva `v*` a Git.
3. El flujo compila y comprueba los instaladores, prepara un borrador con los archivos y después publica la versión estable. No sobrescribe versiones ya publicadas.

Los ajustes, las imágenes personales, la caché de actualizaciones y los ejecutables generados quedan fuera del repositorio. Los binarios se distribuyen como archivos de las versiones.

El icono de bao está en `assets/bao.png`. La compilación genera automáticamente sus nueve tamaños (16–256 píxeles) en `build/Bao.ico`, sin herramientas externas.

## Retirar la aplicación

Ejecuta **Desinstalar.exe** desde el paquete o desde la carpeta instalada. Retira los archivos de la app y su entrada de inicio automático; conserva otros archivos que hayas dejado en esa carpeta.

Los ejecutables no tienen firma digital. Las políticas del equipo pueden limitar su ejecución o la generación de actividad; el programa no cambia esas políticas ni impide un bloqueo manual de Windows.

Autor: Miquel Garrigo.
