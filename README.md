# Neo Twitch

App de Windows en .NET/WPF para escuchar eventos de Twitch y activar audio local y tiras NeoPixel por Arduino.

## Descargar

La forma mas facil de usar la app es descargar el release mas reciente desde GitHub:

[Descargar ultimo release](https://github.com/Dafovi/NeoTwtich/releases/latest)

También puedes descargar el sketch para Arduino desde aquí:

[Descargar Sketch](NeoTwitch/Arduino/NeoTwitchNeoPixel/NeoTwitchNeoPixel.ino)

Despues de descargar el `.zip`:

1. Descomprime la carpeta completa.
2. Abre `NeoTwitch.exe`.
3. No ejecutes el `.exe` directamente dentro del `.zip`.
4. No borres los archivos que vienen junto al `.exe`.

## Que hace

- Escucha EventSub por WebSocket para seguidores, suscripciones, raids, bits, comandos de chat y canjes personalizados.
- Permite crear reglas con luces, audio o ambos.
- Puede enviar mensajes personalizados al chat por regla.
- Puede enviar eventos opcionales a una Skill/relay de Alexa para activar rutinas.
- Puede mantener un fondo opcional con Alexa, por ejemplo enviando eventos de luz encendida o luz apagada.
- Las reglas nuevas vienen activas, pero con luces, audio y chat desactivados para configurar solo lo necesario.
- La interfaz oculta opciones que no aplican al evento, patron o fondo seleccionado.
- Guarda la configuracion en `%AppData%\NeoTwitch\settings.json`.
- Permite exportar e importar la configuracion en un archivo `.json`.
- Crea backups automaticos antes de sobrescribir la configuracion.
- Permite configurar cola de alertas para evitar que muchas alertas se acumulen sin control.
- Incluye volumen global para audios de alertas.
- El cierre con `X` puede mandar la app a segundo plano o cerrar por completo, segun la configuracion.
- Muestra una notificacion cuando queda en segundo plano.
- Muestra la version instalada y avisa si GitHub tiene una release mas reciente.
- Incluye modo claro y modo oscuro desde el panel `Configuracion`.
- Usa un Arduino por puerto COM y permite configurar varias tiras NeoPixel en distintos pines.
- Cuando una regla tiene audio y luces, la duracion del patron se ajusta a la duracion real del audio.
- Al terminar un evento, la app manda `STOP` y restaura el fondo configurado.

## Ejemplo rapido

Esta es una regla de seguidor configurada para activar luces, reproducir audio y enviar un mensaje al chat.

![Ejemplo de regla de seguidor](docs/images/muestra-seguidor.png)

Y asi se ve el efecto cuando se activa en stream:

![Ejemplo del efecto activado](docs/images/muestra-seguidor-activado.gif)
[Canal de Twtich](https://www.twitch.tv/cartafilou)

## Requisitos

- Windows con .NET Desktop Runtime compatible con el proyecto.
- Una app creada en Twitch Developer Console para obtener el Client ID. El Client Secret es opcional, pero ayuda a refrescar la sesion sin autorizar de nuevo.
- Opcional: una Skill/relay de Alexa con endpoint HTTPS para recibir eventos de Neo Twitch.
- Arduino IDE con la libreria `Adafruit NeoPixel`.
- Arduino conectado por USB y una tira NeoPixel en el pin configurado en el sketch.

## Primer uso

1. Abre la app.
2. Consigue el Client ID siguiendo la seccion `Conseguir el Client ID de Twitch`.
3. Escribe ese Client ID en el panel `Twitch`. Si quieres que la app refresque la sesion automaticamente, pega tambien el Client Secret.
4. Presiona `Conectar Twitch`, autoriza en el navegador y usa el codigo que muestra la app.
5. Carga el sketch `NeoTwitch/Arduino/NeoTwitchNeoPixel/NeoTwitchNeoPixel.ino` en cada Arduino.
6. En la app, usa `Detectar` o escribe el puerto COM del Arduino, por ejemplo `COM3`.
7. En `Luces de fondo`, agrega cada salida Arduino con nombre, pin y cantidad de LEDs.
8. En `Luces de fondo`, elige `Arduino Tira led ws2812b` para el fondo LED o `Alexa` para el fondo con rutinas/dispositivos Alexa.
9. Crea o edita reglas. Por defecto quedan activas, pero sin luces, audio ni chat automatico.
10. Activa solo lo que necesites en cada regla; la app muestra los campos que aplican segun el evento, patron y opciones marcadas.
11. Para comandos de chat, elige el evento `Comando de chat` y escribe el comando, por ejemplo `!baile`.
12. Para bits, crea varias reglas `Bits` con distintos `Bits minimos`; si llega una cantidad alta, se usa el umbral mas alto que aplique.
13. Si quieres chat automatico, activa `Enviar mensaje al chat` y usa variables como `{user}`, `{bits}`, `{reward}`, `{viewers}`, `{message}` o `{event}`.
14. Si tienes Alexa configurada, activa `Enviar evento a Alexa`. Neo Twitch enviara el nombre de la regla como evento.
15. Usa `Probar regla` antes de salir en vivo. La prueba ejecuta luces, audio, chat y Alexa si estan activados en esa regla.
16. En `Configuracion`, ajusta volumen, modo oscuro, comportamiento de cierre y cola de alertas.

## Configuracion general

La seccion `Configuracion` concentra opciones que no dependen de Twitch, Arduino o Alexa:

- `Abrir minimizada`: inicia Neo Twitch sin mostrar la ventana principal.
- `Cerrar con X envia a segundo plano`: si esta activo, la `X` oculta la ventana y la app sigue escuchando eventos en la bandeja del sistema. Si esta apagado, la `X` cierra la app por completo.
- `Modo oscuro`: cambia el tema visual.
- `Volumen de alertas`: controla el volumen global de los audios reproducidos por reglas.
- `Exportar configuracion`: guarda una copia manual del `settings.json` para moverla a otro PC o conservar un respaldo.
- `Importar configuracion`: reemplaza la configuracion actual con un archivo exportado.
- `Ejecutar diagnostico`: genera un reporte de version, archivos, Twitch, Arduino, Alexa, reglas, audios, fondo y cola sin disparar eventos.

El archivo exportado puede incluir tokens, URLs y secretos privados. Guardalo en un lugar seguro y no lo publiques.

Neo Twitch tambien crea backups automaticos en:

```text
%AppData%\NeoTwitch\backups
```

Ademas mantiene un backup rapido en:

```text
%AppData%\NeoTwitch\settings.backup.json
```

### Cola de alertas

Cuando Twitch manda muchos eventos seguidos, Neo Twitch ejecuta una alerta a la vez para evitar que audio y luces se superpongan. La cola configurable decide que se acepta y que se descarta mientras una alerta esta sonando.

- `Repetidas maximas en cola`: cuantas alertas de la misma regla pueden esperar. El valor recomendado por defecto es `1`.
- `Tiempo minimo para repetir`: milisegundos que deben pasar desde que empieza una regla antes de permitir otra igual. Usa `0` para desactivar ese filtro.
- `Distintas maximas en cola`: cuantas alertas de reglas diferentes pueden esperar.
- `Tiempo minimo para distintas`: milisegundos que deben pasar desde que empieza una regla antes de permitir otra diferente. Usa `0` para desactivar ese filtro.

Si una alerta se descarta por estos limites, aparece un mensaje `Cola: descarte...` en la consola en vivo.

## Cerrar y actualizar

Por defecto, la `X` de la ventana oculta la app para que siga escuchando el stream. Para cerrarla de verdad, usa el icono de la bandeja del sistema y elige `Salir`, o desactiva `Cerrar con X envia a segundo plano` en `Configuracion`.

La app guarda la configuracion en cada cambio y tambien al ocultar/cerrar. Si no carga lo anterior, revisa la ruta que aparece en `Configuracion`.

## Versiones y actualizaciones

La version instalada aparece abajo a la izquierda como `V1.3.1`.

Cada vez que se abre, Neo Twitch consulta la ultima release publicada en GitHub. Si hay una version mas reciente, muestra un aviso y puede abrir la pagina de descargas:

[Releases de Neo Twitch](https://github.com/Dafovi/NeoTwtich/releases/latest)

La app no se actualiza sola. Esto evita pedir permisos extra o instalar un launcher; el usuario decide si descarga el nuevo `.zip` desde GitHub.

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
11. Opcional: copia el `Client Secret` y pegalo en `Client Secret opcional`.

El `Client Secret` se guarda en tu archivo local de configuracion. Si no quieres guardarlo, puedes dejarlo vacio; cuando el token de Twitch expire, la app puede pedir autorizar otra vez.

## Permisos de Twitch usados

La app pide los scopes minimos para los eventos implementados:

- `moderator:read:followers` para follows.
- `channel:read:subscriptions` para suscripciones.
- `channel:read:redemptions` para canjes personalizados.
- `bits:read` para cheers/bits.
- `user:read:chat` para detectar comandos de chat.
- `user:write:chat` para enviar mensajes al chat.

Raids no requieren un scope propio, pero la conexion por WebSocket usa el token de usuario autorizado.

## Conexion Arduino y NeoPixel

El Arduino se comunica con la app por USB/Serial. Las tiras NeoPixel se conectan al pin configurado en la app y en el sketch. Para tiras largas o de mucho consumo, usa una fuente externa adecuada para la tira LED.

![Plano de conexion Arduino y tira LED](docs/images/arduino-led-strip-wiring.jpg)

Recomendaciones importantes:

- Usa una fuente del mismo voltaje que la tira LED, por ejemplo 5V para tiras de 5V.
- Conecta el GND de la fuente externa con el GND del Arduino.
- Agrega una resistencia entre el pin de datos del Arduino y `DIN` de la tira.
- Agrega un capacitor entre positivo y negativo de la fuente cerca del inicio de la tira.
- Respeta la direccion de la tira LED marcada con flechas.

El plano y las recomendaciones de cableado salen de la documentacion de What Make Art: [Wiring LED Strip to Arduino](https://whatmakeart.com/arduino/wiring-led-strip-to-arduino/).

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

Desde la version con ACK, el sketch responde por serial para confirmar comandos recibidos:

```text
ACK|FX
ACK|STOP
ERR|BAD_COMMAND
```

Si la app no recibe `ACK`, sigue funcionando en modo compatible, pero es recomendable cargar el sketch actualizado para que los comandos de luces sean mas confiables.

## Integracion Alexa opcional

Neo Twitch puede enviar eventos a una integracion de Alexa para que tus rutinas controlen luces, enchufes, anuncios u otros dispositivos que ya tengas configurados en la app de Alexa.

Importante: esta parte requiere configuracion externa en Amazon Developer Console y AWS. Neo Twitch no controla dispositivos Alexa directamente; la app manda un evento HTTP a un relay, y ese relay se encarga de avisarle a Alexa.

Flujo esperado:

```text
Evento Twitch -> Regla Neo Twitch -> Skill/relay Alexa -> Rutina Alexa -> luces, enchufes, anuncios, etc.
```

Sigue la guia completa en [docs/ALEXA_SETUP.md](docs/ALEXA_SETUP.md). Ahi se explica como crear la Smart Home Skill, la Lambda, la Function URL, Account Linking con Login with Amazon, los valores exactos que van en cada campo y como configurar la regla en Neo Twitch.

Resumen corto:

1. Crear una Smart Home Skill con `Provision your own`.
2. Crear una Lambda en AWS y pegar su ARN en Alexa.
3. Crear una Function URL publica para que Neo Twitch mande eventos.
4. Configurar Account Linking con Login with Amazon.
5. Activar `Send Alexa Events`.
6. Crear una rutina en la app de Alexa.
7. En Neo Twitch, pegar la Function URL y activar `Enviar evento a Alexa` en cada regla que lo necesite.

## Documentacion oficial consultada

- Twitch EventSub WebSockets: https://dev.twitch.tv/docs/eventsub/handling-websocket-events/
- Tipos EventSub y scopes: https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/
- OAuth Device Code Flow: https://dev.twitch.tv/docs/authentication/getting-tokens-oauth/
- Twitch Refresh Tokens: https://dev.twitch.tv/docs/authentication/refresh-tokens/
- Conexion Arduino y tira LED: https://whatmakeart.com/arduino/wiring-led-strip-to-arduino/
- Crear Skills en Alexa Developer Console: https://www.developer.amazon.com/en-US/docs/alexa/devconsole/create-a-skill-and-choose-the-interaction-model.html
- Crear una Smart Home Skill: https://www.developer.amazon.com/en-US/docs/alexa/smarthome/create-skill-tutorial.html
- Configurar Account Linking en Alexa: https://developer.amazon.com/en-US/docs/alexa/smarthome/set-up-account-linking-tutorial.html
- Request access to Alexa Event Gateway: https://developer.amazon.com/en-US/docs/alexa/smarthome/authenticate-a-customer-permissions.html
- AWS Lambda Function URLs: https://docs.aws.amazon.com/lambda/latest/dg/urls-configuration.html
- Alexa SimpleEventSource: https://developer.amazon.com/en-US/docs/alexa/device-apis/alexa-simpleeventsource.html
