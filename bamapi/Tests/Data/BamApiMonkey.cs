using Bam.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Application.Test.Data
{
    public class BamApiMonkey
    {
        public BamApiMonkey() { }
        public BamApiMonkey(string name)
        {
            Name = name;
            Birthday = DateTime.UtcNow.Subtract(TimeSpan.FromDays(365 * RandomNumber.Between(10, 150)));
            HasTail = RandomHelper.Bool();
        }
        public string Name { get; set; }
        public bool HasTail { get; set; }
        public DateTime Birthday { get; set; }
    }
}
