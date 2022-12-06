using Bam.Net.CoreServices;
using Bam.Net.Server;
using Bam.Net.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Application
{
    public class BamApiContext
    {
        public BamApiContext(BamApiServiceContext serviceContext)
        {
            this.ApiServiceContext = serviceContext;
        }

        internal BamApiServiceContext ApiServiceContext { get; set; }

        public bool ServerIsListening
        {
            get => (bool)ApiServer?.IsListening;
        }

        public HostBinding DefaultHostBinding
        {
            get => ApiServer?.DefaultHostBinding;
        }

        public BamApiServer ApiServer
        {
            get => ApiServiceContext?.Server;
        }

        public ServiceRegistry DependencyRegistry
        {
            get => ApiServiceContext?.DependencyRegistry ?? new ServiceRegistry();
        }

        public HashSet<Type> ServiceTypes
        {
            get => new HashSet<Type>(ApiServiceContext?.ServiceTypes);
        }

        public void Start()
        {
            ApiServer.Start();
        }

        public void Stop()
        {
            ApiServer.Stop();
        }
    }
}
