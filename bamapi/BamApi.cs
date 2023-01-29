using Bam.Application;
using Bam.Net;
using Bam.Net.CoreServices;
using Bam.Net.Server;
using Bam.Net.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Bam.Net.Application;
using Bam.Net.Services;

namespace Bam.Application
{
    public class BamApi : BamPlatform
    {
        static ApiConf _apiConf;
        static object _apiConfLock = new object();
        public static ApiConf ApiConf
        {
            get
            {
                return _apiConfLock.DoubleCheckLock(ref _apiConf, () => new ApiConf());
            }
            set
            {
                _apiConf = value;
            }
        }

        static BamApiServiceRegistry _bamApiServiceRegistry;
        static object _bamApiServiceRegistryLock = new object();
        public static BamApiServiceRegistry DependencyRegistry
        {
            get
            {
                return _bamApiServiceRegistryLock.DoubleCheckLock(ref _bamApiServiceRegistry, () => BamApiServiceRegistry.ForConfiguration(ApiConf ?? new ApiConf()));
            }
        }

        static ServiceRegistryService _serviceRegistryService;
        static object _serviceRegistryServiceLock = new object();
        public static ServiceRegistryService GetLocalServiceRegistryService()
        {
            return _serviceRegistryServiceLock.DoubleCheckLock(ref _serviceRegistryService, () => ServiceRegistryService.GetLocalServiceRegistryService(DependencyRegistry));
        }

        /// <summary>
        /// Create a BamApiServer that listens for requests to "localhost" on a random port from 8080 to 65535.
        /// </summary>
        /// <returns>BamApiServer</returns>
        public static async Task<BamApiServer> CreateApiServerAsync(params HostBinding[] hostBindings)
        {
            hostBindings = hostBindings ?? new HostBinding[] { new HostBinding(RandomNumber.Between(8080, 65535)) };
            return await CreateManagedServerAsync(() => new BamApiServer(hostBindings));
        }

        /// <summary>
        /// Get a proxy instance using locally available
        /// assemblies.
        /// </summary>
        /// <typeparam name="T">The type of the instance that is returned.</typeparam>
        /// <returns>A proxy instance of T.</returns>
        public static async Task<T> GetClientAsync<T>()
        {
            ProxyFactory factory = new ProxyFactory();
            return await Task.Run(() => factory.GetProxy<T>());
        }

        /// <summary>
        /// Get a proxy instance; if not already done, the assembly is acquired by downloading and compiling the source from the specified host. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hostBinding"></param>
        /// <returns></returns>
        public static async Task<T> GetClientAsync<T>(HostBinding hostBinding)
        {
            return await GetClientAsync<T>(hostBinding.HostName, hostBinding.Port, Log.Default);
        }

        public static async Task<T> GetClientAsync<T>(string hostName, int port = 80, ILogger logger = null)
        {
            ProxyFactory factory = new ProxyFactory();
            return await Task.Run(() => factory.GetProxy<T>(hostName, port, logger));
        }

        public static async Task<BamApiServiceContext> CreateApiServiceContextAsync(params string[] registryNames)
        {
            return await CreateApiServiceContextAsync(GetLocalServiceRegistryService, registryNames);
        }

        public static async Task<BamApiServiceContext> CreateApiServiceContextAsync(Func<ServiceRegistryService> serviceRegistryServiceProvider, params string[] registryNames)
        {
            return await CreateApiServiceContextAsync(serviceRegistryServiceProvider(), registryNames);
        }

        /// <summary>
        /// Serve the specified registry names from the specified ServiceRegistryService.
        /// </summary>
        /// <param name="serviceRegistryService"></param>
        /// <param name="registryNames"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static async Task<BamApiServiceContext> CreateApiServiceContextAsync(ServiceRegistryService serviceRegistryService, string[] registryNames)
        {
            HashSet<Type> serviceTypes = new HashSet<Type>();
            ApplicationServiceRegistry dependencyRegistry = ApplicationServiceRegistry.ForProcess();
            foreach (string registryName in registryNames)
            {
                ServiceRegistry registry = serviceRegistryService.GetServiceRegistry(registryName);
                foreach (string className in registry.ClassNames)
                {
                    registry.Get(className, out Type type);
                    if (type.HasCustomAttributeOfType<ProxyAttribute>())
                    {
                        serviceTypes.Add(type);
                    }
                }
                dependencyRegistry.CombineWith(registry);
            }
            Type[] services = serviceTypes.ToArray();
            if (services.Length == 0)
            {
                throw new ArgumentException("No services were loaded");
            }

            HostBinding[] hostBindings = ServiceConfig.GetConfiguredHostBindings();
            return await CreateApiServiceContextAsync(hostBindings, dependencyRegistry, services);
        }

        public static async Task<BamApiContext> StartAsync(BamApiOptions options)
        {
            BamApiServiceRegistry bamApiServiceRegistry = BamApiServiceRegistry.Create(options);
            object[] services = options.ConfigureServices(bamApiServiceRegistry);
            services.Each(svc => bamApiServiceRegistry.Set(svc.GetType(), svc));
            BamApiServiceContext bamApiServiceContext = await CreateApiServiceContextAsync(options.HostBindings, bamApiServiceRegistry, options.ServiceTypes.ToArray());
            BamApiContext bamApiContext =  new BamApiContext(bamApiServiceContext);
            bamApiContext.Start();
            return bamApiContext;
        }

        public static async Task<BamApiServiceContext> CreateApiServiceContextAsync(string serverName, Func<ApplicationServiceRegistry, ApplicationServiceRegistry> configureDependencyRegistry, params Type[] serviceTypes)
        {
            return await CreateApiServiceContextAsync
            (
                serverName, 
                configureDependencyRegistry, 
                new ApiConf(), 
                serviceTypes
            );
        }

        public static async Task<BamApiServiceContext> CreateApiServiceContextAsync(string serverName, Func<ApplicationServiceRegistry, ApplicationServiceRegistry> configureDepencyRegistry, ApiConf apiConf, params Type[] serviceTypes)
        {
            return await CreateApiServiceContextAsync
            (
                new HostBinding[] { new ManagedServerHostBinding(serverName) },
                configureDepencyRegistry(BamApiServiceRegistry.ForConfiguration(apiConf)), 
                serviceTypes
            );
        }

        public static async Task<BamApiServiceContext> CreateApiServiceContextAsync(HostBinding[] hostBindings, params Type[] serviceTypes)
        {
            return await CreateApiServiceContextAsync(hostBindings, null, serviceTypes);
        }

        public static async Task<BamApiServiceContext> CreateApiServiceContextAsync(HostBinding[] hostBindings, ApplicationServiceRegistry dependencyRegistry, params Type[] serviceTypes)
        {
            BamConf conf = BamConf.Load(BamHome.ContentPath);
            if (dependencyRegistry != null && ServiceRegistry.Default == null)
            {
                ServiceRegistry.Default = dependencyRegistry;
            }
            BamApiServer apiServer = await CreateManagedServerAsync(() => new BamApiServer(conf, Log.Default, ApiConf.Verbose)
            {
                DependencyRegistry = dependencyRegistry,
                HostBindings = new HashSet<HostBinding>(hostBindings),
                MonitorDirectories = new string[] { }
            });
            serviceTypes.Each(svc => apiServer.ServiceTypes.Add(svc));

            BamApiServiceContext bamApiServiceContext = new BamApiServiceContext
            {
                Server = apiServer,
                ServiceTypes = serviceTypes,
                HostBindings = hostBindings,
                ApiConf = ApiConf
            };

            return bamApiServiceContext;
        }

        public static Task<Assembly> CompileBamApiServiceSource(string bamSvcDirectoryPath, string extension)
        {
            return Task.Run(() => 
            {
                string binPath = Path.Combine(BamHome.ToolkitPath, $"{nameof(BamApi)}_bin");
                DirectoryInfo bamSvcSrcDirectory = new DirectoryInfo(bamSvcDirectoryPath.Or(BamHome.SvcScriptsSrcPath)).EnsureExists();
                DirectoryInfo bamSvcBnDirectory = new DirectoryInfo(binPath).EnsureExists();
                FileInfo[] files = bamSvcSrcDirectory.GetFiles($"*.{extension}", SearchOption.AllDirectories);
                StringBuilder src = new StringBuilder();
                foreach (FileInfo file in files)
                {
                    src.AppendLine(file.ReadAllText());
                }
                string hash = src.ToString().Sha1();
                string assemblyName = $"{hash}.dll";
                string svcAssemblyBinPath = Path.Combine(bamSvcBnDirectory.FullName, assemblyName);
                Assembly bamSvcAssembly;
                if (!File.Exists(svcAssemblyBinPath))
                {
                    bamSvcAssembly = files.ToAssembly(assemblyName);
                    FileInfo assemblyFile = bamSvcAssembly.GetFileInfo();
                    FileInfo targetPath = new FileInfo(svcAssemblyBinPath);
                    if (!targetPath.Directory.Exists)
                    {
                        targetPath.Directory.Create();
                    }
                    File.Copy(assemblyFile.FullName, svcAssemblyBinPath);
                }
                else
                {
                    bamSvcAssembly = Assembly.LoadFile(svcAssemblyBinPath);
                }

                return bamSvcAssembly;
            });
        }
    }
}
