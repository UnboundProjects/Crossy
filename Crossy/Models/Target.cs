using System;
namespace Crossy.Models
{
    public class Target
    {
        public ulong id
        {
            get;
            set;
        }
        public string Username
        {
            get;
            set;
        }
        public int Discriminator
        {
            get;
            set;
        }
    }
}
