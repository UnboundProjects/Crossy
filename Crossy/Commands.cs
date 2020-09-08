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
        [Command("setup")]
        public async Task SetupAsync()
        {
            var guild = Context.Guild;
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

            MongoCRUD.Instance.InitServer(newGuild);
            await ReplyAsync("Setup complete");
        }
        [Command ("warn")]
        public async Task WarnAsync(SocketGuildUser target, [Remainder] string reason)
        {
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");
            if (staffRole != null)
            {
                var recs = MongoCRUD.Instance.LoadServerRec<GuildModel>(Context.Guild.Id.ToString(), "GuildID", "Servers");

                int index = recs.IndexOf(recs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());

                if (recs.Count() != 0)
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
                    try
                    {
                        var rec = recs[index].UserWarnings.FirstOrDefault(i => i.UserId == target.Id.ToString()).Warnings;
                        if (rec.Count != 0 && rec != null)
                        {
                            rec.Add(warning);
                            MongoCRUD.Instance.UpdateWarning("Servers", recs[index].GuildID.ToString(), recs[index]);
                            await ReplyAsync("User has been warned.");
                        }
                    }
                    catch
                    {
                        List<Warning> warnings = new List<Warning>();
                        warnings.Add(warning);
                        recs[index].UserWarnings.Add(
                            new UserWarning
                            {
                                UserId = target.Id.ToString(),
                                Warnings = warnings
                            });
                        MongoCRUD.Instance.UpdateWarning("Servers", recs[index].GuildID.ToString(), recs[index]);
                        await ReplyAsync("User has been warned.");
                    }
                }
                else
                {
                    await ReplyAsync("There has been an error.");
                }
            }
        }
        [Command ("warnings")]
        public async Task WarningsAsync(SocketUser target)
        {
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");
            if (staffRole != null)
            {
                int amount = 0;

                var recs = MongoCRUD.Instance.LoadRecords<GuildModel>("Servers");
                int index = recs.IndexOf(recs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());

                StringBuilder sb = new StringBuilder();
                if (recs.Count() != 0)
                {
                    try
                    {
                        var warnings = recs[index].UserWarnings.FirstOrDefault(i => i.UserId == target.Id.ToString()).Warnings;
                        amount = warnings.Count();
                        for (int i = 0; i < amount; i++)
                        {
                            sb.Append($"**Warning #{i + 1}**: {warnings[i].WarnReason} - {warnings[i].DateTime}\n\n");
                        }
                        EmbedBuilder builder = new EmbedBuilder();
                        builder.WithTitle($"**Warnings for {target.Username}#{target.Discriminator}**").WithColor(Discord.Color.Red)
                            .WithDescription(sb.ToString()).WithThumbnailUrl(target.GetAvatarUrl()).WithFooter($"Total: {amount}");

                        await ReplyAsync("", false, builder.Build());
                    }
                    catch
                    {
                        await ReplyAsync("This user has no warnings.");
                    }
                }
            }
        }
        [Command("warning")]
        public async Task WarningAsync(SocketGuildUser target, int warning)
        {
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");
            if (staffRole != null)
            {
                warning -= 1;
                var serverRecs = MongoCRUD.Instance.LoadRecords<GuildModel>("Servers");
                int index = serverRecs.IndexOf(serverRecs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());
                var recs = serverRecs[index].UserWarnings;
                var warningIndex = recs.IndexOf(recs.Where(p => p.UserId == target.Id.ToString()).FirstOrDefault());
                if (recs.Count != 0)
                {
                    if (recs[warningIndex].Warnings != null)
                    {
                        
                        EmbedBuilder builder = new EmbedBuilder();
                        builder.WithTitle($"**Warning #{warning + 1} for {target.Username}#{target.Discriminator}**").WithColor(Discord.Color.Red)
                            .WithDescription($"Reason: {recs[warningIndex].Warnings[warning].WarnReason}\n\nTime given: {recs[warningIndex].Warnings[warning].DateTime}\n\n" +
                                $"Moderator: {recs[warningIndex].Warnings[warning].Moderator.Username}#{recs[warningIndex].Warnings[warning].Moderator.Discriminator}").WithThumbnailUrl(target.GetAvatarUrl());
                        await ReplyAsync("", false, builder.Build());
                    }
                    else
                    {
                        await ReplyAsync("User's warning doesn't exist.");
                    }
                }
                else
                {
                    await ReplyAsync("This user has no warnings.");
                }
            }
            else
            {
                await ReplyAsync("You do not have permission to use this command.");
            }
        }
        [Command("rm warning")]
        public async Task RmWarningAsync(SocketGuildUser target, int warning)
        {
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");
            if (staffRole != null)
            {
                warning -= 1;
                var serverRecs = MongoCRUD.Instance.LoadRecords<GuildModel>("Servers");
                int index = serverRecs.IndexOf(serverRecs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());
                var recs = serverRecs[index].UserWarnings;
                var warningIndex = recs.IndexOf(recs.Where(p => p.UserId == target.Id.ToString()).FirstOrDefault());

                if (recs.Count != 0)
                {
                    recs[warningIndex].Warnings.Remove(recs[warningIndex].Warnings[warning]);
                        MongoCRUD.Instance.UpdateWarning("Servers", serverRecs[index].GuildID.ToString(), serverRecs[index]);
                    
                    await ReplyAsync("User's warning has been removed.");
                }
                else
                {
                    await ReplyAsync("This user has no warnings.");
                }
            }
            else
            {
                await ReplyAsync("You do not have permission to use this command.");
            }
        }
        [Command("mute")]
        public async Task MuteAsync(SocketGuildUser targetFake, string time, [Remainder] string reason)
        {
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");
            if (staffRole != null)
            {

                if (DiscordBot.Instance.currentServer != null)
                {
                    Moderator moderator = new Moderator
                    {
                        Username = Context.User.Username,
                        Discriminator = Context.User.DiscriminatorValue,
                        id = Context.User.Id
                    };
                    Target target = new Target
                    {
                        Username = targetFake.Username,
                        Discriminator = targetFake.DiscriminatorValue,
                        id = targetFake.Id
                    };
                    Mute muteModel = new Mute
                    {
                        TimeMuted = System.DateTime.Now,
                        Reason = reason,
                        Moderator = moderator,
                        Target = target
                    };
                    #region Adding time shit
                    if (time.Contains("d"))
                    {
                        var realTime = time.Substring(0, time.Length - 1);
                        muteModel.Duration = time;

                        System.TimeSpan duration = new System.TimeSpan(int.Parse(realTime), 0, 0, 0);
                        DateTime endMute = System.DateTime.Now.Add(duration);
                        muteModel.MuteFinished = endMute;
                    }
                    else if (time.Contains("h"))
                    {
                        var realTime = time.Substring(0, time.Length - 1);
                        muteModel.Duration = time;

                        System.TimeSpan duration = new System.TimeSpan(0, int.Parse(realTime), 0, 0);
                        DateTime endMute = System.DateTime.Now.Add(duration);
                        muteModel.MuteFinished = endMute;
                    }
                    else if (time.Contains("m"))
                    {
                        var realTime = time.Substring(0, time.Length - 1);
                        muteModel.Duration = time;

                        System.TimeSpan duration = new System.TimeSpan(0, 0, int.Parse(realTime), 0);
                        DateTime endMute = System.DateTime.Now.Add(duration);
                        muteModel.MuteFinished = endMute;
                    }
                    #endregion
                    MongoCRUD.Instance.InsertRecord("Mutes", muteModel);

                    var muteRole = Context.Guild.Roles.FirstOrDefault(r => r.Name == "Muted");

                    await targetFake.AddRoleAsync(muteRole);

                    EmbedBuilder builder = new EmbedBuilder();
                    builder.WithTitle($"**You have been muted in the OEA**").WithDescription($"Duration: {time}\nReason: {reason}").WithColor(Discord.Color.Red)
                        .WithFooter("If you think this was an error, please contact a moderator.");
                    await targetFake.SendMessageAsync("", false, builder.Build());

                    await ReplyAsync($"<@{targetFake.Id}> was muted by <@{Context.User.Id}>.\n" +
                        $"Duration: {time}\n" +
                        $"Reason: {reason}");
                }
                else
                {
                    await ReplyAsync("Please run the setup command before using this command.");
                }
            }
            else
            {
                await ReplyAsync("You do not have permission to use this command.");
            }
        }
    }
}