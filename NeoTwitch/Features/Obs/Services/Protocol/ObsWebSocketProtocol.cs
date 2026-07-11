namespace NeoTwitch.Services.Obs;

public static class ObsWebSocketProtocol
{
    public const int OpHello = 0;
    public const int OpIdentify = 1;
    public const int OpIdentified = 2;
    public const int OpRequest = 6;
    public const int OpRequestResponse = 7;

    public const string Op = "op";
    public const string Data = "d";
    public const string RpcVersion = "rpcVersion";
    public const string Authentication = "authentication";
    public const string Salt = "salt";
    public const string Challenge = "challenge";

    public const string SetCurrentProgramScene = "SetCurrentProgramScene";
    public const string CreateInput = "CreateInput";
    public const string SetInputSettings = "SetInputSettings";
    public const string GetVersion = "GetVersion";
    public const string GetSceneList = "GetSceneList";
    public const string GetStudioModeEnabled = "GetStudioModeEnabled";
    public const string GetVideoSettings = "GetVideoSettings";
    public const string CreateSceneItem = "CreateSceneItem";
    public const string SetSceneItemEnabled = "SetSceneItemEnabled";
    public const string SetSceneItemTransform = "SetSceneItemTransform";
    public const string SetInputVolume = "SetInputVolume";
    public const string GetSceneItemId = "GetSceneItemId";

    public const string RequestType = "requestType";
    public const string RequestId = "requestId";
    public const string RequestData = "requestData";
    public const string RequestStatus = "requestStatus";
    public const string RequestResult = "result";
    public const string RequestCode = "code";
    public const string RequestComment = "comment";
    public const string ResponseData = "responseData";

    public const string SceneName = "sceneName";
    public const string SourceName = "sourceName";
    public const string InputName = "inputName";
    public const string InputKind = "inputKind";
    public const string InputSettings = "inputSettings";
    public const string SceneItemEnabled = "sceneItemEnabled";
    public const string Overlay = "overlay";
    public const string Scenes = "scenes";
    public const string CurrentProgramSceneName = "currentProgramSceneName";
    public const string StudioModeEnabled = "studioModeEnabled";
    public const string ObsVersion = "obsVersion";
    public const string SceneItemId = "sceneItemId";
    public const string SceneItemTransform = "sceneItemTransform";
    public const string PositionX = "positionX";
    public const string PositionY = "positionY";
    public const string BoundsType = "boundsType";
    public const string BoundsScaleInner = "OBS_BOUNDS_SCALE_INNER";
    public const string BoundsStretch = "OBS_BOUNDS_STRETCH";
    public const string BoundsWidth = "boundsWidth";
    public const string BoundsHeight = "boundsHeight";
    public const string InputVolumeMul = "inputVolumeMul";
    public const string BaseWidth = "baseWidth";
    public const string BaseHeight = "baseHeight";
    public const string OutputWidth = "outputWidth";
    public const string OutputHeight = "outputHeight";

    public const string ImageSourceKind = "image_source";
    public const string FfmpegSourceKind = "ffmpeg_source";
    public const string BrowserSourceKind = "browser_source";
    public const string ImageFile = "file";
    public const string IsLocalFile = "is_local_file";
    public const string LocalFile = "local_file";
    public const string BrowserUrl = "url";
    public const string BrowserWidth = "width";
    public const string BrowserHeight = "height";
    public const string BrowserShutdown = "shutdown";
    public const string BrowserRestartWhenActive = "restart_when_active";
    public const string Looping = "looping";
    public const string RestartOnActivate = "restart_on_activate";
    public const string CloseWhenInactive = "close_when_inactive";

    public const string CustomPositionMode = "Custom";
    public const string RandomPositionMode = "Random";
    public const string WebSocketScheme = "ws://";
    public const string SecureWebSocketScheme = "wss://";
}
