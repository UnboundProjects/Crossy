using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Crossy.Models
{
    public class GuildModel
    {
        [BsonId]
        public string GuildID { get; set; }
        public GuildInfo GuildInfo { get; set; }
        public List<Mute> Mutes { get; set; }
        public List<Reaction> Reactions { get; set; }
        public List<UserWarning> UserWarnings { get; set; }
        public CustomAnnouncement CustomAnnouncement { get; set; }
    }
}
