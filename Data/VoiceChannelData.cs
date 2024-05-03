namespace Speak3Po.Data
{
    public class VoiceChannelData
    {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public string TempChannelName { get; set; }
        public ulong OwnerClientId { get; set; }
    }
}
