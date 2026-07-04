# Guia de desarrollo Neo Twitch V2

Esta guia no reemplaza la guia original. Es una segunda version enfocada en la app despues del rediseño, el refactor por vistas, las bibliotecas de medios, OBS y los controles visuales nuevos.

La escribo pensando en alguien que viene de Unity y conoce C#, pero esta aprendiendo WPF/.NET de escritorio.

## 1. La idea mental correcta

En Unity sueles pensar en:

- Una escena con objetos.
- Prefabs reutilizables.
- Scripts que controlan pedazos de UI.
- ScriptableObjects para configuracion central.
- Eventos o referencias para comunicar sistemas.

En Neo Twitch el equivalente aproximado es:

- `MainWindow.xaml`: la ventana principal, parecida a la escena raiz.
- `Views/*.xaml`: cada pestaña visual, parecida a un prefab/panel de UI.
- `Views/*.xaml.cs`: el code-behind de esa vista, normalmente delgado.
- `MainWindow.*.cs`: la logica de host separada por dominio.
- `Models/*.cs`: datos serializables y enums.
- `Services/*.cs`: integraciones y logica externa.
- `App.xaml`: recursos globales, parecido a una combinacion entre tema, estilos globales y configuracion del proyecto.

La regla principal actual es: las vistas dibujan, `MainWindow` coordina, los servicios hablan con el exterior y los modelos guardan estado.

## 2. Estructura de carpetas

```text
NeoTwitch/
  Assets/
    Icons/
    Service*.png
    Nav*.png
  Models/
  Services/
  ViewModels/
  Views/
  MainWindow.xaml
  MainWindow.*.cs
  App.xaml

NeoTwitch.Installer/
  Assets/
  MainWindow.xaml
  MainWindow.xaml.cs
  InstallerService.cs
  GitHubReleaseClient.cs
```

### Carpetas importantes

`Assets`

Contiene imagenes, logos e iconos. Los iconos de servicio como Twitch, Arduino, Alexa y OBS son imagenes reales y no deberian recrearse en XAML. Si el archivo empieza por `Service`, se trata como logo de servicio y normalmente conserva sus colores.

`Views`

Cada pestaña visual vive aqui:

- `DashboardView`
- `ConnectionsView`
- `AlertsView`
- `LightsView`
- `AudioView`
- `ImageLibraryView`
- `VideoLibraryView`
- `ObsView`
- `SettingsView`
- `ActivityView`

La idea es que `MainWindow.xaml` no contenga miles de lineas de cada pestaña. En vez de eso, carga vistas autocontenidas.

`MainWindow.*.cs`

`MainWindow` sigue siendo el host principal, pero esta partido por responsabilidad:

- `MainWindow.Shell.cs`: navegacion, tema, tray icon, color picker.
- `MainWindow.ConfigBinding.cs`: carga datos de config a UI y decide visibilidad.
- `MainWindow.RuleEditor.cs`: editor de alertas, luces de reglas y fondo.
- `MainWindow.AlertExecution.cs`: ejecucion de alertas.
- `MainWindow.Connections.cs`: Twitch, Arduino, Alexa, OBS.
- `MainWindow.AudioLibrary.cs`: biblioteca de audio.
- `MainWindow.MediaLibraries.cs`: imagenes y videos.
- `MainWindow.Obs.cs`: acciones OBS.
- `MainWindow.Activity.cs`: actividad/log visual.
- `MainWindow.Dashboard.cs`: resumen del panel principal.

No es perfecto MVC/MVVM puro, pero ya no es un solo archivo gigante imposible de navegar.

## 3. Como se dibuja una pestaña

Una vista hereda de `NeoTwitchView`.

```csharp
public partial class AlertsView : NeoTwitchView
{
    public AlertsView()
    {
        InitializeComponent();
    }

    private void SaveRuleButton_Click(object sender, RoutedEventArgs e)
        => Host?.SaveRuleButton_Click(sender, e);
}
```

Esto significa:

- La vista conoce su UI.
- La vista no decide reglas de negocio.
- La vista manda eventos al `Host`, que es `MainWindow`.

En Unity seria parecido a tener un componente de UI que solo llama a un `GameManager` o `Controller`, pero sin meter toda la logica dentro del prefab.

## 4. Como se accede a controles de una vista

Para no escribir `AlertsView.PrimaryColorBox` por todo lado, hay archivos de alias:

```csharp
private WpfTextBox PrimaryColorBox => AlertsView.PrimaryColorBox;
private WpfGrid ColorOptionsGrid => AlertsView.ColorOptionsGrid;
private WpfSlider BrightnessSlider => AlertsView.BrightnessSlider;
```

Estos estan en archivos como:

- `MainWindow.AlertControls.cs`
- `MainWindow.LightControls.cs`
- `MainWindow.AudioControls.cs`
- `MainWindow.ConnectionControls.cs`

Esto funciona como una capa de referencias, parecido a tener campos serializados en Unity, pero centralizados.

## 5. Estilos y tema

El estilo general esta en `App.xaml` y se alimenta desde `MainWindow.Theme.cs`.

Los controles usan recursos dinamicos:

```xml
Background="{DynamicResource ThemeSurfaceBrush}"
BorderBrush="{DynamicResource ThemeBorderBrush}"
Style="{StaticResource MutedText}"
```

`DynamicResource` cambia cuando se cambia el tema en ejecucion. `StaticResource` se resuelve una vez. Para colores del tema usa `DynamicResource`; para estilos base que no cambian mucho usa `StaticResource`.

### Regla practica

Si quieres que algo cambie entre modo claro/oscuro, no quemes colores como `#FFFFFF` salvo que sea un icono o un efecto especifico. Usa recursos:

- `ThemeBackgroundBrush`
- `ThemePanelBrush`
- `ThemeSurfaceBrush`
- `ThemeInputBrush`
- `ThemeBorderBrush`
- `ThemeTextBrush`
- `ThemeMutedBrush`
- `ThemeAccentBrush`

## 6. Iconos

Hay dos tipos:

### Logos reales

Ejemplos:

- `ServiceTwitch.png`
- `ServiceArduino.png`
- `ServiceAlexa.png`
- `ServiceObs.png`

Estos no se pintan desde codigo. Ya vienen con identidad visual.

### Iconos de interfaz

Ejemplos:

- `nav_settings.png`
- `icon_delete.png`
- `lights_fx_rainbow.png`
- `appstate_ok.png`

Estos pueden ser blancos o monocromos y se pueden colorear desde estilos o plantillas si hace falta.

En XAML se usan asi:

```xml
<Image Source="/Assets/Icons/lights_fx_rainbow.png"
       Width="22"
       Height="22" />
```

Si agregas un archivo nuevo a `Assets/Icons`, revisa que el `.csproj` lo incluya como `Resource` o que el patron existente lo recoja.

## 7. Layout autocontenido

El problema tipico que tuvimos fue que las pantallas se cortaban al reducir la ventana. La solucion en WPF no es solo agregar scroll en toda la ventana. Lo ideal es:

- La ventana principal mantiene tamaño y estructura.
- Cada vista decide sus scrolls internos.
- Las columnas importantes tienen `MinWidth`.
- Los bloques de opciones usan `WrapPanel` cuando pueden saltar de fila.

Ejemplo:

```xml
<Grid>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="390" />
    <ColumnDefinition Width="*" />
  </Grid.ColumnDefinitions>

  <ScrollViewer Grid.Column="0"
                VerticalScrollBarVisibility="Auto">
    <!-- lista -->
  </ScrollViewer>

  <ScrollViewer Grid.Column="1"
                VerticalScrollBarVisibility="Auto">
    <!-- editor -->
  </ScrollViewer>
</Grid>
```

Para controles que no deben aplastarse:

```xml
<WrapPanel>
  <Grid MinWidth="220" Width="250" />
  <Grid MinWidth="220" Width="250" />
  <Border MinWidth="260" />
</WrapPanel>
```

Esto es parecido a tener un prefab flexible con `LayoutGroup`, `ContentSizeFitter` y limites minimos en Unity.

## 8. Controles de luces

Las luces usan dos niveles:

- Un slider oculto guarda el valor real.
- Un control visual muestra una version bonita del valor.

Ejemplo del brillo:

```xml
<Slider x:Name="BrightnessSlider"
        Minimum="0"
        Maximum="255"
        Visibility="Collapsed" />

<Path x:Name="BrightnessArc" />
<TextBlock x:Name="BrightnessValueText" />
```

El slider es la fuente numerica. El arco circular se actualiza desde:

```csharp
UpdateCircularProgress(BrightnessArc, brightnessPercent / 100d);
```

### Tiempos

Los tiempos tienen:

- Boton `-`
- TextBox editable
- Unidad `ms`
- Boton `+`
- Slider oculto

El TextBox llama a:

```csharp
LightNumberBox_TextChanged(...)
BackgroundLightNumberBox_TextChanged(...)
```

Los botones llaman a:

```csharp
LightValueButton_Click(...)
BackgroundLightValueButton_Click(...)
```

El valor final siempre debe terminar en el modelo:

```csharp
rule.CycleMs = (int)CycleSlider.Value;
config.BackgroundCycleMs = (int)BackgroundCycleSlider.Value;
```

## 9. Visibilidad condicional

No todo se muestra siempre. Esta logica vive en `MainWindow.ConfigBinding.cs`.

Ejemplo:

```csharp
var usesAnyLightColor = useLights
    && (UsesPrimaryColor(pattern) || UsesSecondaryColor(pattern) || UsesTertiaryColor(pattern));

SetVisible(usesAnyLightColor, ColorOptionsGrid);
SetVisible(useLights && UsesBrightness(pattern), BrightnessGrid);
SetVisible(useLights && UsesCycle(pattern), CycleGrid);
SetVisible(useLights && UsesStep(pattern), StepGrid);
```

Si agregas un patron nuevo, revisa:

- `UsesPrimaryColor`
- `UsesSecondaryColor`
- `UsesTertiaryColor`
- `UsesBrightness`
- `UsesCycle`
- `UsesStep`

Asi evitas mostrar controles que no hacen nada.

## 10. Selector de color

El selector vive en:

```text
Views/ColorPickerDialog.xaml
Views/ColorPickerDialog.xaml.cs
```

La app abre el selector desde:

```csharp
PickColor(TextBox target)
```

El flujo es:

1. Se abre el dialogo con el color actual.
2. El usuario cambia HSV, HEX o RGB.
3. Si aplica, se escribe el HEX en el TextBox de destino.
4. Se guarda en `RecentColors`.
5. Se llama a `SaveConfig()`.

Los colores recientes se guardan en `settings.json` dentro de:

```json
"recentColors": [
  "#985CF6",
  "#22D3EE"
]
```

Se conserva un maximo de 8 para que la UI no se desborde.

## 11. Persistencia

La configuracion principal se guarda con `SettingsStore`.

Ruta de usuario:

```text
%APPDATA%\NeoTwitch\settings.json
```

`SettingsStore.NormalizeConfig` es una migracion defensiva. Sirve para que un archivo viejo no rompa la app.

Regla importante: normalizar no debe inventar reglas si el usuario ya tiene un archivo. Por eso las reglas predeterminadas solo nacen desde `AppConfig.CreateDefault()` cuando no existe config.

## 12. Twitch EventSub

El cliente principal es:

```text
Services/TwitchEventSubClient.cs
```

Cuando hay reglas activas, crea suscripciones EventSub segun el tipo:

- Follow
- Subscription
- Raid
- Cheer
- Chat message
- Channel point redemption

Para suscripciones se agrupan varios eventos en la misma regla de `Subscription`:

- `channel.subscribe`
- `channel.subscription.message`
- `channel.subscription.gift`

Asi Prime, tier 1/2/3, resubs y regalos pueden activar la misma alerta de suscripcion.

## 13. Arduino

Arduino se comunica por puerto serial. La app envia comandos con un protocolo de texto:

```text
FX|pin:leds|PATTERN|brightness|duration|cycle|step|#RRGGBB|#RRGGBB|#RRGGBB
STOP|pin:leds
```

La clase importante es:

```text
Models/LightCommand.cs
```

Si cambias el protocolo, debes cambiar tambien el sketch.

## 14. OBS

OBS se conecta por websocket. La configuracion esta en la pestaña de conexiones y la operacion en:

```text
MainWindow.Obs.cs
Services/Obs*
```

OBS puede:

- Cambiar escenas.
- Mostrar imagenes.
- Mostrar videos.
- Usar bibliotecas o grupos.
- Volver a escena anterior si la alerta lo pide.

Imagenes y videos se organizan parecido a audio.

## 15. Bibliotecas de audio, imagenes y videos

Estas pantallas son hermanas conceptuales:

- Audio: `AudioLibrary`
- Imagenes: `ImageLibrary`
- Videos: `VideoLibrary`

Cada asset tiene:

- Nombre
- Ruta
- Duracion
- Grupo opcional
- Uso en alertas

Cada grupo tiene:

- Nombre
- Lista de assets

La alerta puede elegir:

- Asset individual
- Grupo random

## 16. Installer y actualizador

El instalador esta separado:

```text
NeoTwitch.Installer/
```

Piezas:

- `GitHubReleaseClient`: busca el ultimo release.
- `InstallerService`: descarga, copia, crea accesos directos y manifiesto.
- `MainWindow`: UI del instalador.

Cuando se actualiza desde la app:

1. La app lanza el instalador en modo update.
2. El instalador espera a que Neo Twitch se cierre.
3. Descarga el asset correcto del release.
4. Reemplaza archivos.
5. Muestra notas del release.
6. Permite abrir Neo Twitch al finalizar.

## 17. Como cambiar una pantalla sin romperla

Checklist:

1. Edita primero el XAML de la vista.
2. Si agregas `x:Name`, crea alias en `MainWindow.*Controls.cs`.
3. Si el control aparece/oculta segun estado, actualiza `MainWindow.ConfigBinding.cs`.
4. Si tiene click o change, crea handler en `Views/*.xaml.cs` y delega a `Host`.
5. Implementa la logica en el archivo parcial correcto.
6. Ejecuta build.
7. Abre la app al menos una vez.

Ejemplo de flujo para un boton nuevo:

```xml
<Button x:Name="MyButton"
        Content="Probar"
        Click="MyButton_Click" />
```

```csharp
private void MyButton_Click(object sender, RoutedEventArgs e)
    => Host?.MyButton_Click(sender, e);
```

```csharp
internal void MyButton_Click(object sender, RoutedEventArgs e)
{
    // logica real
}
```

## 18. Comandos utiles

Verificacion recomendada antes de probar manualmente:

```powershell
.\scripts\build.ps1 -Mode Verify
```

Este comando ejecuta las pruebas automatizadas, compila la solucion completa y abre el ejecutable unos segundos en modo seguro. Si durante ese arranque se escribe un `crash.log` nuevo, el comando falla. Esto ayuda a detectar errores de XAML o bindings antes de pasarle una version a alguien.

Si necesitas correrlo en un equipo sin escritorio o con Neo Twitch ya abierto, puedes omitir la prueba visual:

```powershell
.\scripts\build.ps1 -Mode Verify -SkipSmokeTest
```

Build debug manual:

```powershell
dotnet build .\NeoTwitch\NeoTwitch.csproj -c Debug
```

Build instalador:

```powershell
dotnet build .\NeoTwitch.Installer\NeoTwitch.Installer.csproj -c Debug
```

Publicar carpeta liviana:

```powershell
dotnet publish .\NeoTwitch\NeoTwitch.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false
```

Publicar exe autocontenido:

```powershell
dotnet publish .\NeoTwitch\NeoTwitch.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## 19. Regla de oro

Si un cambio es visual, intenta resolverlo en XAML, recursos y estilos.

Si un cambio decide comportamiento, ponlo en `MainWindow.*.cs` o en un servicio.

Si un dato debe sobrevivir al cierre de la app, agregalo a `AppConfig` y normalizalo en `SettingsStore`.

Si algo se comunica con Twitch, OBS, Arduino, Alexa o GitHub, debe vivir en `Services` o en un parcial dedicado, no enterrado dentro de una vista.
