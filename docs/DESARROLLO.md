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

Release:

```powershell
.\scripts\release.ps1 -Version 2.2.4 -Clean
```

Artifacts:

```text
artifacts/V{version}/NeoTwitch-V{version}-Windows.zip
artifacts/V{version}/NeoTwitch.exe
artifacts/V{version}/NeoTwitch.Installer.exe
```

`Verify` debe ser el comando de confianza antes de subir cambios grandes, porque compila y ejecuta tests.

## 19. Tests

Proyecto:

```text
NeoTwitch.Tests/
```

Comando:

```powershell
dotnet test .\NeoTwitch.Tests\NeoTwitch.Tests.csproj
```

O por script:

```powershell
.\scripts\build.ps1 -Mode Test
```

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
