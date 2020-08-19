using System;
using System.Collections.Generic;

namespace Crossy.Models
{
    public class UserWarning
    {
        public string UserId { get; set; }
        public List<Warning> Warnings { get; set; }
    }
}
