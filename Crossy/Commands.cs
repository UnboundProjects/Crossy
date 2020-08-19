using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crossy.Models;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Crossy {
    // This is the Commands class. We need to inherit the Context class through our Modules so we can use Context.
    public class Commands : ModuleBase<SocketCommandContext> {
        // Registering a new command. Ping can be whatever, but this is what will follow the prefix we assign in the Program file. In this case, it's an !, so !ping will cause this to run.
        [Command ("Ping")]
        // Every command needs to be followed with a public async Task or Task<T> function.
        public async Task PingAsync () {
            // Creates an EmbedBuilder, something we can use to create an Embed.            
            EmbedBuilder builder = new EmbedBuilder ();

            // Gives the Embed a title, description, and side color.
            builder.WithTitle ("Ping!").WithDescription ("This is a really nice ping.. apparently.").WithColor (Discord.Color.Red);

            // Replies in the channel the command was used, with an empty string, non-text to speech, and using the Embed we made earlier.
            await ReplyAsync ("", false, builder.Build ());
        }

        [Command ("warn")]
        public async Task WarnAsync(SocketGuildUser target, [Remainder] string reason)
        {
            var recs = MongoCRUD.Instance.LoadServerRec<GuildModel>(Context.Guild.Id.ToString(), "GuildID", "Servers");

            if (recs.Count != 0)
            {
                Moderator moderator = new Moderator
                {
                    Username = Context.User.Username,
                    Discriminator = Context.User.DiscriminatorValue,
                    id = Context.User.Id
                };
                Warning warning = new Warning
                {
                    WarnReason = reason,
                    DateTime = DateTime.Now.ToString(),
                    Moderator = moderator
                };
                foreach (var rec in recs)
                {
                    if (rec.Warnings.Count != 0)
                    {
                        await ReplyAsync("I need to do something here");
                    }
                    else
                    {


                        List<Warning> warnings = new List<Warning>();
                        warnings.Add(warning);
                        rec.Warnings.Add(
                            new UserWarning
                            {
                                UserId = target.Id.ToString(),
                                Warnings = warnings
                            });
                        MongoCRUD.Instance.UpdateWarning("Servers", rec.GuildID.ToString(), rec);
                        await ReplyAsync("User has been warned.");
                    }
                }
            }
            else
            {

            }   
        }
    }
}