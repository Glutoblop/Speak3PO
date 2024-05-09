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
            _client.ModalSubmitted += HandleModalSubmitted;
            _client.ReactionAdded += HandleReactionAdded;
            _client.ReactionRemoved += HandleReactionRemoved;
            _client.SelectMenuExecuted += HandleMenuSelection;
            _client.ButtonExecuted += HandleButtonPressed;

            _client.MessageReceived += HandleMessageReceived;
            _client.MessageUpdated += HandleMessageUpdated;
            _client.MessageDeleted += HandleMessageDeleted;

            _client.ChannelDestroyed += HandleChannelDestroyed;
            _client.UserVoiceStateUpdated += HandleVoiceChannelUpdated;
        }

        private async Task HandleMessageReceived(SocketMessage msg)
        {
            _Logger?.Log($"[HandleMessageReceived]", ELogType.Log);
        }

        private async Task HandleVoiceChannelUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
        {
            _Logger?.Log($"[HandleVoiceChannelUpdated]", ELogType.Log);

            var db = _services.GetRequiredService<IDatabase>();
            if (db == null) return;

            if (newState.VoiceChannel != null)
            {
                var voiceChannel = await db.GetAsync<VoiceChannelData>($"TriggerChannel/{newState.VoiceChannel.Id}");
                if (voiceChannel != null)
                {
                    var tempChannel = await newState.VoiceChannel.Guild.CreateVoiceChannelAsync(voiceChannel.TempChannelName,
                        properties =>
                        {
                            properties.CategoryId = newState.VoiceChannel.CategoryId;
                            properties.PermissionOverwrites = newState.VoiceChannel.Category.PermissionOverwrites.ToList();
                        });

                    await tempChannel.SyncPermissionsAsync();

                    await db.PutAsync($"TempChannel/{tempChannel.Id}", new VoiceChannelData()
                    {
                        GuildId = tempChannel.GuildId,
                        ChannelId = tempChannel.Id,
                        OwnerClientId = user.Id
                    });

                    await newState.VoiceChannel.Guild.MoveAsync((IGuildUser)user, tempChannel);

                }
            }

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

        private async Task HandleChannelDestroyed(SocketChannel socketChannel)
        {
            _Logger?.Log($"[HandleChannelDestroyed]", ELogType.Log);
        }

        private async Task HandleMessageUpdated(Cacheable<IMessage, ulong> msgCache, SocketMessage message, ISocketMessageChannel channel)
        {
            _Logger?.Log($"[HandleMessageUpdated] Message {msgCache.Id} updated: '{message.Content}' in channel: {channel.Id}", ELogType.VeryVerbose);
        }

        private async Task HandleMessageDeleted(Cacheable<IMessage, ulong> msgCache, Cacheable<IMessageChannel, ulong> channelCache)
        {
            _Logger?.Log($"[HandleMessageDeleted]", ELogType.Log);
        }

        private async Task HandleButtonPressed(SocketMessageComponent arg)
        {
            _Logger?.Log($"[HandleButtonPressed]", ELogType.Log);
        }

        private async Task HandleMenuSelection(SocketMessageComponent arg)
        {
            _Logger?.Log($"[HandleMenuSelection]", ELogType.Log);
        }

        private async Task HandleReactionAdded(Cacheable<IUserMessage, ulong> msgCache,
            Cacheable<IMessageChannel, ulong> channelCache, SocketReaction reaction)
        {
            _Logger?.Log($"[HandleReactionAdded]", ELogType.Log);
        }

        private async Task HandleReactionRemoved(Cacheable<IUserMessage, ulong> msgCache, Cacheable<IMessageChannel, ulong> channelCache, SocketReaction reaction)
        {
            _Logger?.Log($"[HandleReactionRemoved]", ELogType.Log);
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            _Logger?.Log($"[HandleInteraction]", ELogType.Log);
            var dialogueContext = new InteractionContext(_client, arg);
            await _commands.ExecuteCommandAsync(dialogueContext, _services);
        }

        private async Task HandleModalSubmitted(SocketModal arg)
        {
            _Logger?.Log($"[HandleModalSubmitted]", ELogType.Log);
        }
    }
}
