# Live Captions y Chat Translator

Neo Twitch ofrece dos integraciones **independientes** del creador [sayonari / Ryota Nishimura y saatan](https://www.sayonari.com/trans_asr/):

| Integración | Proyecto | Entrada | Salida |
|---|---|---|---|
| Live Captions | [jimakuChan](https://github.com/sayonari/jimakuChan) | Micrófono predeterminado de Chrome | Subtítulos originales y traducción opcional en OBS |
| Chat Translator | [twitchTransFreeNext](https://github.com/sayonari/twitchTransFreeNext) | Mensajes del canal conectado en Neo Twitch | Traducciones en OBS y, opcionalmente, mensajes en Twitch |

Cada una tiene instalación, configuración, proceso, fuente de OBS y controles separados. Activar o detener una no inicia ni detiene la otra. Ninguna se activa automáticamente al abrir Neo Twitch. También pueden usarse junto a MeowMeowCatCam.

## Live Captions

1. Abre **Integraciones → Live Captions → Instalar**. Se descarga una versión fija del proyecto original con su licencia MIT.
2. Conecta OBS en **Conexiones**. Actualiza las escenas en la integración y elige la escena de destino.
3. Configura idioma hablado, traducción opcional, texto original, tamaño, color, duración y posición superior/inferior.
4. Pulsa **Activar en OBS**. Neo Twitch abre una ventana de **Google Chrome**, con un perfil dedicado para esta integración.
5. En esa ventana pulsa **Iniciar micrófono** y concede el permiso de Chrome. Mantén abierta la ventana durante el stream. Si usas traducción, Chrome puede necesitar descargar el modelo del idioma la primera vez.

La fuente `NeoTwitch · Live Captions` tiene fondo transparente. **Probar apariencia** muestra texto de ejemplo en OBS; no graba audio ni llama al traductor.

Se requiere Google Chrome; la traducción integrada requiere una versión y un equipo compatibles con la API Translator de Chrome (138 o posterior). El reconocimiento de voz utiliza el servicio de Google y requiere internet. La traducción se realiza mediante los modelos locales de Chrome; no requiere una clave API. El audio no se guarda en Neo Twitch. La selección del micrófono se realiza en Chrome/Windows.

Al detener se cierra la sesión de Chrome iniciada para esta integración. Si Neo Twitch deja de responder, la página detiene el reconocimiento tras perder contacto con el servidor local. Pausar desde Chrome conserva la integración abierta y permite reanudarla.

## Chat Translator

1. Abre **Integraciones → Chat Translator → Instalar**. Neo Twitch prepara Python 3.10 privado y las dependencias originales. No cambia el Python del sistema.
2. Conecta Twitch y OBS desde **Conexiones**. No necesitas crear otro bot ni copiar tokens en archivos Python.
3. Elige la escena, el idioma principal del canal y el idioma secundario de traducción. Los mensajes extranjeros se traducen al principal; los del idioma principal se traducen al secundario, conforme a la lógica del proyecto original.
4. Configura la apariencia, la duración y los usuarios ignorados; pulsa **Activar en OBS**.

La fuente `NeoTwitch · Chat Translator` es distinta de la de Live Captions. Por defecto se coloca arriba; Live Captions se coloca abajo, para poder usar ambas en la misma escena.

**Publicar también las traducciones en mi chat de Twitch** está desactivado por defecto. Al marcarlo se enviarán mensajes desde la cuenta conectada a Neo Twitch, con prefijo `[NT Traducción]`. No se activa lectura en voz alta. Los comandos y los mensajes con ese prefijo se ignoran para evitar bucles. La cola admite hasta 20 mensajes pendientes y descarta los más antiguos si se llena; hay una pausa mínima de dos segundos entre traducciones para evitar ráfagas.

La traducción utiliza Google mediante las dependencias del proyecto original: requiere internet y está sujeta a la disponibilidad del servicio. Los textos se envían a ese servicio. El proyecto mantiene una caché SQLite local de traducciones en su carpeta; **Desinstalar** elimina esa caché junto con la instalación. Neo Twitch entrega al proceso Python únicamente los mensajes y las opciones de traducción: las credenciales de Twitch permanecen en Neo Twitch.

## Instalación y cierre

Las carpetas están en `%LocalAppData%\NeoTwitch\integrations\live-captions` y `chat-translator`. Se descargan archivos únicamente al instalar o reparar. Los paquetes se verifican con SHA-256 y una reparación fallida conserva la instalación anterior.

Los archivos originales se mantienen separados del código de Neo Twitch y conservan sus licencias MIT. Neo Twitch incluye sus propios adaptadores de comunicación y presentación, y muestra los enlaces y créditos de los autores. Revisiones fijadas:

- jimakuChan: `4a31ac35042e7dd6587ae201e094fed260c07395`.
- twitchTransFreeNext: `b55e75aebdc0a8b9becf48e6dd062d7be5862bf6`.

**Detener** oculta únicamente la fuente de esa integración. **Salir** de Neo Twitch detiene todas sus integraciones; minimizar a la bandeja las mantiene funcionando. Desinstalar conserva las opciones de Neo Twitch y las fuentes ocultas de OBS. Live Captions también conserva el perfil de Chrome con sus permisos y modelos, para futuras instalaciones.

## Validación de desarrollo

- `dotnet test NeoTwitch.Tests/NeoTwitch.Tests.csproj -c Release`: incluye aislamiento de servidores y configuraciones, acceso local, caracteres Unicode y suscripción de chat solo cuando corresponde.
- `node scripts/test-subtitle-assets.mjs`: verifica el adaptador de voz, traducción, pausa, pérdida de conexión y caducidad de subtítulos con motores simulados, sin micrófono ni servicios externos.
- Instaladores completos probados en Windows; Chat Translator verificado con una frase de muestra traducida realmente, sin publicar en Twitch.
- La prueba de voz con micrófono real, la visualización en OBS y la publicación opcional en un canal requieren una sesión de usuario; no se consideran verificadas por las pruebas simuladas.
