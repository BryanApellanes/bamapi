using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bam.Net;
using Bam.Net.Encryption;
using Bam.Net.ServiceProxy.Encryption;

namespace Bam.Net.Application
{
    [Proxy("glooTestSvc")]
    public class BamApiTestService
    {
        public BamApiMonkey GetMonkey(string name)
        {
            return new BamApiMonkey(name);
        }
    }

    [Encrypt]
    [Proxy("glooEncryptedTestSvc")]
    public class BamApiEncryptedTestService
    {
        public BamApiMonkey GetMonkey(string name)
        {
            return new BamApiMonkey(string.Format("From Encrypted Test Service: {0}", name));
        }
    }

    [ApiHmacKeyRequired]
    [Proxy("glooApiKeyRequiredSvc")]
    public class BamApiKeyRequiredTestService
    {
        public BamApiMonkey GetMonkey(string name)
        {
            return new BamApiMonkey(string.Format("From ApiKeyRequired Test Service: {0}", name));
        }
    }
}
