namespace NeoTwitch.Services.Text;

public static class SpanishUiTextCatalog
{
    public static IReadOnlyDictionary<string, string> Create()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [UiTextKeys.AudioTitle] = "Audio",
            [UiTextKeys.ImagesTitle] = "Imagenes",
            [UiTextKeys.VideosTitle] = "Videos",

            [UiTextKeys.LibraryNoGroup] = "Sin grupo",
            [UiTextKeys.LibraryNoGroupAssigned] = "Sin grupo asignado",
            [UiTextKeys.LibraryNoAlertAssigned] = "Sin alerta asignada",
            [UiTextKeys.LibrarySelectedGroup] = "seleccionado",

            [UiTextKeys.LibraryWriteGroupName] = "Escribe un nombre para el grupo.",
            [UiTextKeys.LibraryDeleteAssetPrompt] = "Eliminar '{0}' de la biblioteca?",
            [UiTextKeys.LibraryDeleteGroupPrompt] = "Eliminar el grupo '{0}'?\n\nLos {1} archivo(s) no se borran; solo quedaran sin grupo.",
            [UiTextKeys.LibrarySavedLog] = "{0}: guardado {1}.",
            [UiTextKeys.LibraryGroupCreatedLog] = "{0}: grupo creado {1}.",
            [UiTextKeys.LibraryShowingGroupLog] = "{0}: mostrando grupo {1}.",
            [UiTextKeys.LibraryGroupDeletedLog] = "{0}: grupo eliminado {1}.",
            [UiTextKeys.LibraryLastUnused] = "Sin uso",
            [UiTextKeys.LibraryFileCount] = "{0} archivo{1}",

            [UiTextKeys.AudioPickValidFile] = "Selecciona un archivo de audio valido.",
            [UiTextKeys.AudioPlayingLog] = "Audio: reproduciendo {0}.",

            [UiTextKeys.MediaPickValidFile] = "Selecciona un archivo valido para {0}.",
            [UiTextKeys.MediaObsConnectRequiredLog] = "OBS: conecta OBS antes de probar imagenes o videos.",
            [UiTextKeys.MediaObsConnectRequiredPrompt] = "Conecta OBS desde Conexiones antes de probar imagenes o videos.",
            [UiTextKeys.MediaObsMissingFileLog] = "OBS: el archivo seleccionado no existe.",
            [UiTextKeys.MediaObsMissingSceneLog] = "OBS: no hay una escena actual para probar el medio.",
            [UiTextKeys.MediaObsPreviewLog] = "OBS: probando {0} '{1}'."
        };
    }
}
