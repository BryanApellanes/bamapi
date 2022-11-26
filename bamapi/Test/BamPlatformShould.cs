using Bam.Net;
using Bam.Net.CommandLine;
using Bam.Net.Server;
using Bam.Net.ServiceProxy;
using Bam.Net.Testing.Unit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bam.Application.Test
{
    public class BamPlatformShould
    {
        [UnitTest]
        public async Task ServeFunctionalTypeClient()
        {
            BamServer bamServer = await BamPlatform.CreateServerAsync();
            bamServer.AddCommonService<Echo>();
            bamServer.Start();
            Message.PrintLine(bamServer.DefaultHostBinding.ToString());

            Echo echoProxy = await BamApi.GetProxyAsync<Echo>(bamServer.DefaultHostBinding);
            string testStringValue = 8.RandomLetters();
            string response = echoProxy.Send(testStringValue);

            response.ShouldBeEqualTo(testStringValue);

            Thread.Sleep(1500);
            bamServer.Stop();
        }
    }
}
