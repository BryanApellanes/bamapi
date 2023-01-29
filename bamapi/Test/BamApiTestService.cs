using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bam.Application.Test.Data;
using Bam.Net;
using Bam.Net.Encryption;
using Bam.Net.ServiceProxy.Encryption;

namespace Bam.Application
{
    [Proxy("testSvc")]
    public class BamApiTestService
    {
        public BamApiMonkey GetMonkey(string name)
        {
            return new BamApiMonkey(name);
        }
    }

    [Encrypt]
    [Proxy("encryptedTestSvc")]
    public class BamApiEncryptedTestService
    {
        public BamApiMonkey GetMonkey(string name)
        {
            return new BamApiMonkey(string.Format("From Encrypted Test Service: {0}", name));
        }
    }

    [ApiHmacKeyRequired]
    [Proxy("apiHmacKeyRequiredSvc")]
    public class BamApiKeyRequiredTestService
    {
        public BamApiMonkey GetMonkey(string name)
        {
            return new BamApiMonkey(string.Format("From ApiKeyRequired Test Service: {0}", name));
        }
    }
}
