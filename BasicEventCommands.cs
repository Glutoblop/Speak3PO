using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Speak3Po.Core.Interfaces;
using Speak3Po.Data;

namespace Speak3Po
{
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class BasicEventCommands : InteractionModuleBase<InteractionContext>
    {
        private readonly IServiceProvider _Services;

        public BasicEventCommands(IServiceProvider services)
        {
            _Services = services;
        }

        [SlashCommand("assign", "The given Voice Channel Id is the generator of temporary voice channels", runMode: RunMode.Async)]
        public async Task SetTriggerTime(IVoiceChannel voiceChannel, string tempChannelName = "Temp Voice")
        {
            await DeferAsync(true);

            var db = _Services.GetRequiredService<IDatabase>();
            //var triggerChannel = await db.GetAsync<VoiceChannelData>($"TriggerChannel/{voiceChannelId}");
            var tempChannel = await db.GetAsync<VoiceChannelData>($"TempChannel/{voiceChannel.Id}");
            if (tempChannel != null)
            {
                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = $"Cannot assign the Master Voice Channel to a Temporary Voice Channel.";
                });
            }

            if (String.IsNullOrEmpty(tempChannelName))
            {
                tempChannelName = "Temp Voice";
            }

            await db.PutAsync($"TriggerChannel/{voiceChannel.Id}", new VoiceChannelData()
            {
                GuildId = Context.Guild.Id,
                ChannelId = voiceChannel.Id,
                TempChannelName = tempChannelName
            });

            await ModifyOriginalResponseAsync(properties =>
            {
                properties.Content = $"{voiceChannel.Mention} assigned as Master Channel.";
            });
        }
    }
}
