# Guia completa de integracion con Alexa

Esta guia explica como conectar Neo Twitch con Alexa para que un evento de Twitch pueda disparar una rutina de Alexa.

La idea es esta:

```text
Twitch -> Neo Twitch -> URL publica del relay -> AWS Lambda -> Alexa Event Gateway -> Rutina Alexa
```

Neo Twitch ya hace su parte: envia un `POST` HTTP cuando una regla tiene activada la opcion `Enviar evento a Alexa`.

Lo que hay que crear afuera de Neo Twitch es:

- una Smart Home Skill en Alexa Developer Console,
- un backend/relay en AWS Lambda,
- una URL publica para que Neo Twitch llame ese backend,
- Account Linking con Login with Amazon,
- una rutina en la app de Alexa.

## Importante antes de empezar

Alexa no deja que una app local de Windows controle directamente tus dispositivos. Por eso no sirve pegar una URL cualquiera dentro de la consola de Alexa.

Hay dos valores distintos:

- `AWS Lambda ARN`: va en Alexa Developer Console, en `Build > Smart Home > Default endpoint`.
- `Function URL`: va en Neo Twitch, en `Conexiones > Alexa > URL de Skill / relay`.

Los dos pueden apuntar a la misma Lambda, pero no son el mismo texto.

Ejemplo de `AWS Lambda ARN`:

```text
arn:aws:lambda:us-east-1:123456789012:function:neo-twitch-alexa-relay
```

Ejemplo de `Function URL`:

```text
https://abc123xyz.lambda-url.us-east-1.on.aws/
```

## Mapa de valores

Esta tabla evita mezclar las consolas. Hay valores que salen de Alexa Developer Console, otros salen de AWS y otros los creas tu.

| Valor | Donde sale | Donde se usa |
| --- | --- | --- |
| `Skill ID` | Alexa Developer Console > tu Skill > `Build > Smart Home` | AWS Lambda trigger `Alexa Smart Home` y variable `ALEXA_SKILL_ID` |
| `Function ARN` | AWS Lambda > pantalla principal de la funcion | Alexa Developer Console > `Build > Smart Home > Default endpoint` |
| `Function URL` | AWS Lambda > `Configuration > Function URL` | Neo Twitch > `Conexiones > Alexa > URL de Skill / relay` |
| `Relay token privado` | Lo creas tu | AWS Lambda variable `NEO_TWITCH_RELAY_TOKEN` y Neo Twitch > `Token opcional` |
| `Client ID` de Login with Amazon | Amazon Developer > Login with Amazon > Security Profile | Alexa Developer Console > `Build > Account Linking > Your Client ID` |
| `Client Secret` de Login with Amazon | Amazon Developer > Login with Amazon > Security Profile | Alexa Developer Console > `Build > Account Linking > Your Secret` |
| `Alexa Client ID` | Alexa Developer Console > `Build > Permissions`, al activar `Send Alexa Events` | AWS Lambda variable `ALEXA_EVENT_CLIENT_ID` |
| `Alexa Client Secret` | Alexa Developer Console > `Build > Permissions`, al activar `Send Alexa Events` | AWS Lambda variable `ALEXA_EVENT_CLIENT_SECRET` |
| `DynamoDB table name` | AWS DynamoDB, cuando creas la tabla | AWS Lambda variable `ALEXA_TOKEN_TABLE` |

El `Client ID` de Login with Amazon y el `Alexa Client ID` no son lo mismo. Tienen nombres parecidos, pero salen de lugares distintos y sirven para cosas distintas.

## Cuentas necesarias

Necesitas:

1. Una cuenta de Amazon Developer.
2. Una cuenta de AWS.
3. La app de Alexa en el celular, iniciada con la misma cuenta de Amazon que usas para probar la Skill.
4. Neo Twitch instalado y configurado con Twitch.

Amazon puede pedir crear un perfil de empresa/desarrollador y verificar identidad con documento oficial antes de habilitar la consola.

## Paso 1: Crear la Smart Home Skill

1. Abre Neo Twitch.
2. Ve a `Conexiones > Alexa`.
3. Presiona `Abrir Alexa Console`.
4. En Alexa Developer Console, entra a `Skills`.
5. Presiona `Create Skill`.
6. En `Skill name`, escribe `NeoTwitch` o el nombre que quieras.
7. En `Primary locale`, elige el idioma de la cuenta de Alexa. Para Colombia normalmente puedes usar `Spanish (Mexico)` si esa es la opcion disponible.
8. Presiona `Next`.
9. En `Choose a type of experience`, selecciona `Smart home`.
10. En `Choose a model`, selecciona `Smart Home`.
11. En `Hosting services`, selecciona `Provision your own`.
12. Presiona `Next`.
13. Revisa que la pantalla diga:
    - `Type of experience`: `Smart home`
    - `Model`: `Smart Home`
    - `Hosting service`: `Provision your own`
14. Presiona `Create Skill`.

Cuando termine, la consola abre la Skill en la pestaña `Build`.

## Paso 2: Guardar el Skill ID

1. En la Skill, ve a `Build > Smart Home`.
2. En `Payload version`, selecciona `v3 (preferred)`.
3. En `Smart Home service endpoint`, copia el `Skill ID`.
4. Guardalo en un lugar seguro porque lo vas a usar en AWS Lambda.

No llenes todavia `Default endpoint`. Ese campo se llena despues de crear la Lambda.

## Paso 3: Crear un Security Profile en Login with Amazon

Esto sirve para llenar `Account Linking`.

1. Abre la consola de Login with Amazon:

```text
https://developer.amazon.com/loginwithamazon/console/site/lwa/overview.html
```

2. Presiona `Create a New Security Profile`.
3. Llena:
   - `Security Profile Name`: `NeoTwitch Alexa`
   - `Security Profile Description`: `Integracion de Neo Twitch con rutinas de Alexa`
   - `Consent Privacy Notice URL`: una URL publica HTTPS con tu politica de privacidad.
   - `Consent Logo Image`: puedes usar el logo de Neo Twitch si quieres.
4. Presiona `Save`.
5. En el perfil creado, busca `Show Client ID and Client Secret`.
6. Copia y guarda:
   - `Client ID`
   - `Client Secret`

El `Consent Privacy Notice URL` debe ser una pagina publica. Si no tienes sitio web, puedes publicar una pagina simple en GitHub Pages o usar una pagina publica del repo, pero lo ideal es tener una politica de privacidad real.

## Paso 4: Crear la Lambda en AWS

La Lambda es el backend. Alexa la llama por ARN y Neo Twitch la llama por URL publica.

1. Entra a AWS Console:

```text
https://console.aws.amazon.com/
```

2. Cambia la region a `US East (N. Virginia) us-east-1` si tu Skill esta en `Spanish (Mexico)`, `Spanish (US)` o `English (US)`.
3. Busca `Lambda`.
4. Presiona `Create function`.
5. Elige `Author from scratch`.
6. Llena:
   - `Function name`: `neo-twitch-alexa-relay`
   - `Runtime`: `Node.js 20.x` o `Node.js 22.x`
   - `Architecture`: `x86_64`
7. En permisos, deja que AWS cree un role basico de Lambda.
8. Presiona `Create function`.

Cuando se cree la funcion:

1. Copia el `Function ARN` que aparece arriba.
2. Este es el valor que va en Alexa Developer Console como `Default endpoint`.

## Paso 5: Permitir que Alexa invoque la Lambda

1. En la Lambda, entra a `Configuration`.
2. En `Triggers`, presiona `Add trigger`.
3. Busca `Alexa Smart Home`.
4. Pega el `Skill ID` que copiaste en el paso 2.
5. Guarda.

Esto le da permiso a Alexa para llamar esa Lambda. Si no agregas este trigger, la Skill puede fallar aunque el ARN sea correcto.

## Paso 6: Crear la Function URL para Neo Twitch

Esta URL sera el endpoint HTTP que Neo Twitch llamara cuando pase un evento.

En la pantalla que muestra `Function URL`:

1. En la Lambda, entra a `Configuration`.
2. Entra a `Function URL`.
3. Presiona `Create function URL`.
4. En `Auth type`, selecciona `NONE`.
5. Guarda.
6. Copia la URL que genera AWS. Esa URL empieza parecido a `https://...lambda-url.us-east-1.on.aws/`.

Esa es la URL que despues pegas en Neo Twitch en `Conexiones > Alexa > URL de Skill / relay`.

Como `Auth type NONE` deja la URL accesible publicamente, vamos a protegerla con un token privado. Este token no sale de Amazon ni de AWS: lo creas tu. Debe ser un texto largo, dificil de adivinar, y se pone en dos lugares:

1. En AWS Lambda, como variable `NEO_TWITCH_RELAY_TOKEN`.
2. En Neo Twitch, en `Conexiones > Alexa > Token opcional`.

Ejemplo de formato de token privado:

```text
nt_9d8c6f4b2a_mi_token_largo_y_secreto
```

Ese valor es privado. Cada persona que instale su propia integracion debe crear el suyo.

## Paso 7: Configurar variables de entorno de Lambda

En la Lambda, ve a `Configuration > Environment variables`.

Primero agrega las variables que ya tienes:

| Variable | Valor |
| --- | --- |
| `NEO_TWITCH_RELAY_TOKEN` | El token privado que creaste en el paso 6 |
| `ALEXA_SKILL_ID` | El `Skill ID` de Alexa Developer Console > `Build > Smart Home` |
| `ALEXA_EVENT_REGION` | `NA` para Norteamerica |

Despues del paso 12, vuelves a esta misma pantalla y agregas estas dos:

| Variable | Valor |
| --- | --- |
| `ALEXA_EVENT_CLIENT_ID` | El `Alexa Client ID` que aparece en Alexa Developer Console > `Build > Permissions` despues de activar `Send Alexa Events` |
| `ALEXA_EVENT_CLIENT_SECRET` | El `Alexa Client Secret` que aparece junto al `Alexa Client ID` anterior |

Para agregar una variable:

1. En AWS Lambda, abre tu funcion.
2. Ve a `Configuration > Environment variables`.
3. Presiona `Edit`.
4. Presiona `Add environment variable`.
5. En `Key`, escribe el nombre de la variable.
6. En `Value`, pega el valor correspondiente.
7. Repite con cada variable.
8. Presiona `Save`.

El `Alexa Client ID` y `Alexa Client Secret` no son los mismos que el `Client ID` y `Client Secret` del Security Profile de Login with Amazon.

- Los de Login with Amazon se usan en `Account Linking`.
- Los de Alexa Developer Console > `Permissions` se usan en Lambda para pedir permiso de enviar eventos al Alexa Event Gateway.

Si todavia no ves el `Alexa Client ID` y `Alexa Client Secret`, deja esas variables pendientes por ahora. Vuelve a este paso despues de activar `Send Alexa Events` en el paso 12 y guardar la Skill.

## Paso 8: Guardar tokens de Alexa

Para enviar eventos a Alexa, la Lambda necesita guardar tokens por usuario.

La forma recomendada es DynamoDB:

1. En AWS Console, busca `DynamoDB`.
2. Presiona `Create table`.
3. Llena:
   - `Table name`: `NeoTwitchAlexaTokens`
   - `Partition key`: `id`
   - Type: `String`
4. Usa capacidad `On-demand`.
5. Crea la tabla.

Luego agrega esta variable de entorno en Lambda:

| Variable | Valor |
| --- | --- |
| `ALEXA_TOKEN_TABLE` | `NeoTwitchAlexaTokens` |

Ahora dale permiso a la Lambda para usar DynamoDB. Para una configuracion sencilla de prueba puedes usar la politica administrada `AmazonDynamoDBFullAccess`.

1. Vuelve a AWS Lambda.
2. Abre tu funcion `neo-twitch-alexa-relay`.
3. Ve a `Configuration > Permissions`.
4. En `Execution role`, abre el enlace del role. El nombre suele parecerse a `neo-twitch-alexa-relay-role-...`.
5. AWS abre IAM en una nueva pantalla.
6. Presiona `Add permissions`.
7. Elige `Attach policies`.
8. En el buscador escribe `dynamodb`.
9. Marca `AmazonDynamoDBFullAccess`.
10. Presiona `Add permissions`.

No quites la politica que ya tenia la Lambda para logs. Normalmente se llama `AWSLambdaBasicExecutionRole`.

Para una integracion publica o de produccion, lo ideal es crear una politica mas limitada solo para la tabla `NeoTwitchAlexaTokens`, pero `AmazonDynamoDBFullAccess` es la ruta mas clara para probar sin perderse en IAM.

Sin este almacenamiento, la Skill puede enlazarse pero no podra enviar eventos despues porque los tokens expiran y hay que refrescarlos.

## Paso 9: Codigo que debe tener la Lambda

El codigo se pega en AWS Lambda, no en Alexa Developer Console.

1. En AWS Lambda, abre tu funcion `neo-twitch-alexa-relay`.
2. Entra a la pestaña `Code`.
3. Abre el archivo `index.mjs`. Si tu Lambda creo `index.js`, puedes borrar su contenido y usar `index.mjs`, o crear un archivo nuevo llamado `index.mjs`.
4. Copia el contenido de [docs/alexa-lambda/index.mjs](alexa-lambda/index.mjs).
5. Pegalo en `index.mjs`.
6. Presiona `Deploy`.
7. Ve a `Configuration > Runtime settings`.
8. Confirma que `Handler` sea `index.handler`.

Ese archivo es una plantilla funcional para una sola cuenta de prueba. La Lambda reconoce dos tipos de entrada:

1. Directivas de Alexa, cuando Alexa llama el ARN.
2. Eventos HTTP de Neo Twitch, cuando la app llama la Function URL.

La Lambda implementa:

- `Alexa.Discovery.Discover`: devuelve los eventos virtuales que Alexa mostrara para rutinas.
- `Alexa.Authorization.AcceptGrant`: intercambia el codigo de Alexa por tokens del Event Gateway y los guarda.
- `POST` desde Neo Twitch: valida el token, lee `eventName`, lo compara con los eventos configurados y envia un `Alexa.SimpleEventSource.Event` a Alexa.
- Refresh de token: si el token de Alexa expira, usa el `refresh_token` para pedir uno nuevo.

Eventos sugeridos para descubrir:

| eventName de Neo Twitch | Nombre visible sugerido en Alexa |
| --- | --- |
| `seguidor` | Seguidor |
| `suscripcion` | Suscripcion |
| `raid` | Raid |
| `bits100` | Bits 100 |
| `canje_susto` | Canje susto |
| `luz_encendida` | Luz encendida |
| `luz_apagada` | Luz apagada |

La respuesta de Discovery debe exponer `Alexa.SimpleEventSource`. Estos eventos luego aparecen en la app de Alexa como disparadores para rutinas.

Neo Twitch envia como `eventName` el nombre de la regla. Si tu regla se llama `Seguidor`, la Lambda lo normaliza y lo compara con `seguidor` o con el nombre visible `Seguidor`. Para agregar mas eventos, agrega filas en `twitchEvents` dentro de [docs/alexa-lambda/index.mjs](alexa-lambda/index.mjs).

El boton `Probar Alexa` de Neo Twitch envia una prueba usando el evento `seguidor`. Para que esa prueba funcione, deja el evento `seguidor` en `twitchEvents` o cambia la prueba en el codigo de la app.

Importante: si solo llenas formularios pero no pegas y despliegas el codigo en Lambda, la integracion no funcionara. La configuracion y el codigo van juntos.

## Paso 10: Pegar el ARN en Alexa Developer Console

1. Vuelve a Alexa Developer Console.
2. Entra a tu Skill.
3. Ve a `Build > Smart Home`.
4. En `Payload version`, confirma `v3 (preferred)`.
5. En `Default endpoint`, pega el `Function ARN` de Lambda.
6. Si aparece `North America`, marcalo y pega el mismo ARN si tu Lambda esta en `us-east-1`.
7. Presiona `Save`.

## Paso 11: Revisar Models

1. Ve a `Build > Models`.
2. Deja `Smart Home` activado.
3. Deja `Custom` apagado.
4. Presiona `Save`.

No necesitas modelo Custom para rutinas basadas en eventos. `Custom` seria para frases de voz propias, que no es lo que Neo Twitch necesita en esta integracion.

## Paso 12: Activar Permissions

1. Ve a `Build > Permissions`.
2. Activa `Send Alexa Events`.
3. Copia el `Alexa Client ID` y el `Alexa Client Secret` que aparecen en esa pantalla o en la seccion de credenciales de permisos.
4. Presiona `Save`.
5. Vuelve a AWS Lambda > `Configuration > Environment variables` y agrega esos dos valores como:
   - `ALEXA_EVENT_CLIENT_ID`
   - `ALEXA_EVENT_CLIENT_SECRET`

Esto es obligatorio para que la Lambda pueda enviar eventos al Alexa Event Gateway.

## Paso 13: Llenar Account Linking

Ve a `Build > Account Linking`.

Esta parte se llena con Login with Amazon, usando el Security Profile que creaste en el paso 3. No se llena con datos de AWS IAM, Lambda ni DynamoDB.

En `Settings`:

| Campo | Valor |
| --- | --- |
| `Do you allow users to create an account or link to an existing account with you?` | Activado |
| `Allow users to link their account to your skill from within your application or website` | Activado |
| `Allow users to authenticate using your mobile application` | Apagado |

En `Security Provider Information`:

| Campo | Valor |
| --- | --- |
| `Authorization Grant Type` | `Auth Code Grant` |
| `PKCE Authorization` | Apagado |
| `Your Web Authorization URI` | `https://www.amazon.com/ap/oa` |
| `Access Token URI` | `https://api.amazon.com/auth/o2/token` |
| `Your Client ID` | `Client ID` del Security Profile del paso 3 |
| `Your Secret` | `Client Secret` del Security Profile del paso 3 |
| `Your Authentication Scheme` | `HTTP Basic (Recommended)` |
| `Scope` | `profile:user_id` |
| `Domain List` | Normalmente vacio con Login with Amazon |
| `Default Access Token Expiration Time` | `3600` |

En esta guia, los unicos valores propios que pegas en Account Linking son:

- el `Client ID` del Security Profile,
- el `Client Secret` del Security Profile.

Las URLs `https://www.amazon.com/ap/oa` y `https://api.amazon.com/auth/o2/token` se escriben tal cual. No son URLs tuyas.

Si `Domain List` se vuelve obligatorio, agrega:

```text
amazon.com
api.amazon.com
```

Despues:

1. Presiona `Save`.
2. Baja hasta `Alexa Redirect URLs`.
3. Copia todas las URLs que aparecen.

## Paso 14: Pegar Redirect URLs en Login with Amazon

Aqui volvemos al Security Profile del paso 3. No es AWS IAM. Es el perfil de Login with Amazon que creaste dentro de Amazon Developer Console.

1. Vuelve a Login with Amazon Console:

```text
https://developer.amazon.com/loginwithamazon/console/site/lwa/overview.html
```

2. Abre el Security Profile `NeoTwitch Alexa`.
3. Entra a `Web Settings`.
4. Presiona `Edit`.
5. En `Allowed Return URLs`, agrega todas las `Alexa Redirect URLs` que copiaste.
6. Guarda.

Si no haces esto, el enlace de cuenta suele fallar al volver desde Amazon a Alexa.

## Paso 15: Probar la Skill

1. En Alexa Developer Console, ve a `Test`.
2. Cambia `Test is disabled for this skill` a `Development`.
3. En el celular, abre la app de Alexa con la misma cuenta.
4. Ve a `More > Skills & Games > Your Skills > Dev`.
5. Busca tu Skill.
6. Presiona `Enable to use`.
7. Inicia sesion cuando aparezca Login with Amazon.
8. Acepta permisos.
9. Revisa CloudWatch Logs de la Lambda para confirmar que llego `Alexa.Authorization.AcceptGrant`.
10. Si la Lambda responde bien, Alexa deberia poder descubrir los eventos virtuales.

### Si aparece `Unable to link skill at this time`

Ese mensaje no dice la causa real. Para saber donde fallo, revisa si Lambda recibio algo.

Primero mira CloudWatch:

1. En AWS Lambda, abre `neo-twitch-alexa-relay`.
2. Ve a `Monitor`.
3. Presiona `View CloudWatch logs`.
4. Abre el log stream mas reciente.
5. Intenta vincular la Skill otra vez desde la app de Alexa.
6. Vuelve al log y busca mensajes cerca de esa hora.

Si ves logs de Lambda, el enlace si llego hasta el backend. Revisa especialmente:

- `Missing ALEXA_EVENT_CLIENT_ID or ALEXA_EVENT_CLIENT_SECRET`: faltan variables del paso 12.
- `LWA token request failed: 401 {"error_description":"Client authentication failed","error":"invalid_client"}`: `ALEXA_EVENT_CLIENT_ID` o `ALEXA_EVENT_CLIENT_SECRET` estan mal.
- `AccessDeniedException`: falta permiso de DynamoDB del paso 8.
- `ResourceNotFoundException`: el nombre de `ALEXA_TOKEN_TABLE` no coincide con la tabla.
- `LWA token request failed`: el `Alexa Client ID` o `Alexa Client Secret` de `Permissions` esta mal.
- `Runtime.ImportModuleError`: el codigo no esta desplegado o el runtime no es Node.js compatible.

Para corregir `invalid_client`:

1. En Alexa Developer Console, abre tu Skill.
2. Ve a `Build > Permissions`.
3. Confirma que `Send Alexa Events` este activado.
4. Copia de ahi el `Alexa Client ID` y el `Alexa Client Secret`.
5. En AWS Lambda, abre `Configuration > Environment variables`.
6. Revisa que existan estas variables con esos nombres exactos:
   - `ALEXA_EVENT_CLIENT_ID`
   - `ALEXA_EVENT_CLIENT_SECRET`
7. Pega los valores sin comillas, sin espacios al inicio o al final y sin saltos de linea.
8. Presiona `Save`.
9. Intenta vincular la Skill otra vez.

No uses aqui el `Client ID` del Security Profile de Login with Amazon. Ese otro `Client ID` solo va en `Build > Account Linking`.

Si no aparece ningun log de Lambda, el error esta antes de llegar al backend. Revisa:

- En `Build > Smart Home`, `Default endpoint` tiene el `Function ARN`, no la Function URL.
- En la Lambda existe el trigger `Alexa Smart Home` con el `Skill ID`.
- En `Build > Account Linking`, `PKCE Authorization` esta apagado.
- `Your Web Authorization URI` es exactamente `https://www.amazon.com/ap/oa`.
- `Access Token URI` es exactamente `https://api.amazon.com/auth/o2/token`.
- `Your Client ID` y `Your Secret` son los del Security Profile de Login with Amazon.
- Las `Alexa Redirect URLs` estan copiadas en `Allowed Return URLs` dentro del Security Profile.
- La Skill esta en modo `Development` en la pestaña `Test`.
- La app de Alexa esta usando la misma cuenta de Amazon Developer.

Despues de cambiar Account Linking, guarda la Skill y espera uno o dos minutos antes de probar otra vez.

## Paso 16: Crear una rutina de Alexa

1. En la app de Alexa, ve a `More > Routines`.
2. Crea una rutina nueva. El nombre de la rutina puede ser el que quieras; no tiene que coincidir con Neo Twitch.
3. En `When this happens`, busca la opcion de Smart Home o dispositivos/eventos.
4. Selecciona el evento virtual de Neo Twitch, por ejemplo `Seguidor`.
5. En `Alexa will`, escoge que quieres hacer:
   - prender luces,
   - cambiar color,
   - prender un enchufe,
   - decir una frase,
   - activar una escena.
6. Guarda la rutina.

## Paso 17: Configurar Neo Twitch

1. Abre Neo Twitch.
2. Ve a `Conexiones > Alexa`.
3. Activa `Activar integracion con Alexa`.
4. En `URL de Skill / relay`, pega la `Function URL` de Lambda.
5. En `Token opcional`, pega el mismo valor de `NEO_TWITCH_RELAY_TOKEN`.
6. Presiona `Probar Alexa`.
7. Abre una regla.
8. Activa `Enviar evento a Alexa`.
9. Guarda.

Neo Twitch enviara a Lambda el nombre de la regla. Por ejemplo:

| Nombre de regla en Neo Twitch | Evento que buscara la Lambda |
| --- | --- |
| `Seguidor` | `seguidor` |
| `Suscripcion` | `suscripcion` |
| `Raid` | `raid` |
| `bits100` | `bits100` |
| `Canje susto` | `canje_susto` |

La rutina de Alexa debe usar el evento virtual que aparece en la app de Alexa. El nombre de la rutina no importa; lo importante es que el evento virtual exista en `twitchEvents` dentro de la Lambda.

## Fondo Alexa

Neo Twitch tambien puede mantener un fondo con Alexa. Esto sirve, por ejemplo, para dejar un bombillo encendido mientras no hay eventos y restaurarlo cuando termina una regla.

En `Luces de fondo`, selecciona la salida `Alexa`:

1. Activa `Mantener fondo Alexa encendido`.
2. En `Evento Alexa encendido`, deja `luz_encendida` o escribe el evento que configuraste en tu Lambda.
3. En `Evento Alexa apagado`, deja `luz_apagada` o escribe el evento que configuraste en tu Lambda.
4. Si quieres que al terminar una regla se apague ese fondo, activa `Apagar fondo Alexa al finalizar evento`.
5. Guarda o presiona `Aplicar fondo Alexa`.

Cuando el fondo Alexa esta activo:

- Al aplicar el fondo, Neo Twitch envia `luz_encendida`.
- Si presionas `Apagar fondo Alexa`, Neo Twitch envia `luz_apagada`.
- Si una regla usa Alexa o luces, al terminar Neo Twitch vuelve a enviar `luz_encendida` o `luz_apagada` segun la configuracion elegida.

En la app de Alexa puedes crear dos rutinas:

- una rutina con el evento `Luz encendida` para prender el bombillo o escena de fondo,
- otra rutina con el evento `Luz apagada` para apagarlo.

## Payload que envia Neo Twitch

Neo Twitch envia un `POST` JSON parecido a este:

```json
{
  "source": "neo-twitch",
  "eventName": "seguidor",
  "ruleName": "Seguidor",
  "eventKind": "Nuevo seguidor",
  "userName": "Usuario",
  "rewardTitle": "",
  "bits": null,
  "viewerCount": null,
  "message": "",
  "title": "Usuario siguio el canal",
  "occurredAt": "2026-05-25T15:00:00Z"
}
```

Si configuras token en Neo Twitch, tambien envia:

```text
Authorization: Bearer TU_TOKEN
```

## Checklist rapido

Antes de probar, confirma:

- La Skill esta en modelo `Smart Home`.
- `Payload version` esta en `v3`.
- `Default endpoint` tiene el ARN de Lambda.
- La Lambda tiene trigger `Alexa Smart Home` con el Skill ID.
- La Lambda tiene Function URL publica.
- Neo Twitch usa la Function URL, no el ARN.
- `Send Alexa Events` esta activado.
- Account Linking usa Login with Amazon.
- Las Alexa Redirect URLs estan copiadas en el Security Profile.
- La Lambda guarda tokens de `AcceptGrant`.
- La rutina existe en la app de Alexa.

## Errores comunes

### Alexa dice que no puede enlazar la cuenta

Revisa:

- `Allowed Return URLs` en Login with Amazon.
- `Your Web Authorization URI`.
- `Access Token URI`.
- `Client ID` y `Client Secret`.
- Que `Scope` tenga `profile:user_id`.

### No llega nada a Lambda

Revisa:

- Que pegaste el ARN correcto en `Default endpoint`.
- Que agregaste el trigger `Alexa Smart Home`.
- Que el Skill ID del trigger coincide con la Skill.
- CloudWatch Logs de la Lambda.

### Neo Twitch dice que envio pero Alexa no hace nada

Revisa:

- Que Neo Twitch tenga la Function URL, no el ARN.
- Que el token de Neo Twitch coincide con `NEO_TWITCH_RELAY_TOKEN`.
- Que la Lambda mapea ese `eventName`.
- Que la Lambda tiene token valido del Alexa Event Gateway.
- Que existe una rutina usando ese evento.

### Funcionaba y dejo de funcionar

Revisa:

- Si el usuario deshabilito la Skill, los tokens dejan de servir.
- Si se perdieron los tokens, deshabilita y habilita la Skill otra vez para que Alexa envie un nuevo `AcceptGrant`.
- Revisa si la Lambda esta refrescando los tokens antes de que expiren.

## Documentacion oficial

- Crear Smart Home Skill: https://www.developer.amazon.com/en-US/docs/alexa/smarthome/create-skill-tutorial.html
- Implementar Smart Home Skill en Lambda: https://www.developer.amazon.com/fr-FR/docs/alexa/smarthome/implement-your-skill.html
- Account Linking con Login with Amazon: https://developer.amazon.com/en-US/docs/alexa/smarthome/set-up-account-linking-tutorial.html
- Authorization Code Grant: https://developer.amazon.com/en-US/docs/alexa/account-linking/configure-authorization-code-grant.html
- Request access to Alexa Event Gateway: https://developer.amazon.com/en-US/docs/alexa/smarthome/authenticate-a-customer-permissions.html
- Send events to Alexa Event Gateway: https://www.developer.amazon.com/en-US/docs/alexa/smarthome/send-events-to-the-alexa-event-gateway.html
- Alexa SimpleEventSource: https://developer.amazon.com/en-US/docs/alexa/device-apis/alexa-simpleeventsource.html
- AWS Lambda Function URLs: https://docs.aws.amazon.com/lambda/latest/dg/urls-configuration.html
