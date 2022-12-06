using Bam.Net.CommandLine;
using Bam.Net.Server;
using Bam.Net.ServiceProxy;
using Bam.Net.ServiceProxy.Encryption;
using Bam.Net.Testing.Integration;
using Bam.Net.Testing.Unit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bam.Net;
using System.Threading;

namespace Bam.Application.Test
{
    public class BamApiShould
    {
        // TODO: write tests for the following

        // Execute unencrypted
        // Execute encrypted
        // Validate method attributes
        // Validate Hmac signature
        // Authorize bam://org:app:user
        [UnitTest("BamApi.CreateApiServerAsync should create BamApiServer that responds to BamApi proxy GET.")]
        public async Task ServeType()
        {
            BamApiServer apiServer = await BamApi.CreateApiServerAsync();
            apiServer.SetServiceType<Echo>();
            apiServer.Start();

            Message.PrintLine(apiServer.DefaultHostBinding.ToString());

            Echo echoProxy = await BamApi.GetProxyAsync<Echo>(apiServer.DefaultHostBinding);
            string testStringValue = 8.RandomLetters();
            string response = echoProxy.Send(testStringValue);

            response.ShouldBeEqualTo(testStringValue);
            Thread.Sleep(300);
            apiServer.Stop();
        }

        [UnitTest]
        public async Task CreateContext()
        {
            BamApiContext bamApiContext = await BamApi.StartAsync(new BamApiOptions { ServiceTypes = new HashSet<Type> { typeof(Echo) } });

            HashSet<Type> types = new HashSet<Type>(bamApiContext.ServiceTypes);
            types.Contains(typeof(Echo)).ShouldBeTrue();
        }

        [UnitTest]
        public async Task ServeEncryptedType()
        {
            BamApiContext bamApiContext = await BamApi.StartAsync(new BamApiOptions { ServiceTypes = new HashSet<Type> { typeof(EncryptedEcho) } });

            EncryptedEcho encryptedEcho = await BamApi.GetProxyAsync<EncryptedEcho>(bamApiContext.DefaultHostBinding);
            string testStringValue = 8.RandomLetters();
            string response = encryptedEcho.Send(testStringValue);

            response.ShouldBeEqualTo(testStringValue);
            Thread.Sleep(300);
            bamApiContext.Stop();
        }

        [UnitTest]
        public async Task ServeConfiguredEncryptedType()
        {
            BamApiContext bamApiContext = await BamApi.StartAsync(new BamApiOptions()
            {
                ServerName = $"{nameof(ServeConfiguredEncryptedType)}_test",
                ConfigureDependencies = (svcReg) =>
                {

                },
                ConfigureServices = (svcReg) =>
                {
                    return new Type[] { typeof(EncryptedEcho) };
                }
            });

            EncryptedEcho echoProxy = await BamApi.GetProxyAsync<EncryptedEcho>(bamApiContext.DefaultHostBinding);
            string testStringValue = 8.RandomLetters();
            string response = echoProxy.Send(testStringValue);

            response.ShouldBeEqualTo(testStringValue);
            Thread.Sleep(300);
            bamApiContext.Stop();
        }
    }
}
