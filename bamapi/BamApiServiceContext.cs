using Bam.Net.Application;
using Bam.Net.CoreServices;
using Bam.Net.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Application
{
    public class BamApiServiceContext
    {
        public BamApiServer Server { get; internal set; }
        public ApiConf ApiConf { get; internal set; }
        public ServiceRegistry DependencyRegistry 
        {
            get => Server?.DependencyRegistry;
        }
        public Type[] ServiceTypes { get; internal set; }
        public HostBinding[] HostBindings { get; internal set; }

    }
}
