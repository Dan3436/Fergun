using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Fergun
{
    class Program
    {
		public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

		private DiscordSocketConfig _config;
        private DiscordSocketClient _client;
		private CommandService _commands;
		private IServiceProvider _services;

		public static ulong BotOwnerID = 385963075892936706;

		public async Task MainAsync()
        {
			// When working with events that have Cacheable<IMessage, ulong> parameters,
			// you must enable the message cache in your config settings if you plan to
			// use the cached message entity. 
			_config = new DiscordSocketConfig { MessageCacheSize = 100 };
			_client = new DiscordSocketClient(_config);
			_commands = new CommandService();
			_services = new ServiceCollection()
				.AddSingleton(_client)
				.AddSingleton(_commands)
				.BuildServiceProvider();
				
			// Hardcoded token 🤔
            string token = "Token";

			_client.Log += Log;

			CommandHandler CHandler = new CommandHandler(_client, _commands);
			await CHandler.InstallCommandsAsync();

			await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
			await _client.SetGameAsync("-", null, ActivityType.Streaming);
			//await _client.SetActivityAsync();
			await _client.SetStatusAsync(UserStatus.Online);

			_client.MessageUpdated += MessageUpdated;
			_client.Ready += () =>
			{
				Console.WriteLine("Bot is connected!");
				return Task.CompletedTask;
			};

			// Block this task until the program is closed.
			await Task.Delay(-1);
        }

		private async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
		{
			// If the message was not in the cache, downloading it will result in getting a copy of `after`.
			var message = await before.GetOrDownloadAsync();
			Console.WriteLine($"{message} -> {after}");
			
		}

		private Task Log(LogMessage msg)
		{
			Console.WriteLine(msg.ToString());
			return Task.CompletedTask;
		}

        public class CommandHandler
        {
            private readonly DiscordSocketClient _client;
            private readonly CommandService _commands;

            // Retrieve client and CommandService instance via ctor
            public CommandHandler(DiscordSocketClient client, CommandService commands)
            {
                _commands = commands;
                _client = client;
            }

            public async Task InstallCommandsAsync()
            {
                // Hook the MessageReceived event into our command handler
                _client.MessageReceived += HandleCommandAsync;

                // Here we discover all of the command modules in the entry 
                // assembly and load them. Starting from Discord.NET 2.0, a
                // service provider is required to be passed into the
                // module registration method to inject the 
                // required dependencies.
                //
                // If you do not use Dependency Injection, pass null.
                // See Dependency Injection guide for more information.
                await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);
            }

            private async Task HandleCommandAsync(SocketMessage messageParam)
            {
                // Don't process the command if it was a system message
                var message = messageParam as SocketUserMessage;
                if (message == null) return;

                // Create a number to track where the prefix ends and the command begins
                int argPos = 0;

                // Determine if the message is a command based on the prefix and make sure no bots trigger commands
                if (!(message.HasCharPrefix('-', ref argPos) ||
                    message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                    message.Author.IsBot)
                    return;

                // Create a WebSocket-based command context based on the message
                var context = new SocketCommandContext(_client, message);

                // Execute the command with the command context we just
                // created, along with the service provider for precondition checks.
                await _commands.ExecuteAsync(
                    context: context,
                    argPos: argPos,
                    services: null);
            }
        }

		/*
		public class LoggingService
		{
			public LoggingService(DiscordSocketClient client, CommandService command)
			{
				client.Log += LogAsync;
				command.Log += LogAsync;
			}
			private Task LogAsync(LogMessage message)
			{
				if (message.Exception is CommandException cmdException)
				{
					Console.WriteLine($"[Command/{message.Severity}] {cmdException.Command.Aliases.First()}"
						+ $" failed to execute in {cmdException.Context.Channel}.");
					Console.WriteLine(cmdException);
				}
				else
					Console.WriteLine($"[General/{message.Severity}] {message}");

				return Task.CompletedTask;
			}
		}
		*/
	}
}
