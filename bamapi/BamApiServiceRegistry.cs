using Bam.Net.CoreServices;
using Bam.Net.Logging;
using Bam.Net.ServiceProxy.Secure;
using Bam.Net.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Net.Application
{
    public class BamApiServiceRegistry : ApplicationServiceRegistry
    {
        [ServiceRegistryLoader]
        public static BamApiServiceRegistry ForConfiguration(ApiConfig config, ILogger logger = null)
        {
            BamApiServiceRegistry bamApiServiceRegistry = new BamApiServiceRegistry();
            bamApiServiceRegistry.CombineWith(Configure(appRegistry =>
            {
                appRegistry
                    .For<ISecureChannelSessionDataManager>().Use<SecureChannelSessionDataManager>();
            }));

            return bamApiServiceRegistry;
        }
    }
}
