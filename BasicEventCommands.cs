using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Speak3Po.Core.Interfaces;
using Speak3Po.Data;

namespace Speak3Po
{
    [RequireContext(ContextType.Guild)]
    public class BasicEventCommands : InteractionModuleBase<InteractionContext>
    {
        private readonly IServiceProvider _Services;

        public BasicEventCommands(IServiceProvider services)
        {
            _Services = services;
        }
        
        [EnabledInDm(false)]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
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
                return;
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

        [EnabledInDm(false)]
        [SlashCommand("max_people", "If you are the owner of the Temp Voice Channel set the max people allowed.",
            runMode: RunMode.Async)]
        public async Task SetMaxPeople(int maxPeople)
        {
            await DeferAsync(true);

            if (maxPeople < 0)
            {
                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = $"Max People has to be more than 0";
                });
                return;
            }

            var db = _Services.GetRequiredService<IDatabase>();
            SocketSlashCommand command = Context.Interaction as SocketSlashCommand;
            var channel = command.Channel;
            var tempChannel = await db.GetAsync<VoiceChannelData>($"TempChannel/{channel.Id}");
            if (tempChannel == null)
            {
                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = $"This channel is not a Temp Voice Channel.";
                });
                return;
            }

            if (tempChannel.OwnerClientId != Context.Interaction.User.Id)
            {
                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = $"You are not the creator of this Temp Voice Channel.";
                });
                return;
            }

            var voiceChannel = channel as IVoiceChannel;
            await voiceChannel.ModifyAsync(properties =>
            {
                properties.UserLimit = maxPeople;
            });

            await ModifyOriginalResponseAsync(properties =>
            {
                properties.Content = $"Max limit set to {maxPeople}";
            });
        }
    }
}
