using Bam.Net;
using Bam.Net.Automation.Testing.Data.Dao;
using Bam.Net.CommandLine;
using Bam.Net.Incubation;
using Bam.Net.Server;
using Bam.Net.ServiceProxy;
using Bam.Net.Testing;
using Bam.Net.Testing.Unit;
using Org.BouncyCastle.Crypto.Tls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bam.Application.Test
{
    public class BamPlatformShould : CommandLineTool
    {
        [UnitTest("BamPlatform.CreateServerAsync should create BamServer that responds to BamApi proxy GET.")]
        public async Task ServeUnencryptedService()
        {
            BamServer bamServer = await BamPlatform.CreateServerAsync();
            bamServer.AddCommonService<Echo>();
            bamServer.Start();
            
            Message.PrintLine($"{nameof(ServeUnencryptedService)}::{bamServer.DefaultHostBinding}");

            Echo echoProxy = await BamApi.GetClientAsync<Echo>(bamServer.DefaultHostBinding);
            string testStringValue = 8.RandomLetters();
            string response = echoProxy.Send(testStringValue);

            response.ShouldBeEqualTo(testStringValue);

            Thread.Sleep(300);
            bamServer.Stop();
        }

        [UnitTest("BamPlatform.CreateServerAsync should create a BamServer that responds to encrypted proxy.")]
        public async Task ServeEncryptedService()
        {
            BamServer bamServer = await BamPlatform.CreateServerAsync();
            bamServer.AddCommonService<EncryptedEcho>();
            bamServer.Start();

            Message.PrintLine($"{nameof(ServeEncryptedService)}::{bamServer.DefaultHostBinding}");

            EncryptedEcho echoProxy = await BamApi.GetClientAsync<EncryptedEcho>(bamServer.DefaultHostBinding);
            string testStringValue = 8.RandomLetters();
            string response = echoProxy.Send(testStringValue);

            response.ShouldBeEqualTo(testStringValue);
            Thread.Sleep(300);
            bamServer.Stop();
        }

        [UnitTest]
        public void GetSameUnprivilegedPortForName()
        {
            int port = BamPlatform.GetUnprivilegedPortForName(nameof(GetSameUnprivilegedPortForName));
            int check = BamPlatform.GetUnprivilegedPortForName(nameof(GetSameUnprivilegedPortForName));
            Expect.AreEqual(port, check);
            for (int i = 0; i < 2000; i++)
            {
                int checkAgain = BamPlatform.GetUnprivilegedPortForName(nameof(GetSameUnprivilegedPortForName));

                Expect.AreEqual(port, checkAgain);
                Expect.IsGreaterThanOrEqualTo(port, 1024);
                Expect.IsLessThanOrEqualTo(port, 65535);
            }
            Message.PrintLine("Test Port = {0}", port);
        }
    }
}
