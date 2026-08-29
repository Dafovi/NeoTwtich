# Guia Tecnica de Neo Twitch

Esta guia resume como esta construido el proyecto despues del refactor. Esta pensada para poder moverse por el codigo, compilar, depurar y agregar funciones sin tener que buscar todo desde `MainWindow`.

## 1. Solucion

Archivos principales:

```text
NeoTwitch.slnx
Directory.Build.props
build.config.json
scripts/
```

Proyectos:

```text
NeoTwitch/             App WPF principal
NeoTwitch.Installer/   Instalador y actualizador
NeoTwitch.Tests/       Tests automatizados
Shared/                Codigo compartido entre app e instalador
```

`Directory.Build.props` contiene la version central:

```xml
<Version>2.2.4</Version>
```

`build.config.json` contiene rutas, nombres de artifacts, runtime y proyectos usados por los scripts.

## 2. Arquitectura General

La app sigue una estructura por dominio:

```text
NeoTwitch/
  App/
  Assets/
  Features/
  Resources/
  Shared/
```

### App

`NeoTwitch/App` contiene piezas de aplicacion:

- `Composition`: crea y agrupa servicios compartidos.
- `Configuration`: carga, guarda y migra configuracion.
- `Shell`: estado de ventana, navegacion y host principal.
- `Startup`: arranque, autoconexion y argumentos de inicio.
- `Theme`: aplicacion de tema y estilos dinamicos.

### Features

`NeoTwitch/Features` contiene funcionalidades por area:

```text
Activity
Alerts
Alexa
Arduino
Audio
Connections
Dashboard
Lights
Media
Obs
Settings
Twitch
VirtualLights
```

Cada feature puede tener:

- `Views`: XAML y code-behind visual.
- `ViewModels`: estado que consume la vista.
- `Services`: logica reutilizable.
- `Host`: metodos parciales de `MainWindow` que conectan la vista con servicios legacy.
- `Models`: tipos especificos del dominio.

### Shared

`NeoTwitch/Shared` contiene utilidades generales:

- `Configuration`: modelos de settings.
- `Diagnostics`: reportes.
- `IO`: rutas de archivos.
- `Models`: modelos compartidos.
- `Parsing`: parseos comunes.
- `Text`: catalogo de textos.
- `Theme`: tema claro/oscuro/sistema.
- `Ui`: helpers visuales.
- `ViewModels`: bases y modelos UI reutilizables.
- `Views`: controles/dialogos compartidos.

## 3. MainWindow despues del Refactor

`MainWindow` sigue existiendo porque WPF necesita una ventana principal y porque muchas piezas nacieron alli. La deuda grande se redujo partiendola en archivos parciales por dominio.

Ejemplos:

```text
NeoTwitch/App/Shell/MainWindow.Shell.cs
NeoTwitch/App/Startup/MainWindow.Startup.cs
NeoTwitch/Features/Alerts/Host/MainWindow.RuleTesting.cs
NeoTwitch/Features/Arduino/Host/MainWindow.ArduinoPorts.cs
NeoTwitch/Features/Obs/Host/MainWindow.Obs.cs
```

En C#, una clase `partial` permite dividir una misma clase en varios archivos. Para alguien que viene de Unity, se puede pensar como separar un MonoBehaviour gigante en varios archivos por responsabilidad, aunque sigue compilando como una sola clase.

La meta a futuro seria mover cada vez mas logica desde `Host` hacia servicios o comandos de ViewModel.

## 4. Views y XAML

WPF usa XAML para declarar UI.

Ejemplo simplificado:

```xml
<views:NeoTwitchView x:Class="NeoTwitch.Views.AudioView">
    <Grid>
        <TextBlock Text="Audio" />
    </Grid>
</views:NeoTwitchView>
```

El `x:Class` conecta el XAML con su archivo `.xaml.cs`.

`NeoTwitchView` es una base comun para vistas:

```text
NeoTwitch/Shared/Views/NeoTwitchView.cs
```

Las pestañas principales viven en features:

```text
NeoTwitch/Features/Dashboard/Views/DashboardView.xaml
NeoTwitch/Features/Connections/Views/ConnectionsView.xaml
NeoTwitch/Features/Alerts/Views/AlertsView.xaml
NeoTwitch/Features/Lights/Views/LightsView.xaml
```

## 5. ViewModels y Binding

Un ViewModel guarda el estado que la vista muestra.

Ejemplo conceptual:

```csharp
public string VersionText
{
    get => _versionText;
    set => SetProperty(ref _versionText, value);
}
```

En XAML:

```xml
<TextBlock Text="{Binding VersionText}" />
```

Cuando `VersionText` cambia, WPF actualiza la UI si el ViewModel notifica `PropertyChanged`.

Base comun:

```text
NeoTwitch/Shared/ViewModels/Core/ObservableObject.cs
```

ViewModels importantes:

```text
NeoTwitch/Features/Alerts/ViewModels/Alerts/
NeoTwitch/Features/Connections/ViewModels/Connections/
NeoTwitch/Features/Settings/ViewModels/Settings/
NeoTwitch/Features/Obs/ViewModels/Obs/
NeoTwitch/App/Shell/ViewModels/Shell/
```

## 6. Textos y Futuro Multi-Idioma

Los textos reutilizables estan en:

```text
NeoTwitch/Shared/Text/Services/UiTextKeys.cs
NeoTwitch/Shared/Text/Services/SpanishUiTextCatalog.cs
```

`UiTextKeys` define claves estables.

`SpanishUiTextCatalog` define el texto actual en espanol.

Esto evita que toda la logica tenga strings quemados y prepara el camino para multi-idioma.

Ejemplo:

```csharp
AddLog(_text.Get(UiTextKeys.StartupAutoConnectSuppressedLog));
```

## 7. Configuracion

La configuracion se guarda en:

```text
%AppData%\NeoTwitch\settings.json
```

Rutas compartidas:

```text
Shared/IO/ApplicationPaths.cs
```

Modelos de configuracion:

```text
NeoTwitch/Shared/Configuration/
NeoTwitch/Features/Alerts/Models/EventRule.cs
```

La app tambien migra desde la carpeta legacy:

```text
%AppData%\LucesCanjeTwitch
```

### Integridad, recuperación y credenciales

- El esquema persistido actual es `schemaVersion: 1`; no depende de la versión de la aplicación. Un archivo sin versión se interpreta como esquema 0 y pasa por la migración explícita `0 -> 1` antes de normalizar valores. Volver a cargar el esquema 1 es idempotente y una versión futura no soportada se conserva sin sobrescribirla.
- Cada solicitud de guardado captura una instantánea profunda antes de entrar en una cola de escritor único. Usa un staging único en el mismo directorio, escritura con flush a disco, relectura/validación y `File.Replace`/`File.Move` atómico. Una solicitud más antigua nunca sobrescribe otra ya confirmada con una secuencia posterior.
- Antes del reemplazo se conserva `settings.backup.json` y, una vez por sesión, un backup fechado. Los backups contienen las credenciales protegidas, nunca texto plano. Un primario corrupto recupera el último backup válido; si ambos están dañados se usan valores por defecto, se bloquean guardados automáticos y no se borran ni sobrescriben los archivos. Los `.staging.*` interrumpidos no participan en la recuperación.
- IDs de reglas, grupos, assets y tiras LED son no vacíos y únicos dentro de su propio dominio. Se conserva la primera aparición válida; duplicados reciben IDs nuevos. Referencias inexistentes se limpian. Si un ID duplicado hace ambigua una referencia, se desactiva y se informa en el arranque en vez de adivinar un destino.
- `TwitchClientSecret`, access/refresh tokens de Twitch, token de Alexa y contraseña de OBS se escriben como blobs DPAPI ligados al usuario actual de Windows. Los valores de ejecución siguen en memoria, pero sus campos JSON de texto plano quedan vacíos. Una credencial dañada o creada por otro usuario se omite sin impedir cargar el resto; los guardados quedan bloqueados hasta reemplazar las credenciales fallidas o confirmar una importación válida, evitando borrar el blob recuperable por accidente.
- Al cargar un archivo legacy con secretos en texto plano, la app primero crea y valida el estado protegido de forma atómica; solo después reemplaza o elimina la copia legacy. Si DPAPI falla, el original recuperable se conserva.
- La exportación normal es portable y excluye tanto secretos en texto plano como blobs protegidos; al importarla se requiere reautenticar integraciones. Los backups manuales y automáticos sí conservan blobs DPAPI, útiles únicamente para el mismo usuario de Windows.
- DPAPI reduce la exposición por copia casual de disco o archivos. No protege frente a software malicioso que ya se ejecute con la cuenta de Windows del usuario.

## 8. Arranque y Autoconexion

El arranque esta en:

```text
NeoTwitch/App/Startup/MainWindow.Startup.cs
```

Flujo:

1. Carga configuracion.
2. Aplica inicio con Windows si esta activo.
3. Si no se ejecuto con `--no-autoconnect`, intenta conectar servicios marcados.
4. Arduino se conecta antes que Twitch para que las alertas ya tengan luces listas.
5. Twitch usa token guardado y refresco si puede.
6. OBS se conecta si esta activo y marcado para reconectar.
7. Alexa se marca como configurada si tiene URL/token.

Argumentos utiles:

```text
--debug
--no-autoconnect
--no-start-hidden
--safe-mode
```

Perfiles de Visual Studio:

```text
Neo Twitch
Neo Twitch - Debug sin conexiones
Neo Twitch - Safe mode
```

## 9. Twitch

Carpetas:

```text
NeoTwitch/Features/Twitch/
```

Servicios principales:

- OAuth Device Code Flow para autorizar.
- Refresh token si hay credenciales suficientes.
- EventSub WebSocket para eventos.
- Consulta de stream live/offline con Helix.

Los eventos se convierten a modelos internos `TwitchEvent` y luego se comparan con `EventRule`.

Si el canal esta offline, las alertas se suprimen y se notifica en bandeja.

### Fiabilidad de EventSub

- Cada notificación se acepta por `metadata.message_id` antes de ejecutar efectos. La caché de deduplicación es compartida entre reconexiones, thread-safe, conserva como máximo 4096 IDs durante 10 minutos y expulsa primero el ID aceptado más antiguo. Un duplicado se registra y se ignora sin tratarlo como error.
- `TwitchEvent` conserva el ID de mensaje, el ID de sesión y el tipo EventSub para diagnóstico. El despacho hacia alertas es asincrónico y esperado por el bucle receptor; una notificación aceptada tiene semántica de máximo una ejecución durante la ventana, incluso si el manejador falla parcialmente.
- El `session_welcome` aporta el `keepalive_timeout_seconds`. Cualquier mensaje WebSocket válido —incluidos keepalive y notification— actualiza la frescura. Si no llega ninguno durante ese tiempo más 10 segundos, el socket pasa a `Stale`, se aborta y el único bucle propietario reconecta contra el endpoint base.
- La salud distingue `Disconnected`, `Connecting`, `Connected`, `Degraded`, `Reconnecting`, `Stale` y `Faulted`. Solo `Connected` significa socket activo y todas las suscripciones requeridas saludables.
- Todas las suscripciones derivadas de reglas activas son requeridas. El registrador devuelve un intento estructurado por tipo con éxito, código HTTP y diagnóstico. Un fallo requerido deja la sesión conectada pero `Degraded`; no se reintenta indefinidamente dentro de la misma sesión. Los tipos opcionales están soportados por el modelo y no degradan la salud, aunque actualmente el planificador no crea ninguno.
- Un `session_reconnect` usa la URL indicada por Twitch y no vuelve a crear suscripciones porque Twitch migra la sesión. Un cierre normal, timeout o fallo transitorio vuelve al endpoint base y crea suscripciones una vez. El cierre solicitado por la aplicación no reconecta.
- El refresh OAuth es single-flight por `TwitchAuthService`: los llamantes concurrentes esperan la misma operación. Cancelar un llamante cancela solo su espera, no el refresh compartido. El resultado se aplica únicamente si el token que originó la solicitud sigue siendo el actual, evitando sobrescribir credenciales más nuevas.

## 10. Alertas

Carpetas:

```text
NeoTwitch/Features/Alerts/
```

Conceptos:

- `EventRule`: configuracion guardada de una alerta.
- `RuleEditorViewModel`: estado editable de la alerta seleccionada.
- `EventRuleRowViewModel`: datos visuales de cada fila.
- `RuleEditorFormService`: copia datos desde el editor a la regla.
- `EventRuleSnapshotService`: detecta cambios sin guardar.
- `AlertExecutionPlanService`: decide que acciones se ejecutan.

Las acciones pueden incluir:

- Luces Arduino.
- Luces virtuales.
- Audio.
- Chat.
- Imagenes.
- Videos.
- OBS.
- Alexa.

La duracion de la alerta usa la duracion mas larga entre medios y efectos configurados.

## 11. Arduino

Carpetas:

```text
NeoTwitch/Features/Arduino/
NeoTwitch/Features/Lights/
```

Sketch:

```text
NeoTwitch/Features/Arduino/Sketch/NeoTwitchNeoPixel/NeoTwitchNeoPixel.ino
```

Protocolo serial:

```text
FX|6:30,7:60|RAVE|180|6500|35|120|#FF2D55|#00D1FF|#FFFFFF
STOP|6:30,7:60
```

Campos:

- Comando.
- Salidas `pin:leds`.
- Patron.
- Brillo.
- Duracion total.
- Delay entre pasos.
- Duracion del paso.
- Color principal.
- Color secundario.
- Tercer color.

Respuestas esperadas:

```text
ACK|FX
ACK|STOP
ERR|BAD_COMMAND
ERR|NO_MEMORY
```

El ACK ayuda a detectar si el sketch recibio el comando. Si se desconecta el COM, el monitor intenta reflejar el estado y reconectar si aplica.

## 12. Luces Virtuales

Carpetas:

```text
NeoTwitch/Features/VirtualLights/
```

Servicios:

- `VirtualLightsOverlayService`: genera overlay local para OBS.
- `VirtualLightsScreenOverlayService`: ventana superpuesta en pantalla.
- `VirtualScreenService`: detecta pantallas.

Las luces virtuales tienen configuracion propia y no reemplazan a las luces fisicas.

## 13. OBS

Carpetas:

```text
NeoTwitch/Features/Obs/
```

Servicios principales:

- `ObsWebSocketService`: conexion y requests al WebSocket de OBS.
- `ObsOverlayService`: URL/estado para fuente de navegador.
- Protocolos en `Services/Protocol`.

Fuentes usadas:

```text
Neo Twitch - Imagen de alerta
Neo Twitch - Video de alerta
Neo Twitch - Luces virtuales
Neo Twitch - Prueba imagen
Neo Twitch - Prueba video
```

El flujo WebSocket crea o actualiza fuentes sin que el usuario tenga que agregarlas manualmente. La fuente de navegador sigue existiendo como opcion para overlays fijos.

## 14. Audio, Imagenes y Videos

Carpetas:

```text
NeoTwitch/Features/Audio/
NeoTwitch/Features/Media/
```

Audio tiene servicios propios de reproduccion local.

Imagenes y videos usan el mismo patron de biblioteca:

- Assets.
- Grupos.
- Filtros.
- Prueba.
- Eliminacion.
- Seleccion individual o aleatoria por grupo desde alertas.

La meta del refactor fue evitar duplicar la misma logica para cada biblioteca.

## 15. Alexa

Carpetas:

```text
NeoTwitch/Features/Alexa/
docs/ALEXA_SETUP.md
```

Neo Twitch envia eventos HTTP a un relay propio. Ese relay expone eventos a Alexa.

La app no controla dispositivos Alexa directamente; eso ocurre en rutinas configuradas en Alexa.

## 16. Tema y UI

Tema:

```text
NeoTwitch/App/Theme/
NeoTwitch/Shared/Theme/
NeoTwitch/Shared/Ui/
```

El tema puede ser:

- Claro.
- Oscuro.
- Sistema.

El modo sistema lee Windows desde registro:

```text
Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
```

El selector de color esta en:

```text
NeoTwitch/Shared/Views/ColorPickerDialog.xaml
```

## 17. Instalador y Actualizador

Proyecto:

```text
NeoTwitch.Installer/
```

Piezas:

- `GitHubReleaseClient`: lee el ultimo release y elige el asset instalable.
- `InstallerService`: descarga, extrae/copia y crea accesos.
- `InstallerOptions`: parsea argumentos.

El instalador ignora assets que contienen `Installer` para no instalarse a si mismo como paquete de app.

Cuando la app encuentra una version nueva, busca un instalador local. Si existe, lo copia a una ruta temporal y lo lanza con `--update` para evitar bloquear el archivo instalado.

### Seguridad de la carpeta de instalación

Una instalación nueva solo puede usar una carpeta inexistente o vacía. Antes de copiar archivos, el instalador rechaza raíces de unidades, carpetas raíz del sistema o del perfil del usuario, raíces de repositorios y vínculos/puntos de reanálisis.

Una carpeta no vacía solo se puede limpiar si es una instalación verificada de Neo Twitch: debe contener `NeoTwitch.exe` y el marcador `neo-twitch-install.json` con el identificador de producto `com.dafovi.neotwitch` y el esquema admitido. Los manifiestos legados creados por versiones anteriores se validan mediante su versión, fecha y ruta exacta para permitir su actualización; al finalizar correctamente se reemplazan por el marcador actual.

Si el usuario escribe `--target` o selecciona una carpeta no vacía que no cumple esa verificación, la instalación se detiene antes de descargar, borrar o copiar archivos y muestra que debe elegir una carpeta nueva/vacía o la instalación existente. El modo `--update` exige siempre una instalación verificada.

## 18. Builds

Instala el SDK indicado por `global.json`. El repositorio fija `10.0.400` y usa `latestPatch`: permite parches de seguridad dentro de la feature band 10.0.4xx, pero evita saltos silenciosos a otra feature band o version principal.

Comandos:

```powershell
.\scripts\build.ps1 -Mode Debug
.\scripts\build.ps1 -Mode Release
.\scripts\build.ps1 -Mode Test
.\scripts\build.ps1 -Mode Verify
.\scripts\build.ps1 -Mode Portable
.\scripts\build.ps1 -Mode SelfContained
.\scripts\build.ps1 -Mode Installer
.\scripts\build.ps1 -Mode FullRelease -Clean
```

Release firmado:

```powershell
.\scripts\release.ps1 -Version 2.2.4 -SigningKeyPath "D:\secure\neo-twitch-release-private.pem" -Clean
```

Artifacts:

```text
artifacts/V{version}/NeoTwitch-V{version}-Windows.zip
artifacts/V{version}/NeoTwitch.exe
artifacts/V{version}/NeoTwitch.Installer.exe
artifacts/V{version}/neo-twitch-release.json
artifacts/V{version}/neo-twitch-release.json.sig
```

### Integridad de releases

Las actualizaciones automáticas confían en un manifiesto autenticado, no únicamente en HTTPS o en los hashes publicados por GitHub. `neo-twitch-release.json` declara el esquema, `com.dafovi.neotwitch`, la versión y, para cada artefacto, su nombre exacto, tamaño y SHA-256. `neo-twitch-release.json.sig` contiene la firma ECDSA P-256/SHA-256 separada, codificada en Base64. Se firman los bytes UTF-8 exactos del manifiesto y las firmas usan el formato IEEE P1363.

La clave pública de producción debe estar en `NeoTwitch.Installer/ReleaseIntegrityPublicKey.pem`; el proyecto la incrusta en el instalador. La clave privada correspondiente debe vivir fuera del repositorio, de sus artifacts, backups y logs. No existe una clave privada ni una clave pública de producción de ejemplo en el repositorio: mientras la clave pública no esté configurada, el instalador rechaza todas las actualizaciones automáticas.

Para la ceremonia inicial, ejecuta PowerShell 7 y sustituye la ruta segura por almacenamiento cifrado y respaldado fuera del checkout:

```powershell
$curve = [System.Security.Cryptography.ECCurve]::CreateFromFriendlyName("nistP256")
$key = [System.Security.Cryptography.ECDsa]::Create($curve)
[System.IO.File]::WriteAllText("D:\secure\neo-twitch-release-private.pem", $key.ExportECPrivateKeyPem())
[System.IO.File]::WriteAllText(".\NeoTwitch.Installer\ReleaseIntegrityPublicKey.pem", $key.ExportSubjectPublicKeyInfoPem())
$key.Dispose()
```

Versiona únicamente `ReleaseIntegrityPublicKey.pem`. Nunca agregues `neo-twitch-release-private.pem`, otra clave privada o su contenido al repositorio. Después, `release.ps1` compila los artifacts, invoca `scripts/sign-release.ps1`, verifica que la clave privada corresponda a la pública incrustada, calcula hashes en streaming y produce el manifiesto y su firma. Publica juntos todos los artifacts de nivel superior, el manifiesto y la firma sin modificar ninguno después de firmar.

El instalador descarga primero el manifiesto y la firma, valida la firma con la clave pública incrustada, comprueba producto/esquema/versión/nombres y solo entonces descarga el payload. Después verifica tamaño y SHA-256 antes de volver a validar el destino y empezar la limpieza. Releases legados sin ambos archivos se rechazan; no existe fallback automático sin firma. `--package` queda como instalación manual explícita para destinos nuevos, pero está prohibido junto con `--update`.

La primera migración desde una versión cuyo instalador todavía no aplica estas reglas requiere que el usuario obtenga por un canal confiable el nuevo instalador que ya contiene la clave pública. El código antiguo no puede protegerse retroactivamente.

`Verify` es el comando canonico antes de subir cambios grandes. Restaura, compila toda la solucion en Release, ejecuta la suite mediante `dotnet test`, valida el resultado TRX y hace un smoke test aislado de la app. Si Neo Twitch ya esta abierto, falla con un diagnostico en vez de omitir silenciosamente el smoke test.

`FullRelease` ejecuta primero exactamente esa verificacion. Solo publica artifacts si build, tests, conteo minimo y smoke test aprobaron; `release.ps1` agrega despues la firma. El smoke test demuestra que el ejecutable Release permanece activo cinco segundos y no crea `crash.log` en un perfil temporal aislado. No sustituye automatizacion completa de UI ni pruebas con Twitch, OBS o Arduino reales.

GitHub Actions ejecuta `Verify` en `windows-latest`, usando el SDK de `global.json`, y conserva el TRX de Release como artifact. Tambien ejecuta `git diff --check`.

## 19. Tests

Proyecto:

```text
NeoTwitch.Tests/
```

El proyecto usa MSTest con versiones resueltas en `NeoTwitch.Tests/packages.lock.json`. Cada uno de los casos heredados se descubre con su nombre de comportamiento; los tests nuevos pueden usar clases y metodos MSTest normales. `Verify` restaura en modo bloqueado para detectar cualquier cambio de dependencias no versionado.

Flujo directo reproducible:

```powershell
dotnet restore .\NeoTwitch.slnx
dotnet build .\NeoTwitch.slnx -c Release --no-restore
dotnet test .\NeoTwitch.slnx -c Release --no-build --no-restore
```

Flujo con proteccion contra cero tests:

```powershell
.\scripts\build.ps1 -Mode Test
.\scripts\test.ps1 -Configuration Release
.\scripts\build.ps1 -Mode Verify
```

`scripts/test.ps1` inspecciona el TRX, exige que todos los tests descubiertos se ejecuten y compara el total con `minimumDiscoveredTests` de `build.config.json`. Por eso una ejecucion con cero tests no puede producir una validacion verde.

## 20. Donde Cambiar Cosas

Para cambiar texto:

```text
NeoTwitch/Shared/Text/Services/SpanishUiTextCatalog.cs
NeoTwitch/Shared/Text/Services/UiTextKeys.cs
```

Para cambiar una pantalla:

```text
NeoTwitch/Features/{Feature}/Views/
NeoTwitch/Features/{Feature}/ViewModels/
```

Para cambiar una accion de alerta:

```text
NeoTwitch/Features/Alerts/
NeoTwitch/Features/Alerts/Host/
NeoTwitch/Features/Alerts/Services/
```

Para cambiar OBS:

```text
NeoTwitch/Features/Obs/
```

Para cambiar Arduino:

```text
NeoTwitch/Features/Arduino/
NeoTwitch/Features/Lights/
```

Para cambiar builds/versiones:

```text
Directory.Build.props
build.config.json
scripts/
```

## 21. Buenas Practicas del Proyecto

- Si una funcionalidad pertenece a un dominio, ubicala en `Features/{Dominio}`.
- Si se comparte entre dominios, muevela a `Shared`.
- Evita meter logica nueva directamente en `MainWindow.xaml.cs`.
- Prefiere servicios pequenos para logica testeable.
- Prefiere ViewModels para estado visible en UI.
- Usa `UiTextKeys` para textos reutilizables.
- Usa `Directory.Build.props` para version.
- Usa `build.config.json` para nombres/rutas de release.
- Antes de release, ejecuta `.\scripts\build.ps1 -Mode FullRelease -Clean`.

## 22. Documentacion Externa Util

- Twitch EventSub WebSockets: https://dev.twitch.tv/docs/eventsub/handling-websocket-events/
- Twitch EventSub subscription types: https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/
- Twitch OAuth Device Code Flow: https://dev.twitch.tv/docs/authentication/getting-tokens-oauth/
- OBS WebSocket: https://github.com/obsproject/obs-websocket
- Arduino NeoPixel wiring: https://whatmakeart.com/arduino/wiring-led-strip-to-arduino/
- Alexa Smart Home Skills: https://www.developer.amazon.com/en-US/docs/alexa/smarthome/create-skill-tutorial.html
- AWS Lambda Function URLs: https://docs.aws.amazon.com/lambda/latest/dg/urls-configuration.html
