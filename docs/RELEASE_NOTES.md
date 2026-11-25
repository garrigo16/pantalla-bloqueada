Drifft Analyzer 1.6.4: actualizador más resistente y pinball completo.

- Se incluye la campaña completa de Cat Pinball en español de España, con los dos Ctrl, fondo lofi animado, Nunito y centrado en la pantalla elegida.
- Iniciar.exe sigue siendo pequeño para que las versiones antiguas puedan actualizarse. Descarga el paquete completo verificando su SHA-256; el ZIP incluye ambos instaladores para usarlo sin esa descarga adicional.
- Corregida la conexión TLS del instalador inicial cuando arranca en un proceso nuevo, sin una consulta previa a la API.
- Las versiones nuevas descargan directamente el instalador completo y admiten su tamaño real.
- Reintentos ante errores temporales de GitHub y consulta alternativa si su API falla o limita las peticiones.
- Preparación de archivos antes de cerrar la app; si falla la instalación, restaura y vuelve a arrancar la versión anterior.
- El botón permite reintentar si falla el instalador y los errores de las vistas no cierran la aplicación.
- Diagnóstico local en actualizador.log, sin rutas, ajustes, texto escrito ni datos personales.

Conserva tus ajustes e inicio automático. Si una copia antigua ya quedó atascada al actualizar, abre una vez el nuevo Iniciar.exe; también puedes instalar directamente con DrifftAnalyzer-Setup.exe.
