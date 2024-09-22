using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Speak3Po.Core.Interfaces;
using Speak3Po.Data;
using System.Reflection;

namespace Speak3Po
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _commands;
        private readonly IServiceProvider _services;

        private ILogger _Logger;

        public InteractionHandler(DiscordSocketClient client, InteractionService commands, IServiceProvider services)
        {
            _client = client;
            _commands = commands;
            _services = services;

            _Logger = services.GetRequiredService<ILogger>();
        }

        public async Task InitialiseAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _client.InteractionCreated += HandleInteraction;
            _client.UserVoiceStateUpdated += HandleVoiceChannelUpdated;
        }

        private async Task HandleVoiceChannelUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
        {
            _Logger?.Log($"[HandleVoiceChannelUpdated]", ELogType.Log);

            var db = _services.GetRequiredService<IDatabase>();
            if (db == null) return;

            //You just joined a voice chat
            if (newState.VoiceChannel != null)
            {
                var voiceChannel = await db.GetAsync<VoiceChannelData>($"TriggerChannel/{newState.VoiceChannel.Id}");
                //The voice chat you joined, is it a trigger channel?
                if (voiceChannel != null)
                {
                    var tempChannel = await newState.VoiceChannel.Guild.CreateVoiceChannelAsync(voiceChannel.TempChannelName,
                        properties =>
                        {
                            properties.CategoryId = newState.VoiceChannel.CategoryId;
                            properties.PermissionOverwrites = newState.VoiceChannel?.Category?.PermissionOverwrites.ToList();
                        });

                    await tempChannel.SyncPermissionsAsync();

                    await db.PutAsync($"TempChannel/{tempChannel.Id}", new VoiceChannelData()
                    {
                        GuildId = tempChannel.GuildId,
                        ChannelId = tempChannel.Id,
                        OwnerClientId = user.Id
                    });

                    await tempChannel.AddPermissionOverwriteAsync(user, 
                        OverwritePermissions.AllowAll(tempChannel)
                            .Modify(manageChannel: PermValue.Allow)
                            .Modify(stream: PermValue.Allow)
                        );
                    await newState.VoiceChannel.Guild.MoveAsync((IGuildUser)user, tempChannel);
                }
            }

            //You just left a voice chat
            if (oldState.VoiceChannel != null)
            {
                var voiceChannel = await db.GetAsync<VoiceChannelData>($"TempChannel/{oldState.VoiceChannel.Id}");
                if (voiceChannel != null)
                {
                    if (oldState.VoiceChannel.ConnectedUsers.Count == 0)
                    {
                        await oldState.VoiceChannel.DeleteAsync();
                        await db.DeleteAsync($"TempChannel/{oldState.VoiceChannel.Id}");
                    }
                }
            }
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            var dialogueContext = new InteractionContext(_client, arg);
            await _commands.ExecuteCommandAsync(dialogueContext, _services);
        }
    }
}
