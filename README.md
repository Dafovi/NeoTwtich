# Luces Canje Twitch

App de Windows en .NET/WPF para escuchar eventos de Twitch y activar audio local y tiras NeoPixel por Arduino.

## Que hace

- Escucha EventSub por WebSocket para seguidores, suscripciones, raids, bits y canjes personalizados.
- Permite crear reglas con luces, audio o ambos.
- Puede enviar mensajes personalizados al chat por regla.
- Guarda la configuracion en `%AppData%\LucesCanjeTwitch\settings.json`.
- Se queda en segundo plano al cerrar o minimizar la ventana.
- Incluye modo claro y modo oscuro desde el panel `Inicio`.
- Usa un Arduino por puerto COM y permite configurar varias tiras NeoPixel en distintos pines.
- Cuando una regla tiene audio y luces, la duracion del patron se ajusta a la duracion real del audio.
- Al terminar un evento, la app manda `STOP` y restaura el fondo configurado.

## Requisitos

- Windows con .NET Desktop Runtime compatible con el proyecto.
- Una app creada en Twitch Developer Console para obtener el Client ID.
- Arduino IDE con la libreria `Adafruit NeoPixel`.
- Arduino conectado por USB y una tira NeoPixel en el pin configurado en el sketch.

## Primer uso

1. Abre la app.
2. Consigue el Client ID siguiendo la seccion `Conseguir el Client ID de Twitch`.
3. Escribe ese Client ID en el panel `Twitch`.
4. Presiona `Conectar Twitch`, autoriza en el navegador y usa el codigo que muestra la app.
5. Carga el sketch `LucesCanjeTwitch/Arduino/LucesCanjeNeoPixel/LucesCanjeNeoPixel.ino` en cada Arduino.
6. En la app, usa `Detectar` o escribe el puerto COM del Arduino, por ejemplo `COM3`.
7. En `Tiras LED`, agrega cada tira con nombre, pin de Arduino y cantidad de LEDs.
8. En `Tiras LED > Fondo`, configura si quieres un color o patron permanente mientras no haya eventos.
9. Ajusta reglas, colores, patron, pines de salida y audio.
10. Para bits, crea varias reglas `Bits` con distintos `Bits minimos`; si llega una cantidad alta, se usa el umbral mas alto que aplique.
11. Si quieres chat automatico, activa `Enviar mensaje al chat` y usa variables como `{user}`, `{bits}`, `{reward}`, `{viewers}`, `{message}` o `{event}`.
12. Usa `Probar regla` antes de salir en vivo.

## Cerrar y actualizar

La `X` de la ventana oculta la app para que siga escuchando el stream. Para cerrarla de verdad, usa el icono de la bandeja del sistema y elige `Salir`.

La app guarda la configuracion en cada cambio y tambien al ocultar/cerrar. Si no carga lo anterior, revisa la ruta que aparece al fondo del panel izquierdo.

## Conseguir el Client ID de Twitch

1. Entra a https://dev.twitch.tv/console con la cuenta de Twitch que va a crear la app.
2. Si Twitch lo pide, verifica el correo de la cuenta y activa 2FA.
3. Abre la pestaña `Applications`.
4. Haz clic en `Register Your Application`.
5. En `Name`, pon un nombre unico, por ejemplo `Luces Canje Twitch - NombreDelCanal`.
6. En `OAuth Redirect URLs`, agrega `http://localhost:3000`.
   - Esta app usa Device Code Flow, asi que no usa esa URL para iniciar sesion.
   - Twitch puede pedir una URL al registrar la app, por eso dejamos una local.
7. En `Category`, elige la categoria mas cercana, por ejemplo una de integracion/app si aparece.
8. Marca `I'm not a robot` y crea la app.
9. Vuelve a `Applications`, busca la app y entra en `Manage`.
10. Copia el valor `Client ID` y pegalo en la app de Windows.

No pegues ni compartas el `Client Secret`. Esta app no lo necesita.

## Permisos de Twitch usados

La app pide los scopes minimos para los eventos implementados:

- `moderator:read:followers` para follows.
- `channel:read:subscriptions` para suscripciones.
- `channel:read:redemptions` para canjes personalizados.
- `bits:read` para cheers/bits.
- `user:write:chat` para enviar mensajes al chat.

Raids no requieren un scope propio, pero la conexion por WebSocket usa el token de usuario autorizado.

## Conexion con Twitch

Esta version usa OAuth Device Code Flow y EventSub WebSocket. Para una app local de Windows es una combinacion practica porque no obliga a guardar un client secret ni a publicar un servidor HTTPS.

Alternativas posibles:

- Authorization Code Flow con un servidor local `localhost`: mas fluido para login, pero requiere registrar redirect URI y manejar callback local.
- EventSub Webhooks: buena opcion para un servicio en la nube, pero necesita URL publica HTTPS.
- Librerias de terceros: pueden reducir codigo propio, pero agregan dependencias y cambios externos.

## Protocolo Arduino

La app envia una linea serial por evento. El protocolo incluye tercer color, delay entre pasos, duracion del paso y comando de corte:

```text
FX|6:30,7:60|RAVE|180|6500|35|120|#FF2D55|#00D1FF|#FFFFFF
STOP|6:30,7:60
```

Campos:

- Comando: `FX`.
- Tiras destino: `pin:leds`, separadas por coma.
- Patron: `SOLID`, `PULSE`, `RAINBOW`, `CHASE`, `THEATER`, `SPARKLE`, `RAVE`.
- Brillo de 0 a 255.
- Duracion total en milisegundos. Si vale `0`, el patron sigue hasta recibir `STOP`.
- Delay entre pasos en milisegundos.
- Duracion del paso en milisegundos.
- Color principal `#RRGGBB`.
- Color secundario `#RRGGBB`.
- Tercer color `#RRGGBB`.

La app envia `STOP` cuando termina el audio o cuando necesita cortar el fondo para lanzar un evento.

## Documentacion oficial consultada

- Twitch EventSub WebSockets: https://dev.twitch.tv/docs/eventsub/handling-websocket-events/
- Tipos EventSub y scopes: https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/
- OAuth Device Code Flow: https://dev.twitch.tv/docs/authentication/getting-tokens-oauth/
