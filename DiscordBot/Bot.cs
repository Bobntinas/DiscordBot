using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DiscordBot
{
    public class Bot : IBot
    {
        private ServiceProvider? _serviceProvider;

        private readonly ILogger<Bot> _logger;
        private readonly IConfiguration _configuration;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;

        public Bot(
            ILogger<Bot> logger,
            IConfiguration configuration)    
        {
            _logger = logger;
            _configuration = configuration;

            DiscordSocketConfig config = new()
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };

            _client = new DiscordSocketClient(config);
            _commands = new CommandService();
        }

        public async Task StartAsync(ServiceProvider services)
        {
            string discordToken = _configuration["DiscordToken"] ?? throw new Exception("Missing Discord Token");

            _serviceProvider = services;

            await _commands.AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider);

            await _client.LoginAsync(TokenType.Bot, discordToken);
            await _client.StartAsync();

            _client.MessageReceived += HandleCommandAsync;
        }

        public async Task StopAsync()
        {
            _logger.LogInformation("Shutting down");

            if(_client != null)
            {
                await _client.LogoutAsync();
                await _client.StopAsync();
            }
        }
        
        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            //Don't process the command if it was a system message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            //Log the received message
            _logger.LogInformation($"{DateTime.Now.ToShortTimeString()} - {message.Author}: {message.Content}");

            int position = 0;

            //Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix('!', ref position) ||
                message.HasMentionPrefix(_client.CurrentUser, ref position)) ||
                message.Author.IsBot)
                return;

            //Create a WebSocket-based command context based on the message  
            var context = new SocketCommandContext(_client, message);

            await _commands.ExecuteAsync(
                context: context,
                position,
                services: null);
            
        }
    }
}