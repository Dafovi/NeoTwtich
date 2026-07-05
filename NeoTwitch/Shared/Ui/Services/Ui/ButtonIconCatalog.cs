namespace NeoTwitch.Services.Ui;

public static class ButtonIconCatalog
{
    private static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Abrir Twitch Console"] = "ExternalLink",
        ["Detectar"] = "Search",
        ["Conectar"] = "Plug",
        ["Probar Alexa"] = "Play",
        ["Probar OBS"] = "Play",
        ["Conectar OBS"] = "Plug",
        ["Desconectar OBS"] = "Plug",
        ["Actualizar escenas"] = "Refresh",
        ["Cambiar ahora"] = "Play",
        ["Ver guia OBS"] = "Book",
        ["Abrir Alexa Console"] = "ExternalLink",
        ["Guardar configuracion"] = "Save",
        ["Ir a actividad"] = "Activity",
        ["Nueva"] = "Plus",
        ["Nueva alerta"] = "Plus",
        ["Duplicar"] = "Copy",
        ["Eliminar"] = "Trash",
        ["Probar regla"] = "Play",
        ["Probar alerta"] = "Play",
        ["Parar prueba"] = "Square",
        ["Guardar cambios"] = "Save",
        ["Eliminar alerta"] = "Trash",
        ["Agregar audio"] = "Plus",
        ["Guardar audio"] = "Save",
        ["Nuevo grupo"] = "Plus",
        ["Buscar"] = "Search",
        ["Arduino Tira led ws2812b"] = "Arduino",
        ["Alexa"] = "Alexa",
        ["Aplicar fondo LED"] = "Sun",
        ["Apagar tiras"] = "Power",
        ["Borrar salida"] = "Trash",
        ["Agregar salida de pin digital"] = "Plus",
        ["Descargar ultimo sketch"] = "Download",
        ["Ver guia"] = "Book",
        ["Aplicar fondo Alexa"] = "Alexa",
        ["Apagar fondo Alexa"] = "Power",
        ["Exportar configuracion"] = "Upload",
        ["Importar configuracion"] = "Download",
        ["Crear backup ahora"] = "Save",
        ["Restaurar backup"] = "Download",
        ["Ejecutar diagnostico"] = "MonitorCheck",
        ["Limpiar actividad"] = "Trash",
        ["Limpiar filtros"] = "Search",
        ["Limpiar"] = "Trash"
    };

    public static bool TryGetIconKey(string label, out string iconKey)
    {
        return Labels.TryGetValue(label.Trim(), out iconKey!);
    }
}
