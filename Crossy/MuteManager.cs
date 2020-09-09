using System;
using System.Linq;
using System.Threading.Tasks;
using Crossy.Models;
using Discord.WebSocket;

namespace Crossy
{
    public class MuteManager
    {
        private static MuteManager instance;
        private MuteManager() { }

        public static MuteManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MuteManager();
                }

                return instance;
            }
        }
        public async Task CheckMutesAsync()
        {
            await Task.Delay(120000);
            DateTime todayDateTime = DateTime.UtcNow;

            bool removedMute = false;

            var serverRecs = MongoCRUD.Instance.LoadRecords<GuildModel>("Servers");
            foreach(var rec in serverRecs)
            {
                var guildId = rec.GuildID;
                foreach(var mute in rec.Mutes)
                { 
                    if(todayDateTime >= mute.MuteFinished)
                    {
                        var guild = DiscordBot.Instance._client.GetGuild(ulong.Parse(guildId));
                        SocketGuildUser user = guild.GetUser(mute.Target.id);
                        var role = guild.Roles.FirstOrDefault(r => r.Name == "Muted");

                        await user.RemoveRoleAsync(role);

                        serverRecs.Remove(rec);

                        removedMute = true;
                    }
                    if (removedMute)
                    {
                        MongoCRUD.Instance.UpdateRecord("Servers", rec.GuildID.ToString(), rec);
                    }
                }

            }
            
            _ = Task.Run(CheckMutesAsync);
        }
    }
}
