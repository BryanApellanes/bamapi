using Bam.Net.CoreServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Application
{
    public class WebServiceInfo
    {
        public WebServiceInfo(params Type[] serviceTypes) : this(ServiceRegistry.Default, serviceTypes)
        { }

        public WebServiceInfo(ServiceRegistry dependencyRegistry, params Type[] serviceTypes)
        {
            this.DependencyRegistry = dependencyRegistry;
            this.ServiceTypes = new HashSet<Type>(serviceTypes);
        }

        public ServiceRegistry DependencyRegistry { get; private set; }
        public HashSet<Type> ServiceTypes { get; private set; }
    }
}
