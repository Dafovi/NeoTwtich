namespace NeoTwitch.Services;

public static class TwitchEventSubProtocol
{
    public const string WebSocketUrl = "wss://eventsub.wss.twitch.tv/ws?keepalive_timeout_seconds=30";
    public const string SubscriptionsApiUrl = "https://api.twitch.tv/helix/eventsub/subscriptions";
    public const string ChatMessagesApiUrl = "https://api.twitch.tv/helix/chat/messages";
    public const string ContentTypeJson = "application/json";
    public const string TransportMethodWebSocket = "websocket";

    public static class Json
    {
        public const string Metadata = "metadata";
        public const string MessageType = "message_type";
        public const string Payload = "payload";
        public const string Session = "session";
        public const string ReconnectUrl = "reconnect_url";
        public const string Id = "id";
        public const string Subscription = "subscription";
        public const string Type = "type";
        public const string Event = "event";
        public const string Message = "message";
        public const string Text = "text";
        public const string Reward = "reward";
        public const string Title = "title";
    }

    public static class MessageTypes
    {
        public const string Welcome = "session_welcome";
        public const string KeepAlive = "session_keepalive";
        public const string Reconnect = "session_reconnect";
        public const string Notification = "notification";
        public const string Revocation = "revocation";
    }

    public static class Events
    {
        public const string Follow = "channel.follow";
        public const string Subscribe = "channel.subscribe";
        public const string SubscriptionMessage = "channel.subscription.message";
        public const string SubscriptionGift = "channel.subscription.gift";
        public const string Raid = "channel.raid";
        public const string Cheer = "channel.cheer";
        public const string ChatMessage = "channel.chat.message";
        public const string ChannelPointRedemption = "channel.channel_points_custom_reward_redemption.add";
    }

    public static class Versions
    {
        public const string V1 = "1";
        public const string V2 = "2";
    }

    public static class Conditions
    {
        public const string BroadcasterUserId = "broadcaster_user_id";
        public const string ModeratorUserId = "moderator_user_id";
        public const string ToBroadcasterUserId = "to_broadcaster_user_id";
        public const string UserId = "user_id";
    }

    public static class EventFields
    {
        public const string UserName = "user_name";
        public const string FromBroadcasterUserName = "from_broadcaster_user_name";
        public const string ChatterUserName = "chatter_user_name";
        public const string Viewers = "viewers";
        public const string Total = "total";
        public const string Bits = "bits";
    }
}
