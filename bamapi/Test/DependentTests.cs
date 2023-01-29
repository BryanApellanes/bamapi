using Bam.Net.Server;
using Bam.Net.Testing.Unit;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bam.Net;

namespace Bam.Application.Test
{
    public class DependentTests
    {
        [UnitTest]
        public void ManagedServerHostBindingTest()
        {
            string testServerName = $"{nameof(ManagedServerHostBindingTest)}_{8.RandomLetters()}";
            IManagedServer mockServer = Substitute.For<IManagedServer>();
            mockServer.ServerName.Returns(testServerName);
            ManagedServerHostBinding serverHostBinding1 = new ManagedServerHostBinding(mockServer);
            ManagedServerHostBinding serverHostBinding2 = new ManagedServerHostBinding(testServerName);
            
            Expect.AreEqual(testServerName, serverHostBinding1.ServerName);
            Expect.AreEqual(testServerName, serverHostBinding2.ServerName);
            Expect.AreEqual(serverHostBinding1.Port, serverHostBinding2.Port);
        }
    }
}
