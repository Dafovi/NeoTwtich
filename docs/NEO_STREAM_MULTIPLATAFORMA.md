# Neo Stream: hoja de ruta multiplataforma

## Objetivo

Neo Stream conserva el motor de alertas, OBS, Arduino, luces virtuales, audio y multimedia de Neo Twitch. La diferencia es que esos efectos pueden responder a eventos recibidos desde más de una plataforma de streaming.

Una persona podrá conectar Twitch y YouTube a la vez, mantener una sola biblioteca de efectos y decidir por regla qué plataformas pueden dispararla. Por seguridad, las reglas creadas antes de este cambio se migran como `solo Twitch`; nunca pasan a reaccionar a otros servicios de forma automática.

## Base ya implementada

- `StreamEvent` es el formato común de eventos entrantes.
- `IStreamingPlatformProvider` es el contrato que implementará cada conector.
- Twitch ya funciona a través de `TwitchStreamingPlatformProvider`, que adapta EventSub sin cambiar sus alertas actuales.
- `PlatformEventRouter` deduplica por `plataforma + id del evento`. Un evento de Twitch y uno de YouTube que tengan el mismo identificador local no se confunden.
- Cada `EventRule` tiene `SourcePlatforms`. La selección actual por defecto es Twitch.
- La configuración de usuario usa el esquema 3 e incluye `StreamingPlatforms`, conservando `autoConnectTwitch` para compatibilidad durante la transición.

## Orden de implementación

### 1. Twitch: referencia funcional

Twitch es el proveedor de referencia. Sus eventos llegan por EventSub WebSocket al equipo local, se convierten en `StreamEvent` y luego se adaptan temporalmente al motor de alertas existente. Esto permite avanzar sin cambiar el comportamiento que ya usan los streamers.

Pendiente dentro de esta fase: sustituir los últimos nombres internos heredados de Twitch en el motor de alertas por nombres neutrales, manteniendo una migración de configuración.

### 2. YouTube: siguiente proveedor

YouTube puede ser el primer proveedor nuevo porque la app de escritorio puede autorizar al usuario con OAuth y leer el chat en directo. Neo Stream deberá soportar inicialmente:

- Mensajes de chat y comandos.
- Membresías y regalos de membresías cuando la API los entregue.
- Super Chats y Super Stickers como contribuciones de plataforma.
- Estado de directo para aplicar la misma protección que evita alertas invasivas fuera de stream.

Antes de programar la conexión real se necesita crear un proyecto de Google Cloud para Neo Stream, habilitar YouTube Data API v3 y crear un cliente OAuth de escritorio. Ese Client ID es público; no se debe incluir un Client Secret de una aplicación de escritorio.

### 3. Kick: proveedor con relay

Kick ofrece OAuth y eventos por webhook, pero un webhook necesita una URL HTTPS pública a la que Kick pueda enviar sus solicitudes. Una aplicación de escritorio no puede exponer de forma fiable esa URL desde la red doméstica.

La solución recomendada es un relay pequeño y propio:

1. Kick entrega el webhook al relay HTTPS.
2. El relay valida la firma de Kick y reenvía el evento a la sesión autenticada de Neo Stream por un canal seguro.
3. Neo Stream lo transforma a `StreamEvent` y usa el mismo router que Twitch.

El relay no debe guardar audio, vídeo ni configuraciones de alertas. Solo validará, encolará durante unos segundos si la app reconecta y reenviará eventos. Su despliegue y coste se decidirán antes de habilitar Kick en una versión pública.

### 4. TikTok: investigación y aprobación

TikTok requiere que las integraciones se sometan a revisión para estar en producción. No se añadirá una integración basada en APIs no oficiales ni scraping. Primero se definirá qué eventos Live oficiales están disponibles y, si cubren las alertas necesarias, se preparará la solicitud de revisión con un vídeo de demostración.

## Reglas y multistream

- Una alerta puede seleccionar una o varias plataformas de origen.
- Una alerta de Twitch existente se mantiene exclusivamente en Twitch después de actualizar.
- Cada plataforma conserva sus propios estados de conexión, credenciales y salud.
- Los comandos de chat y las respuestas automáticas se enviarán de vuelta a la plataforma que originó el evento, nunca a Twitch por defecto.
- El identificador de deduplicación incluye la plataforma. Si una persona apoya en Twitch y YouTube, son dos eventos reales y se pueden tratar como tales.
- Más adelante habrá una opción explícita de enfriamiento global para quienes quieran limitar efectos simultáneos durante multistream.

## Cambio de marca

El nombre visible de producto será Neo Stream. La primera versión de la transición conservará identificador de instalación, ejecutable y carpeta de datos actuales para que las actualizaciones no creen otra instalación ni pierdan configuraciones. El cambio de iconos, recursos de YouTube/Kick y créditos se hará cuando estén disponibles los archivos definitivos.

## Pruebas requeridas

- Tests unitarios de conversión de cada proveedor a `StreamEvent`.
- Tests de reglas para una plataforma, varias plataformas y eventos no compatibles.
- Tests de deduplicación por plataforma.
- Pruebas manuales con cuentas de prueba: solo Twitch, solo YouTube y Twitch + YouTube.
- Para Kick, pruebas de firma, reintentos y reconexión del relay.
- Para cada proveedor, una prueba de stream offline que confirme que se muestra notificación sin activar luces, OBS ni audio.
