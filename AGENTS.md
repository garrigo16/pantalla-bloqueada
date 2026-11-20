# Mantenimiento

- Conservar preferencias anteriores; definir valores iniciales en src/Shared/AppSettings.cs, compartido con el instalador.
- No incluir ajustes, fondos personales, credenciales, ejecutables ni resultados generados en Git. Los binarios se distribuyen como assets de las releases.
- Antes de publicar: ejecutar scripts/Build.ps1 y scripts/Test.ps1; actualizar VersionInfo.cs y docs/RELEASE_NOTES.md. Cambiar la versión en main dispara la publicación automática.
- Mantener salida inmediata durante animaciones, monitor elegido y temporizadores independientes. Entradas solo en ventanas propias; sin historial de texto, sonido ni clics sintéticos.
- Evitar código y documentación duplicados u obsoletos. Las notas describen la versión que se publica; Git conserva el histórico.
- Autor: Miquel Garrigo. Conservar la identidad configurada, sin firmas automáticas ni coautores añadidos.
