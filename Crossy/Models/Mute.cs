using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Crossy.Models
{
    public class Mute
    {
        [BsonId]
        public DateTime MuteFinished { get; set; }
        public DateTime TimeMuted { get; set; }
        public string Duration { get; set; }
        public string Reason { get; set; }
        public Target Target { get; set; }
        public Moderator Moderator { get; set; }
    }
}
