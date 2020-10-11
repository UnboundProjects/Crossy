using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crossy.Models;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Serialization;

namespace Crossy 
{
    // This is the Commands class. We need to inherit the Context class through our Modules so we can use Context.
    public class Commands : ModuleBase<SocketCommandContext>
    {
        // Registering a new command. Ping can be whatever, but this is what will follow the prefix we assign in the Program file. In this case, it's an !, so !ping will cause this to run.
        [Command("Ping")]
        // Every command needs to be followed with a public async Task or Task<T> function.
        public async Task PingAsync()
        {
            // Creates an EmbedBuilder, something we can use to create an Embed.            
            EmbedBuilder builder = new EmbedBuilder();

            // Gives the Embed a title, description, and side color.
            builder.WithTitle("Ping!").WithDescription("This is a really nice ping.. apparently.").WithColor(Discord.Color.Red);

            // Replies in the channel the command was used, with an empty string, non-text to speech, and using the Embed we made earlier.
            await ReplyAsync("", false, builder.Build());
        }
        [Command("setup")]
        public async Task SetupAsync()
        {
            //Creating the variables we need for the setup command
            var guild = Context.Guild;
            List<Reaction> reactions = new List<Reaction>();
            List<Mute> mutes = new List<Mute>();
            List<UserWarning> warnings = new List<UserWarning>();
            CustomAnnouncement customAnnouncement = new CustomAnnouncement();
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
                UserWarnings = warnings,
                CustomAnnouncement = customAnnouncement
            };
            //Run the init server method which inputs the server into the database
            MongoCRUD.Instance.InitServer(newGuild);
            //Reply with setup is complete if it doesnt break
            await ReplyAsync("Setup complete");
        }
        #region Moderation
        #region Warn related
        [Command("warn")]
        public async Task WarnAsync(SocketGuildUser target, [Remainder] string reason)
        {
            //Creating the variables we need for the warn command
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");
            //Checks to see if the person who used the command has the staff role
            if (staffRole != null)
            {
                //Gets the server record from the database
                var recs = MongoCRUD.Instance.LoadServerRec<GuildModel>(Context.Guild.Id.ToString(), "GuildID", "Servers");

                //Gets the index of the recs array where the guild id is equal to the guild the command was used in
                int index = recs.IndexOf(recs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());

                //If recs isn't empty. This if statement is redundant and can be removed
                if (recs.Count() != 0)
                {
                    //Creating the moderator var
                    Moderator moderator = new Moderator
                    {
                        Username = Context.User.Username,
                        Discriminator = Context.User.DiscriminatorValue,
                        id = Context.User.Id
                    };
                    //Creating the warning var
                    Warning warning = new Warning
                    {
                        WarnReason = reason,
                        DateTime = DateTime.Now.ToString(),
                        Moderator = moderator
                    };
                    //The try catch is just a test to see if it exists yet or not
                    try
                    {
                        //Set rec to to the record where the user id matches the target id. This is where the catch will happen if the
                        //Rec doesn't exist
                        var rec = recs[index].UserWarnings.FirstOrDefault(i => i.UserId == target.Id.ToString()).Warnings;
                        //Just making sure stuff doesn't break. Could probably be removed
                        if (rec.Count != 0 && rec != null)
                        {
                            //Add warning to the array
                            rec.Add(warning);
                            //Update the record
                            MongoCRUD.Instance.UpdateRecord("Servers", recs[index].GuildID.ToString(), recs[index]);
                            //Let them know user has been warned if everything goes well
                            await ReplyAsync("User has been warned.");
                        }
                    }
                    //If something breaks
                    catch
                    {
                        //Creating the list of warnings we need
                        List<Warning> warnings = new List<Warning>();
                        //Adding the warning to the list we just made
                        warnings.Add(warning);
                        //Adding that warning array to the user warning array
                        recs[index].UserWarnings.Add(
                            new UserWarning
                            {
                                UserId = target.Id.ToString(),
                                Warnings = warnings
                            });
                        //Update the record
                        MongoCRUD.Instance.UpdateRecord("Servers", recs[index].GuildID.ToString(), recs[index]);
                        //Let them know the user has been warned if everything goes well
                        await ReplyAsync("User has been warned.");
                    }
                }
                else
                {
                    await ReplyAsync("There has been an error.");
                }
            }
        }
        [Command("warnings")]
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
                    MongoCRUD.Instance.UpdateRecord("Servers", serverRecs[index].GuildID.ToString(), serverRecs[index]);

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
        #endregion
        #region Mute Related
        [Command("mute")]
        public async Task MuteAsync(SocketGuildUser targetFake, string time, [Remainder] string reason)
        {
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");
            if (staffRole != null)
            {
                var serverRecs = MongoCRUD.Instance.LoadRecords<GuildModel>("Servers");
                int index = serverRecs.IndexOf(serverRecs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());
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
                #region Adding time stuff
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
                serverRecs[index].Mutes.Add(muteModel);
                MongoCRUD.Instance.UpdateRecord("Servers", serverRecs[index].GuildID.ToString(), serverRecs[index]);

                var muteRole = Context.Guild.Roles.FirstOrDefault(r => r.Name == "Muted");

                await targetFake.AddRoleAsync(muteRole);

                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle($"**You have been muted in {Context.Guild.Name}**").WithDescription($"Duration: {time}\nReason: {reason}").WithColor(Discord.Color.Red)
                    .WithFooter("If you think this was an error, please contact a server moderator.");
                await targetFake.SendMessageAsync("", false, builder.Build());

                await ReplyAsync($"<@{targetFake.Id}> was muted by <@{Context.User.Id}>.\n" +
                    $"Duration: {time}\n" +
                    $"Reason: {reason}");
            }
            else
            {
                await ReplyAsync("You do not have permission to use this command.");
            }
        }
        [Command("unmute")]
        public async Task UnMuteAsync(SocketGuildUser targetFake)
        {
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");
            if (staffRole != null)
            {
                var serverRecs = MongoCRUD.Instance.LoadRecords<GuildModel>("Servers");
                int index = serverRecs.IndexOf(serverRecs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());

                int muteIndex = serverRecs[index].Mutes.IndexOf(serverRecs[index].Mutes.Where(x => x.Target.id == targetFake.Id).FirstOrDefault());

                serverRecs[index].Mutes.Remove(serverRecs[index].Mutes[muteIndex]);

                MongoCRUD.Instance.UpdateRecord("Servers", serverRecs[index].GuildID.ToString(), serverRecs[index]);

                var muteRole = Context.Guild.Roles.FirstOrDefault(r => r.Name == "Muted");

                await targetFake.RemoveRoleAsync(muteRole);

                await ReplyAsync($"<@{targetFake.Id}> has been unmuted.");

            }
            else
            {
                await ReplyAsync("You do not have permission to use this command.");
            }
        }
        [Command("mutes")]
        public async Task MutesAsync()
        {
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");
            if (staffRole != null)
            {
                var serverRecs = MongoCRUD.Instance.LoadRecords<GuildModel>("Servers");
                int index = serverRecs.IndexOf(serverRecs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());

                StringBuilder sb = new StringBuilder();
                foreach (var mute in serverRecs[index].Mutes)
                {
                    TimeSpan timeLeft = mute.MuteFinished.Subtract(DateTime.UtcNow);
                    string timeLeftTrimmed = string.Format($"{timeLeft.Days}:{timeLeft.Hours}:{timeLeft.Minutes}:{timeLeft.Seconds}");
                    sb.Append($"<@{mute.Target.id}> - {mute.Reason} - {timeLeftTrimmed}\n");
                }

                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle("Active Mutes:").WithDescription(sb.ToString()).WithColor(Discord.Color.DarkerGrey);

                await ReplyAsync("", false, builder.Build());
            }
            else
            {
                await ReplyAsync("You do not have permission to use this command.");
            }
        }
        #endregion

        [Command("softban")]
        public async Task SoftbanAsync(SocketGuildUser user, [Remainder] string reason)
        {
            var user1 = Context.User as SocketGuildUser;
            var staffRole = user1.Roles.FirstOrDefault(x => x.Name == "Staff");
            if (staffRole != null)
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle($"**{user.Username}#{user.Discriminator} has been soft banned.**").WithColor(Discord.Color.Red);

                EmbedBuilder builder1 = new EmbedBuilder();
                builder1.WithTitle($"**You have been kicked from the {Context.Guild.Name} server**").WithDescription($"Reason: {reason}").WithColor(Discord.Color.Red)
                    .WithFooter("If you think this was an error, please contact a moderator.");

                await user.SendMessageAsync("", false, builder1.Build());
                await user.BanAsync();
                await Context.Guild.RemoveBanAsync(user);
                await ReplyAsync("", false, builder.Build());
            }
            else
            {
                await ReplyAsync("You do not have permission to use this command.");
            }
        }
        [Command("clear")]
        public async Task ClearAsync(int amount)
        {
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");
            if (staffRole != null)
            {
                var messages = await Context.Channel.GetMessagesAsync(amount + 1).FlattenAsync();
                await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messages);
                var msg = await ReplyAsync($"{amount} messages cleared.");

                await Task.Delay(2000);

                await msg.DeleteAsync();
            }
            else
            {
                await ReplyAsync("You do not have permission to use this command.");
            }
        }
        #endregion
        #region Custom announcements
        [Command("custom body")]
        public async Task CustomBodyAsync([Remainder] string body)
        {
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");
            //Checks to see if the person who used the command has the staff role
            if (staffRole != null)
            {
                //Gets the server record from the database
                var recs = MongoCRUD.Instance.LoadServerRec<GuildModel>(Context.Guild.Id.ToString(), "GuildID", "Servers");

                //Gets the index of the recs array where the guild id is equal to the guild the command was used in
                int index = recs.IndexOf(recs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());

                //Set the body of the announcement in the recs variable
                recs[index].CustomAnnouncement.Body = body;

                //Update the record
                MongoCRUD.Instance.UpdateRecord("Servers", recs[index].GuildID.ToString(), recs[index]);

                await ReplyAsync("Custom body has been set.");
            }
        }

        [Command("custom title")]
        public async Task CustomTitleAsync([Remainder] string title)
        {
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");
            //Checks to see if the person who used the command has the staff role
            if (staffRole != null)
            {
                //Gets the server record from the database
                var recs = MongoCRUD.Instance.LoadServerRec<GuildModel>(Context.Guild.Id.ToString(), "GuildID", "Servers");

                //Gets the index of the recs array where the guild id is equal to the guild the command was used in
                int index = recs.IndexOf(recs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());

                //Set the title of the announcement in the recs variable
                recs[index].CustomAnnouncement.Title = title;

                //Update the record
                MongoCRUD.Instance.UpdateRecord("Servers", recs[index].GuildID.ToString(), recs[index]);

                await ReplyAsync("Custom title has been set.");
            }
            
        }
        [Command("custom footer")]
        public async Task CustomFooterAsync([Remainder] string footer)
        {
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");
            //Checks to see if the person who used the command has the staff role
            if (staffRole != null)
            {
                //Gets the server record from the database
                var recs = MongoCRUD.Instance.LoadServerRec<GuildModel>(Context.Guild.Id.ToString(), "GuildID", "Servers");

                //Gets the index of the recs array where the guild id is equal to the guild the command was used in
                int index = recs.IndexOf(recs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());

                //Set the footer of the announcement in the recs variable
                recs[index].CustomAnnouncement.Footer = footer;

                //Update the record
                MongoCRUD.Instance.UpdateRecord("Servers", recs[index].GuildID.ToString(), recs[index]);

                await ReplyAsync("Custom footer has been set.");
            }

        }

        [Command("custom thumbnail")]
        public async Task CustomThumbnailAsync([Remainder] string thumbnail)
        {
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");
            //Checks to see if the person who used the command has the staff role
            if (staffRole != null)
            {
                //Gets the server record from the database
                var recs = MongoCRUD.Instance.LoadServerRec<GuildModel>(Context.Guild.Id.ToString(), "GuildID", "Servers");

                //Gets the index of the recs array where the guild id is equal to the guild the command was used in
                int index = recs.IndexOf(recs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());

                //Set the thumbnail of the announcement in the recs variable
                recs[index].CustomAnnouncement.Thumbnail = thumbnail;

                //Update the record
                MongoCRUD.Instance.UpdateRecord("Servers", recs[index].GuildID.ToString(), recs[index]);

                await ReplyAsync("Custom thumbnail has been set.");
            }

        }

        [Command("custom post")]
        public async Task CustomPostAsync()
        {
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");
            //Checks to see if the person who used the command has the staff role
            if (staffRole != null)
            {
                //Gets the server record from the database
                var recs = MongoCRUD.Instance.LoadServerRec<GuildModel>(Context.Guild.Id.ToString(), "GuildID", "Servers");

                //Gets the index of the recs array where the guild id is equal to the guild the command was used in
                int index = recs.IndexOf(recs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());

                var rec = recs[index].CustomAnnouncement;

                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle(rec.Title).WithColor(Discord.Color.Red)
                    .WithDescription(rec.Body).WithFooter(rec.Footer);

                try
                {
                    builder.WithThumbnailUrl(rec.Thumbnail);
                    await ReplyAsync("", false, builder.Build());
                }
                catch
                {
                    await ReplyAsync("", false, builder.Build());
                }
                
            }

        }

        //Works but need to figure out a way to make it so it cannot break. The try catches dont work because if the first
        //arg isnt a channel it just errors out. Need to figure out how to get around this.
        [Command("custom post")]
        public async Task CustomPostToChannelAsync(SocketTextChannel channel, [Remainder] string message)
        {
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");
            //Checks to see if the person who used the command has the staff role
            if (staffRole != null)
            {
                //Gets the server record from the database
                var recs = MongoCRUD.Instance.LoadServerRec<GuildModel>(Context.Guild.Id.ToString(), "GuildID", "Servers");

                //Gets the index of the recs array where the guild id is equal to the guild the command was used in
                int index = recs.IndexOf(recs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());

                var rec = recs[index].CustomAnnouncement;

                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle(rec.Title).WithColor(Discord.Color.Red)
                    .WithDescription(rec.Body).WithFooter(rec.Footer);

                try
                {
                    builder.WithThumbnailUrl(rec.Thumbnail);
                    try
                    {
                        await channel.SendMessageAsync(message, false, builder.Build());
                    }
                    catch
                    {
                        await ReplyAsync("ERROR: Text Channel doens't exist.");
                    }
                }
                catch
                {
                    try
                    {
                        await channel.SendMessageAsync(message, false, builder.Build());
                    }
                    catch
                    {
                        await ReplyAsync("ERROR: Text Channel doens't exist.");
                    }
                }

            }

        }

        #endregion
    }
}