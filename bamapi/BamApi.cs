using Bam.Net.CoreServices;
using Bam.Net.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Net.Application
{
    public class BamApi
    {
        public static async Task<BamApiServer> CreateApiServer()
        {
            return await BamPlatform.CreateManagedServerAsync(() => new BamApiServer(new Server.HostBinding()));
        }

        public static async Task<T> GetProxyAsync<T>()
        {
            ProxyFactory factory = new ProxyFactory();
            return await Task.Run(() => factory.GetProxy<T>());
        }

        public static async Task<T> GetProxyAsync<T>(string hostName, int port = 80, ILogger logger = null)
        {
            ProxyFactory factory = new ProxyFactory();
            return await Task.Run(() => factory.GetProxy<T>(hostName, port, logger));
        }
    }
}
