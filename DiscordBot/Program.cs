using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordBot {
    class Program {
        // DiscordClient with WebSocket support.
        DiscordSocketClient _client;
        CommandService _commands;


        public static void Main(string[] args) => new Program().Start().GetAwaiter().GetResult();

        private async Task Start() {
            _client = new DiscordSocketClient(new DiscordSocketConfig() {
                LogLevel = LogSeverity.Info
            });

            _commands = new CommandService();

            _client.Log += (message) => {
                Console.WriteLine(message.ToString(prependTimestamp: true, timestampKind: DateTimeKind.Utc));
                System.IO.File.AppendAllLines("bot.log", new System.Collections.Generic.List<string>() { message.ToString(prependTimestamp: true, timestampKind: DateTimeKind.Utc) });
                return Task.CompletedTask;
            };

            await InitializeCommandsAsync();
            await Authorization.AuthLoginAsync(_client);
            await _client.ConnectAsync();

            // Block until exited.
            await Task.Delay(-1);
        }

        private async Task InitializeCommandsAsync() {
            _client.MessageReceived += HandleCommand;

            await _commands.AddModules(Assembly.GetEntryAssembly());
        }

        private async Task HandleCommand(SocketMessage message) {
            SocketUserMessage userMessage;

            try {
                 userMessage = (SocketUserMessage)message;
            } catch (InvalidCastException) {
                return;
            }

            int pos = 0;

            if (userMessage.HasCharPrefix('!', ref pos) || userMessage.HasMentionPrefix(_client.CurrentUser, ref pos)) {
                CommandContext context = new CommandContext(_client, userMessage);
                IResult result = await _commands.Execute(context, pos);

                if (!result.IsSuccess) {
                    await userMessage.Channel.SendMessageAsync(result.ErrorReason);
                }
            }
        }
    }
}
