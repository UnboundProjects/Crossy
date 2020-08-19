using System;
namespace Crossy.Models
{
    public class GuildInfo
    {
        public string ServerName { get; set; }
        public string CreationDate { get; set; }
        public string Creator { get; set; }
        public ulong CreatorId { get; set; }
        public string BannerURL { get; set; }
    }
}
