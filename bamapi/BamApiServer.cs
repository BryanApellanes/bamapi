using Bam.Net.Configuration;
using Bam.Net.CoreServices;
using Bam.Net.Logging;
using Bam.Net.Server;
using Bam.Net.ServiceProxy.Encryption;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Timers;
using Bam.Net.Services.Clients;
using Bam.Net.Services;
using Bam.Net;

namespace Bam.Application
{
    public class BamApiServer : SimpleServer<BamApiResponder>, IManagedServer
    {
        public BamApiServer(params HostBinding[] hostBindings): this(new BamConf(), Log.Default, false)
        {
            SetHostBindings(hostBindings);
        }

        public BamApiServer(BamConf conf, ILogger logger, bool verbose = false)
            : base(new BamApiResponder(conf, logger, verbose), logger)
        {
            Type type = this.GetType();
            ServerName = $"{type.Namespace}.{type.Name}_{Environment.MachineName}_{Guid.NewGuid()}";

            Responder.Initialize();
            CreatedOrChangedHandler = (o, fsea) =>
            {
                ReloadServices(fsea);
            };
            RenamedHandler = (o, rea) =>
            {
                DirectoryInfo dir = GetDirectory(rea.FullPath);
                if (dir != null)
                {
                    TryReloadServices(dir);
                }
            };
            ServiceTypes = new HashSet<Type>();
        }
        
        public override void Start()
        {
            if (MonitorDirectories.Length > 0)
            {
                ServiceTypes.Clear();
                MonitorDirectories.Each(new { Server = this }, (ctx, dir) =>
                {
                    ctx.Server.TryReloadServices(new DirectoryInfo(dir));
                });
            }
            else
            {
                RegisterServiceTypes();
            }
            base.Start();
        }

        public string ServerName { get; set; }

        public HostBinding DefaultHostBinding
        {
            get => HostBindings.FirstOrDefault() ?? new ManagedServerHostBinding(this);
        }

        ApplicationServiceRegistry _dependencyRegistry;
        public ApplicationServiceRegistry DependencyRegistry
        {
            get
            {
                return _dependencyRegistry;
            }
            set
            {
                this._dependencyRegistry = BamApi.DependencyRegistry;
                this.Responder.ServiceProxyResponder.DependencyInjectionServiceRegistry = DependencyRegistry;
            }
        }

        readonly object _serviceTypeLock = new object();
        public HashSet<Type> ServiceTypes { get; private set; }

        /// <summary>
        /// Set a single service of the specified generic type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void SetServiceType<T>()
        {
            SetServiceType(typeof(T));
        }

        /// <summary>
        /// Set a single service of the specified type.
        /// </summary>
        /// <param name="serviceType"></param>
        public void SetServiceType(Type serviceType)
        {
            SetServiceTypes(serviceType);
        }

        /// <summary>
        /// Serve the specified types.
        /// </summary>
        /// <param name="serviceTypes"></param>
        public void SetServiceTypes(params Type[] serviceTypes)
        {
            RegisterServiceTypes(serviceTypes);
        }

        protected ServiceProxyResponder RegisterServiceTypes(IEnumerable<Type> serviceTypes)
        {
            lock (_serviceTypeLock)
            {
                ServiceTypes.Clear();
                serviceTypes.Each(st => ServiceTypes.Add(st));
            }
            return RegisterServiceTypes();
        }

        protected ServiceProxyResponder RegisterServiceTypes()
        {
            BamApiResponder api = Responder;
            ServiceProxyResponder responder = api.ServiceProxyResponder;
            AddCommonServices(responder);
            api.RpcResponder.Executors = responder.CommonServiceProvider;
            return responder;
        }

        private HostBinding[] SetHostBindings(HostBinding[] hostBindings)
        {
            if (hostBindings == null || hostBindings.Length == 0)
            {
                hostBindings = new HostBinding[] { new ManagedServerHostBinding(this) };
            }
            HostBindings = new HashSet<HostBinding>(hostBindings);
            return hostBindings;
        }

        private void AddCommonServices(ServiceProxyResponder responder)
        {
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            ServiceTypes.Each(new {  Logger, Responder = responder }, (ctx, serviceType) =>
            {
                ctx.Responder.RemoveCommonService(serviceType);
                ctx.Responder.AddCommonService(serviceType, ServiceRegistry.GetServiceLoader(serviceType, entryAssembly));
                ctx.Logger.AddEntry("Added service: {0}", serviceType.FullName);
            });
            IApiHmacKeyResolver apiKeyResolver = DependencyRegistry.Get<IApiHmacKeyResolver>(new CoreClient());
            responder.CommonSecureChannel.ApiHmacKeyResolver = apiKeyResolver;
            responder.AppSecureChannels.Values.Each(sc => sc.ApiHmacKeyResolver = apiKeyResolver);
        }

        Timer reloadDelay;
        private void ReloadServices(FileSystemEventArgs fsea)
        {
            if (reloadDelay != null)
            {
                reloadDelay.Stop();
                reloadDelay.Dispose();
            }

            reloadDelay = new Timer(3000);
            reloadDelay.Elapsed += (o, args) =>
            {
                string path = fsea.FullPath;

                DirectoryInfo directory = GetDirectory(path);
                if (directory != null)
                {
                    TryReloadServices(directory);
                }
            };
            reloadDelay.AutoReset = false;
            reloadDelay.Enabled = true;            
        }

        private static DirectoryInfo GetDirectory(string path)
        {
            DirectoryInfo directory = null;
            if (File.Exists(path))
            {
                directory = new FileInfo(path).Directory;
            }
            else if (Directory.Exists(path))
            {
                directory = new DirectoryInfo(path);
            }
            return directory;
        }

        private void TryReloadServices(DirectoryInfo directory)
        {
            try
            {
                List<string> excludeNamespaces = new List<string>();
                excludeNamespaces.AddRange(DefaultConfiguration.GetAppSetting("ExcludeNamespaces").DelimitSplit(",", "|"));
                List<string> excludeClasses = new List<string>();
                excludeClasses.AddRange(DefaultConfiguration.GetAppSetting("ExcludeClasses").DelimitSplit(",", "|"));

                lock (_serviceTypeLock)
                {
                    DefaultConfiguration
                    .GetAppSetting("AssemblySearchPattern")
                    .Or("*Services.dll,*Proxyables.dll")
                    .DelimitSplit(",", "|")
                    .Each(new { Directory = directory, ExcludeNamespaces = excludeNamespaces, ExcludeClasses = excludeClasses },
                    (ctx, searchPattern) =>
                    {
                        FileInfo[] files = ctx.Directory.GetFiles(searchPattern, SearchOption.AllDirectories);
                        foreach (FileInfo file in files)
                        {
                            try
                            {
                                Assembly toLoad = Assembly.LoadFrom(file.FullName);
                                Type[] types = toLoad.GetTypes().Where(type => !ctx.ExcludeNamespaces.Contains(type.Namespace) &&
                                        !ctx.ExcludeClasses.Contains(type.Name) &&
                                        type.HasCustomAttributeOfType<ProxyAttribute>()).ToArray();
                                foreach (Type t in types)
                                {
                                    ServiceTypes.Add(t);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.AddEntry("An exception occurred loading services from file {0}: {1}", LogEventType.Warning, ex, file.FullName, ex.Message);
                            }
                        }
                    });
                }
                RegisterServiceTypes();
            }
            catch (Exception ex)
            {
                Logger.AddEntry("An exception occurred loading services: {0}", ex, ex.Message);
            }
        }                        
    }
}
