using Bam.Net.CoreServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bam.Net;
using Bam.Net.Application;
using CsQuery.EquationParser.Implementation;
using Bam.Net.Server;
using Bam.Net.Services;

namespace Bam.Application
{
    public class BamApiOptions
    {
        public BamApiOptions()
        {
            this.ServiceTypes = new HashSet<Type>();
            this.ServerName = $"BamApiServer::{ApplicationNameProvider.Default.GetApplicationName()}";
            this.HostBindings = new HostBinding[] { new ManagedServerHostBinding(this.ServerName) };
            this.ApiConf = new ApiConf();
            this.ConfigureDependencies = (svcReg) => { };
            this.ConfigureServices = (svcReg) => WebServiceRegistry.FromRegistry(svcReg).ClassNameTypes;
        }

        public BamApiOptions(Action<ServiceRegistry> configureDependencies, Func<ServiceRegistry, object[]> configureServiceTypes, params HostBinding[] hostBindings)
        {
            if (configureDependencies == null)
            {
                throw new ArgumentNullException(nameof(configureDependencies));
            }
            if (configureServiceTypes == null)
            {
                throw new ArgumentNullException(nameof(configureServiceTypes));
            }

            this.ServiceTypes = new HashSet<Type>();
            this.ServerName = $"BamApiServer::{ApplicationNameProvider.Default.GetApplicationName()}";
            this.HostBindings = hostBindings ?? new HostBinding[] { new ManagedServerHostBinding(this.ServerName) };
            this.ApiConf = new ApiConf();
            this.ConfigureDependencies = configureDependencies;
            this.ConfigureServices = configureServiceTypes;
        }

        public HostBinding[] HostBindings { get; set; }

        public string ServerName { get; set; }
        public ApiConf ApiConf { get; set; }

        public Action<ServiceRegistry> ConfigureDependencies { get; set; }
        public Func<ServiceRegistry, object[]> ConfigureServices { get; set; }
        public HashSet<Type> ServiceTypes { get; internal set; }
    }
}
