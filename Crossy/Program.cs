using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using DotNetEnv;
using Crossy.Models;

// Rename this to whatever you'd like. Also rename in the Commands.cs file and any other files you are using this namespace in.
namespace Crossy {
    class DiscordBot {
        // Basic Discord variables. Client, for the instance of the Discord bot client, and the commands and services that are required to run the bot and inject commands.
        public DiscordSocketClient _client;
        public CommandService _commands;
        public IServiceProvider _services;

        static void Main (string[] args) => new DiscordBot ().RunBotAsync ().GetAwaiter ().GetResult ();

        // This is a singleton.
        // This is used to get a recurring instance of the Discord bot that is the same as long as the Bot is running.
        // This is so you can get the client of the Bot, and other variables saved in here.
        #region Bot Singleton
        private static DiscordBot instance;
        private DiscordBot () { }

        public static DiscordBot Instance {
            get {
                if (instance == null) {
                    instance = new DiscordBot ();
                }

                return instance;
            }
        }
        #endregion

        public async Task RunBotAsync () {

            // Creates the Client, Command Service and Discord Service.
            _client = new DiscordSocketClient ();
            _commands = new CommandService ();
            _services = new ServiceCollection().AddSingleton(_client).AddSingleton(_commands).BuildServiceProvider();

            //Loads the .env file. We use .env so we don't push important information, such as the bot token.
            DotNetEnv.Env.Load();

            // This is your bot token. You get this from http://www.discordapp.com/developers/, if you need help, read the README in the repository.
            string botToken = DotNetEnv.Env.GetString("BOT_TOKEN");

            // Injects the commands into the bot as something to monitor.
            await RegisterCommandsAsync (); 

            // Log the Bot in, as a Bot, using the token retrieved earlier, then start the bot.
            await _client.LoginAsync (Discord.TokenType.Bot, botToken);
            await _client.StartAsync ();

            //Ouput a message to console so we know the bot connected
            Console.WriteLine("Bot connected and ready to go!");

            // This is so the bot never closes unless there is a bug or error.
            await Task.Delay (-1);
        }

        public async Task RegisterCommandsAsync () {
            // This is a Message Received event. The client has a multitude of different events that you can use to sign up and customize how certain things are handled.
            // Here, we register ourselves to handle commands differently to how Discord handles them without our help (which is by doing nothing).
            _client.MessageReceived += HandleCommandAsync;
            _client.JoinedGuild += GuildJoin;

            // We then add the Commands module through the Discord service, and then the bot will listen for certain commands in the Commands.cs file.
            await _commands.AddModulesAsync (Assembly.GetEntryAssembly (), _services);
        }

        private async Task GuildJoin(SocketGuild guild)
        {
            List<Reaction> reactions = new List<Reaction>();
            List<Mute> mutes = new List<Mute>();
            List<UserWarning> warnings = new List<UserWarning>();
            GuildInfo guildInfo = new GuildInfo
            {
                ServerName = guild.Name,
                CreationDate = guild.CreatedAt.ToString(),
                Creator = $"{guild.Owner.Username}#{guild.Owner.Discriminator}",
                CreatorId = guild.OwnerId,
                BannerURL = guild.BannerUrl
            };

            GuildModel newGuild = new GuildModel
            {
                GuildID = guild.Id.ToString(),
                GuildInfo = guildInfo,
                Mutes = mutes,
                Reactions = reactions,
                UserWarnings = warnings
            };

            MongoCRUD.Instance.InitOrg(newGuild);

            await Task.CompletedTask;
        }

        // In this function, we handle a SocketMessage. This is a Discord class for any message the Bot monitors.
        private async Task HandleCommandAsync (SocketMessage arg) {
            // Turn that SocketMessage into a SocketUserMessage, giving us access to the User who wrote the message as well as the message itself.
            var msg = arg as SocketUserMessage;
            // If the message is empty or is from a bot, do nothing.
            if (msg is null || msg.Author.IsBot) return;

            // Start at the beginning of the string.
            int argumentPosition = 0;

            // This checks if the message starts with !, in this case.
            // The ! can be anything you want, but this goes before the command. ie - !ping, !help, !trade.
            if (msg.HasStringPrefix ("!", ref argumentPosition)) {
                // Injects the Context variable for the message.
                var context = new SocketCommandContext (_client, msg);

                // Tries to find and execute a command with the remainder of the message.
                // If there's no command for the remainder of the message, the result will comeback with an Unknown Command message.
                                        var result = await _commands.ExecuteAsync (context, argumentPosition, _services);

                if (!result.IsSuccess) {
                    Console.WriteLine (result.ErrorReason);
                }
            }
        }
    }
}