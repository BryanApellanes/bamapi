using Bam.Net;
using Bam.Net.CoreServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bam.Net.Incubation;
using Bam.Application;

namespace Bam.Application.Test.Data
{
    [Serializable]
    [ServiceRegistryContainer]
    public class BamApiTestRegistry
    {
        [ServiceRegistryLoader(nameof(BamApiTestRegistry), ProcessModes.Dev, ProcessModes.Test)]
        public static ServiceRegistry CreateTestRegistry()
        {
            return ServiceRegistry.Create()
                .For<BamApiTestService>().Use<BamApiTestService>()
                .For<BamApiEncryptedTestService>().Use<BamApiEncryptedTestService>()
                .For<BamApiKeyRequiredTestService>().Use<BamApiKeyRequiredTestService>()
                .Cast<ServiceRegistry>();
        }
    }
}
