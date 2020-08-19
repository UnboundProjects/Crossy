using System;
namespace Crossy.Models
{
    public class Warning
    {
        public string WarnReason
        {
            get;
            set;
        }
        public string DateTime
        {
            get;
            set;
        }
        public Moderator Moderator
        {
            get;
            set;
        }
    }
}
