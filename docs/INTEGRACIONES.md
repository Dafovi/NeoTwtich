# Integraciones opcionales

Además de MeowMeowCatCam, están disponibles **Live Captions** y **Chat Translator**, con activación y configuración independientes. Consulta la [guía de subtítulos y traducción](SUBTITULOS.md).

## MeowMeowCatCam

Proyecto original: **[catherpiee/meowmeowcatcam](https://github.com/catherpiee/meowmeowcatcam)**, creado por **catherpiee**. Neo Twitch implementa el instalador y el control de OBS; el detector, los modelos y los memes se descargan directamente del repositorio original al pulsar **Instalar**. No se incorporan al código ni a los paquetes de Neo Twitch.

### Uso

1. Abre **Integraciones → MeowMeowCatCam → Instalar**. Necesitas internet y Windows x64. Se descarga un Python privado y las dependencias, sin añadir nada al PATH ni requerir un Python del sistema.
2. Conecta OBS desde **Conexiones** y vuelve a Integraciones. Pulsa **Actualizar escenas**, selecciona una escena y elige gato, cámara o ambos.
3. Pulsa **Activar en OBS**. Acepta el acceso a la cámara en Windows si fuese necesario. La aplicación externa utiliza la cámara predeterminada (índice 0).
4. Ajusta las fuentes `NeoTwitch · MeowMeowCatCam · Camera` y `NeoTwitch · MeowMeowCatCam · Meme` en OBS. Su posición se conserva al volver a activar. Mantén las ventanas Camera y Meme abiertas y sin minimizar durante la captura.
5. **Detener** cierra el proceso iniciado por Neo Twitch y oculta sus fuentes. Salir de Neo Twitch también lo detiene; cerrar a la bandeja lo mantiene activo.

La cámara original muestra marcas y texto de diagnóstico del proyecto externo. Si no abre, revisa los permisos de cámara para aplicaciones de escritorio y desactiva cualquier otra aplicación que la esté ocupando. La primera versión no permite elegir un índice distinto ni produce una cámara virtual.

### Instalación y mantenimiento

Carpeta: `%LocalAppData%\NeoTwitch\integrations\meowmeowcatcam`.

- Descarga fijada al commit `9cae127fb922df948a1348f2fcfc7083f7746895`, con comprobación SHA-256.
- Python 3.12.10 x64 del paquete oficial `python` de NuGet, también con SHA-256. Se ejecuta con entorno aislado del Python del usuario.
- pip instala las versiones de `requirements.txt` del commit usando paquetes binarios de PyPI. Las dependencias transitivas se resuelven al instalar; no hay actualización silenciosa del proyecto original.
- Antes de marcar la instalación como lista, comprueba imports, imágenes y la carga de ambos modelos. Esta comprobación no abre la webcam.
- **Cancelar** interrumpe la descarga o el proceso de instalación. **Reparar instalación** prepara otra copia y solo sustituye la activa tras verificarla. Una reparación fallida conserva la anterior.
- **Desinstalar** elimina las carpetas administradas de la integración. Conserva las fuentes ocultas en OBS y la selección de escena en Neo Twitch. No elimina un Python del sistema ni otras instalaciones del proyecto.
- Los archivos de instalación y el proceso de cámara usan un bloqueo para evitar que otra instancia repare o elimine la integración mientras está en uso.

### Autoría y distribución

El enlace y el crédito al creador aparecen en la interfaz y en `current/CREDITS.txt`. La descarga conserva los archivos originales, sin alterar el detector. El repositorio consultado no declara una licencia en su raíz: el crédito no concede permisos adicionales para redistribuir o adaptar su contenido. Antes de empaquetar o publicar una variante del proyecto externo, debe aclararse el permiso del titular. La licencia MIT de Neo Twitch no se extiende al contenido descargado.

### Validación

Pruebas automatizadas cubren cancelación previa a reparación, aislamiento de carpetas al desinstalar, bloqueo entre instaladores y rechazo de descargas con hash incorrecto. Se ha probado la instalación completa en Windows x64 y la carga de los modelos sin cámara. La prueba visual con webcam y OBS requiere ambos disponibles; no se sustituye por la comprobación de instalación.
