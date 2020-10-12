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

/*
 * 
 * TO DO
 * Remove the index variable and just find the rec properly like in the first command :)
 * Add catches to !mute command to stop people from being dumb
 */

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
        
        //TO DO:
        //Check to see if the document is there first before creating another, otherwise
        //The whole thing will bug out, poggers!
        [Command("setup")]
        public async Task SetupAsync()
        {
            //VARIABLES
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

            //Run the InitServer method which inputs the server into the database
            MongoCRUD.Instance.InitServer(newGuild);
            
            //Reply with setup is complete if it doesnt break
            await ReplyAsync("Setup complete");
        }
        #region Moderation
        #region Warn related
        [Command("warn")]
        public async Task WarnAsync(SocketGuildUser target, [Remainder] string reason)
        {
            //VARIABLES
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");
            //Checks to see if the person who used the command has the staff role
            if (staffRole != null)
            {
                //Gets the server record from the database
                var recs = MongoCRUD.Instance.LoadServerRec<GuildModel>(Context.Guild.Id.ToString(), "GuildID", "Servers");

                //Gets the index of the recs array where the guild id is equal to the guild the command was used in
                int index = recs.IndexOf(recs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());

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
        }
        [Command("warnings")]
        public async Task WarningsAsync(SocketUser target)
        {
            //VARIABLES
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");
            
            //If the user has the Staff role
            if (staffRole != null)
            {
                //Set amount to 0
                int amount = 0;

                //Load in recs
                var recs = MongoCRUD.Instance.LoadRecords<GuildModel>("Servers");
                int index = recs.IndexOf(recs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());

                //Set sb to a new string builder
                StringBuilder sb = new StringBuilder();
                
                
                if (recs.Count() != 0)
                {
                    //Trying to see if the user has any warnings
                    try
                    {
                        //Sets warnings to the users warnings
                        var warnings = recs[index].UserWarnings.FirstOrDefault(i => i.UserId == target.Id.ToString()).Warnings;
                        
                        //Sets amount to the count of the warnings the user has
                        amount = warnings.Count(); 

                        //For every item in the array, append it to the string builder
                        for (int i = 0; i < amount; i++)
                        {
                            sb.Append($"**Warning #{i + 1}**: {warnings[i].WarnReason} - {warnings[i].DateTime}\n\n");
                        }

                        //Creates an embed builder
                        EmbedBuilder builder = new EmbedBuilder();

                        //Adds information to the embed builder
                        builder.WithTitle($"**Warnings for {target.Username}#{target.Discriminator}**").WithColor(Discord.Color.Red)
                            .WithDescription(sb.ToString()).WithThumbnailUrl(target.GetAvatarUrl()).WithFooter($"Total: {amount}");
                        
                        //Reply to the user with the built embed
                        await ReplyAsync("", false, builder.Build());
                    }

                    //Catches it if the users warnings cannot be found
                    catch
                    {

                        //Reply to the user with the built embed
                        await ReplyAsync("This user has no warnings.");
                    }
                }
            }
        }
        [Command("warning")]
        public async Task WarningAsync(SocketGuildUser target, int warning)
        {
            //VARIABLES
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");
            
            //If the user has the staff role
            if (staffRole != null)
            {
                //MORE VARIABLES
                //Take 1 from the warning amount since numbers start at 0 in programming
                warning -= 1;
                //Get the server recs
                var serverRecs = MongoCRUD.Instance.LoadRecords<GuildModel>("Servers");
                //Get the index of the server the command was used in
                int index = serverRecs.IndexOf(serverRecs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());
                //Save serverRecs[index].UserWarnings as recs for ease of access
                var recs = serverRecs[index].UserWarnings;
                //Get the index of the target's warnings inside the UserWarnings array
                var warningIndex = recs.IndexOf(recs.Where(p => p.UserId == target.Id.ToString()).FirstOrDefault());
                
                //If recs exists. This is a safe guard if statement
                if (recs.Count != 0)
                {

                    //If the user has warnings
                    if (recs[warningIndex].Warnings != null)
                    {
                        //Create the embed builder object
                        EmbedBuilder builder = new EmbedBuilder();
                        
                        //Add information to the builder
                        builder.WithTitle($"**Warning #{warning + 1} for {target.Username}#{target.Discriminator}**").WithColor(Discord.Color.Red)
                            .WithDescription($"Reason: {recs[warningIndex].Warnings[warning].WarnReason}\n\nTime given: {recs[warningIndex].Warnings[warning].DateTime}\n\n" +
                                $"Moderator: {recs[warningIndex].Warnings[warning].Moderator.Username}#{recs[warningIndex].Warnings[warning].Moderator.Discriminator}").WithThumbnailUrl(target.GetAvatarUrl());
                        
                        //Reply to the user with the embed
                        await ReplyAsync("", false, builder.Build());
                    }
                    //If the user has no warnings
                    else
                    {
                        //Reply to the user
                        await ReplyAsync("User's warning doesn't exist.");
                    }
                }
                //If the recs dont exist
                else
                {
                    //Reply to the user
                    await ReplyAsync("This user has no warnings.");
                }
            }
            //If they dont have the staff role
            else
            {
                //Reply to the user 
                await ReplyAsync("You do not have permission to use this command.");
            }
        }
        [Command("rm warning")]
        public async Task RmWarningAsync(SocketGuildUser target, int warning)
        {
            //VARIABLES
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");

            //If the user has the staff role
            if (staffRole != null)
            {
                //MORE VARIABLES
                //Take 1 from the warning amount since numbers start at 0 in programming
                warning -= 1;
                var serverRecs = MongoCRUD.Instance.LoadRecords<GuildModel>("Servers");
                //Get the index of the server the command was used in
                int index = serverRecs.IndexOf(serverRecs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());
                //Save serverRecs[index].UserWarnings as recs for ease of access
                var recs = serverRecs[index].UserWarnings;
                //Get the index of the target's warnings inside the UserWarnings array
                var warningIndex = recs.IndexOf(recs.Where(p => p.UserId == target.Id.ToString()).FirstOrDefault());
                
                //If recs exists. This is a safe guard if statement
                if (recs.Count != 0)
                {
                    //Remove the selected warning from the recs array
                    recs[warningIndex].Warnings.Remove(recs[warningIndex].Warnings[warning]);
                    //Update the record
                    MongoCRUD.Instance.UpdateRecord("Servers", serverRecs[index].GuildID.ToString(), serverRecs[index]);

                    //Reply to the user
                    await ReplyAsync("User's warning has been removed.");
                }
                //If the rec doesnt exist
                else
                {
                    //Reply to the user
                    await ReplyAsync("This user has no warnings.");
                }
            }
            //If the user doesnt have the staff role
            else
            {
                //Reply to the user
                await ReplyAsync("You do not have permission to use this command.");
            }
        }
        #endregion
        #region Mute Related
        [Command("mute")]
        public async Task MuteAsync(SocketGuildUser targetFake, string time, [Remainder] string reason)
        {
            //VARIABLES
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");

            //If the user has the staff role
            if (staffRole != null)
            {
                //Get the server recs
                var serverRecs = MongoCRUD.Instance.LoadRecords<GuildModel>("Servers");
                //Gets the index of the recs array where the guild id is equal to the guild the command was used in
                int index = serverRecs.IndexOf(serverRecs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());
                //Create a moderator object
                Moderator moderator = new Moderator
                {
                    Username = Context.User.Username,
                    Discriminator = Context.User.DiscriminatorValue,
                    id = Context.User.Id
                };
                //Create a target object
                Target target = new Target
                {
                    Username = targetFake.Username,
                    Discriminator = targetFake.DiscriminatorValue,
                    id = targetFake.Id
                };
                //Create the muteModel object
                Mute muteModel = new Mute
                {
                    TimeMuted = System.DateTime.Now,
                    Reason = reason,
                    Moderator = moderator,
                    Target = target
                };
                #region Adding time stuff
                //If 2nd arg contains D
                if (time.Contains("d"))
                {
                    //Get the time string and remove the D
                    var realTime = time.Substring(0, time.Length - 1);
                    //Set the muteModel.Duration to the time string
                    muteModel.Duration = time;

                    //Create a new TimeSpam for the duration of the mute
                    System.TimeSpan duration = new System.TimeSpan(int.Parse(realTime), 0, 0, 0);
                    //Create a DateTime for when the mute is scheduled to end
                    DateTime endMute = System.DateTime.Now.Add(duration);
                    //Set muteModel.MuteFinished to endMute
                    muteModel.MuteFinished = endMute;
                }
                //If 2nd arg contains H
                else if (time.Contains("h"))
                {
                    //Get the time string and remove the H
                    var realTime = time.Substring(0, time.Length - 1);
                    //Set the muteModel.Duration to the time string
                    muteModel.Duration = time;
                    //Create a new TimeSpam for the duration of the mute
                    System.TimeSpan duration = new System.TimeSpan(0, int.Parse(realTime), 0, 0);
                    //Create a DateTime for when the mute is scheduled to end
                    DateTime endMute = System.DateTime.Now.Add(duration);
                    //Set muteModel.MuteFinished to endMute
                    muteModel.MuteFinished = endMute;
                }
                //If 2nd arg contains M
                else if (time.Contains("m"))
                {
                    //Get the time string and remove the M
                    var realTime = time.Substring(0, time.Length - 1);
                    //Set the muteModel.Duration to the time string
                    muteModel.Duration = time;
                    //Create a new TimeSpam for the duration of the mute
                    System.TimeSpan duration = new System.TimeSpan(0, 0, int.Parse(realTime), 0);
                    //Create a DateTime for when the mute is scheduled to end
                    DateTime endMute = System.DateTime.Now.Add(duration);
                    //Set muteModel.MuteFinished to endMute
                    muteModel.MuteFinished = endMute;
                }
                #endregion
                //Add the mute to the mutes array inside the ServerRec
                serverRecs[index].Mutes.Add(muteModel);
                //Update the records
                MongoCRUD.Instance.UpdateRecord("Servers", serverRecs[index].GuildID.ToString(), serverRecs[index]);

                //Get the muteRole object from the guild
                var muteRole = Context.Guild.Roles.FirstOrDefault(r => r.Name == "Muted");

                //Add the muteRole to the user
                await targetFake.AddRoleAsync(muteRole);

                //Create the EmbedBuilder object
                EmbedBuilder builder = new EmbedBuilder();

                //Add information to the builder object
                builder.WithTitle($"**You have been muted in {Context.Guild.Name}**").WithDescription($"Duration: {time}\nReason: {reason}").WithColor(Discord.Color.Red)
                    .WithFooter("If you think this was an error, please contact a server moderator.");
                
                //Send the target a direct message with the embed
                await targetFake.SendMessageAsync("", false, builder.Build());

                //Reply to the user
                await ReplyAsync($"<@{targetFake.Id}> was muted by <@{Context.User.Id}>.\n" +
                    $"Duration: {time}\n" +
                    $"Reason: {reason}");
            }
            //If the user doesn't have the staff role
            else
            {
                //Reply to the user
                await ReplyAsync("You do not have permission to use this command.");
            }
        }
        [Command("unmute")]
        public async Task UnMuteAsync(SocketGuildUser targetFake)
        {
            //VARIABLES
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");

            //If the user has the staffRole
            if (staffRole != null)
            {
                //Get the server recs
                var serverRecs = MongoCRUD.Instance.LoadRecords<GuildModel>("Servers");
                //Gets the index of the recs array where the guild id is equal to the guild the command was used in
                int index = serverRecs.IndexOf(serverRecs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());
                //Gets the index of the mute where the target id matches the target specified from the mutes array
                int muteIndex = serverRecs[index].Mutes.IndexOf(serverRecs[index].Mutes.Where(x => x.Target.id == targetFake.Id).FirstOrDefault());

                //Remove the mute from the array
                serverRecs[index].Mutes.Remove(serverRecs[index].Mutes[muteIndex]);

                //Update the record inside the database
                MongoCRUD.Instance.UpdateRecord("Servers", serverRecs[index].GuildID.ToString(), serverRecs[index]);

                //Get the muted role from the guild
                var muteRole = Context.Guild.Roles.FirstOrDefault(r => r.Name == "Muted");

                //Remove the role from the target
                await targetFake.RemoveRoleAsync(muteRole);

                //Reply the user
                await ReplyAsync($"<@{targetFake.Id}> has been unmuted.");

            }
            //If the user doesnt have the staffRole
            else
            {
                //Reply to the user
                await ReplyAsync("You do not have permission to use this command.");
            }
        }
        [Command("mutes")]
        public async Task MutesAsync()
        {
            //VARIABLES
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");

            //If the user has the staff role
            if (staffRole != null)
            {
                //Get the server recs
                var serverRecs = MongoCRUD.Instance.LoadRecords<GuildModel>("Servers");
                //Gets the index of the recs array where the guild id is equal to the guild the command was used in
                int index = serverRecs.IndexOf(serverRecs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());

                //Create a string builder object
                StringBuilder sb = new StringBuilder();
                
                //For each mute in the mutes array inside the server recs
                foreach (var mute in serverRecs[index].Mutes)
                {
                    //New TimeSpan set to the MuteFinished - DateTime now
                    TimeSpan timeLeft = mute.MuteFinished.Subtract(DateTime.UtcNow);
                    //Format the TimeSpan and make it a string
                    string timeLeftTrimmed = string.Format($"{timeLeft.Days}:{timeLeft.Hours}:{timeLeft.Minutes}:{timeLeft.Seconds}");
                    //Append it to the string builder
                    sb.Append($"<@{mute.Target.id}> - {mute.Reason} - {timeLeftTrimmed}\n");
                }

                //Create a new embed builder
                EmbedBuilder builder = new EmbedBuilder();
                //Add stuff to the embed builder
                builder.WithTitle("Active Mutes:").WithDescription(sb.ToString()).WithColor(Discord.Color.DarkerGrey);

                //Reply to the user
                await ReplyAsync("", false, builder.Build());
            }
            //If the user doesn't have the staff role
            else
            {
                //Reply to the user
                await ReplyAsync("You do not have permission to use this command.");
            }
        }
        #endregion
        [Command("softban")]
        public async Task SoftbanAsync(SocketGuildUser user, [Remainder] string reason)
        {
            //VARIABLES
            var user1 = Context.User as SocketGuildUser;
            var staffRole = user1.Roles.FirstOrDefault(x => x.Name == "Staff");

            //If the user has the staff role
            if (staffRole != null)
            {
                //Ban the targeted user
                await user.BanAsync();
                //Remove the targeted user
                await Context.Guild.RemoveBanAsync(user);
                
                //Create an embed builder object
                EmbedBuilder builder = new EmbedBuilder();
                //Add information to the embed builder
                builder.WithTitle($"**{user.Username}#{user.Discriminator} has been soft banned.**").WithColor(Discord.Color.Red);

                //Create another embed builder object
                EmbedBuilder builder1 = new EmbedBuilder();
                //Add information to the embed builder
                builder1.WithTitle($"**You have been kicked from the {Context.Guild.Name} server**").WithDescription($"Reason: {reason}").WithColor(Discord.Color.Red)
                    .WithFooter("If you think this was an error, please contact a moderator.");

                //Send the 2nd embed builder to the user that was banned then unbanned
                await user.SendMessageAsync("", false, builder1.Build());
                //Reply to the user who used the command
                await ReplyAsync("", false, builder.Build());
            }
            //If the user doesn't have the staff role
            else
            {
                //Reply to the user
                await ReplyAsync("You do not have permission to use this command.");
            }
        }
        [Command("clear")]
        public async Task ClearAsync(int amount)
        {
            //VARAIBLES
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");

            //If the user has the staff role
            if (staffRole != null)
            {
                //Get the messages from the guild, plus 1 to include the command itself
                var messages = await Context.Channel.GetMessagesAsync(amount + 1).FlattenAsync();
                //Delete the amount of messages 
                await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messages);
                //Reply saying messages have been cleared, saving the msg object
                var msg = await ReplyAsync($"{amount} messages cleared.");

                //Wait 2 seconds
                await Task.Delay(2000);
                //Delete msg object
                await msg.DeleteAsync();
            }
            //If the user doesnt have the staff role
            else
            {
                //Reply to the user
                await ReplyAsync("You do not have permission to use this command.");
            }
        }
        #endregion
        #region Custom announcements
        [Command("custom body")]
        public async Task CustomBodyAsync([Remainder] string body)
        {
            //Variables
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

                //Reply to the user
                await ReplyAsync("Custom body has been set.");
            }
        }

        [Command("custom title")]
        public async Task CustomTitleAsync([Remainder] string title)
        {
            //Variables
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

                //Reply to the user
                await ReplyAsync("Custom title has been set.");
            }
            
        }
        [Command("custom footer")]
        public async Task CustomFooterAsync([Remainder] string footer)
        {
            //Variables
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

                //Reply to the user
                await ReplyAsync("Custom footer has been set.");
            }

        }

        [Command("custom thumbnail")]
        public async Task CustomThumbnailAsync([Remainder] string thumbnail)
        {
            //Variables
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

                //Reply to the user
                await ReplyAsync("Custom thumbnail has been set.");
            }

        }

        [Command("custom post")]
        public async Task CustomPostAsync()
        {
            //Variables
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");
            //Checks to see if the person who used the command has the staff role
            if (staffRole != null)
            {
                //Gets the server record from the database
                var recs = MongoCRUD.Instance.LoadServerRec<GuildModel>(Context.Guild.Id.ToString(), "GuildID", "Servers");

                //Gets the index of the recs array where the guild id is equal to the guild the command was used in
                int index = recs.IndexOf(recs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());

                //Set rec to the custom announement embed inside the server rec. this is just to make the code cleaner
                var rec = recs[index].CustomAnnouncement;

                //Create an embed builder
                EmbedBuilder builder = new EmbedBuilder();
                //Add information to the embed builder
                builder.WithTitle(rec.Title).WithColor(Discord.Color.Red)
                    .WithDescription(rec.Body).WithFooter(rec.Footer);

                //Try add the thumbnail, if it's not a proper URI it wont work
                try
                {
                    //Add the thumbnail to the builder
                    builder.WithThumbnailUrl(rec.Thumbnail);
                    //Reply to the user
                    await ReplyAsync("", false, builder.Build());
                }
                //If it fails to add the proper URI
                catch
                {
                    //Reply to the user
                    await ReplyAsync("", false, builder.Build());
                }
                
            }

        }

        [Command("custom post")]
        public async Task CustomPostToChannelAsync(SocketTextChannel channel, [Remainder] string message)
        {
            //Variables
            var user = Context.User as SocketGuildUser;
            var staffRole = user.Roles.FirstOrDefault(x => x.Name == "Staff");
            //Checks to see if the person who used the command has the staff role
            if (staffRole != null)
            {
                //Gets the server record from the database
                var recs = MongoCRUD.Instance.LoadServerRec<GuildModel>(Context.Guild.Id.ToString(), "GuildID", "Servers");

                //Gets the index of the recs array where the guild id is equal to the guild the command was used in
                int index = recs.IndexOf(recs.Where(p => p.GuildID == Context.Guild.Id.ToString()).FirstOrDefault());

                //Set rec to the custom announement embed inside the server rec. this is just to make the code cleaner
                var rec = recs[index].CustomAnnouncement;

                EmbedBuilder builder = new EmbedBuilder();
                builder.WithTitle(rec.Title).WithColor(Discord.Color.Red)
                    .WithDescription(rec.Body).WithFooter(rec.Footer);

                //Try add the thumbnail, if it's not a proper URI it wont work
                try
                {
                    //Add the thumbnail to the embed
                    builder.WithThumbnailUrl(rec.Thumbnail);
                    //Send the message to the channel provided
                    await channel.SendMessageAsync(message, false, builder.Build());
                }
                //If it fails to add a proper URI
                catch
                {
                    //Send the message to the channel provided
                    await channel.SendMessageAsync(message, false, builder.Build());
                }

            }

        }

        #endregion
    }
}