using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Crossy.Models
{
    public class GuildModel
    {
        [BsonId]
        public ulong GuildID { get; set; }
    }
}
