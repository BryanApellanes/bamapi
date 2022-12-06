using Bam.Net.CoreServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Application
{
    public class BamApiServiceRegistryOptions : BamApiOptions
    {
        public BamApiServiceRegistryOptions(params string[] registryNames) : base(svcReg => { }, svcReg => new Type[] { })
        {
            this.RegistryNames = new HashSet<string>(registryNames);
        }

        public ServiceRegistryService ServiceRegistryService { get; set; }
        public HashSet<string> RegistryNames { get; }
    }
}
