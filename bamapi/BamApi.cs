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
        /// Creates a BamApiServer set to listen on the specified binding.  The default binding is for http://localhost:8080 if none is specified.
        /// </summary>
        /// <returns></returns>
        public static async Task<BamApiServer> CreateApiServer(HostBinding hostBinding = null)
        {
            hostBinding = hostBinding ?? new HostBinding();
            return await CreateManagedServerAsync(() => new BamApiServer(new HostBinding()));
        }

        public static async Task<T> GetProxyAsync<T>()
        {
            ProxyFactory factory = new ProxyFactory();
            return await Task.Run(() => factory.GetProxy<T>());
        }

        public static async Task<T> GetProxyAsync<T>(HostBinding hostBinding)
        {
            return await GetProxyAsync<T>(hostBinding.HostName, hostBinding.Port, Log.Default);
        }

        public static async Task<T> GetProxyAsync<T>(string hostName, int port = 80, ILogger logger = null)
        {
            ProxyFactory factory = new ProxyFactory();
            return await Task.Run(() => factory.GetProxy<T>(hostName, port, logger));
        }

        public static async Task<ApiServiceInfo> ServeRegistries(params string[] registryNames)
        {
            return await ServeRegistriesAsync(GetLocalServiceRegistryService, registryNames);
        }

        public static async Task<ApiServiceInfo> ServeRegistriesAsync(Func<ServiceRegistryService> serviceRegistryServiceProvider, params string[] registryNames)
        {
            return await ServeRegistriesAsync(serviceRegistryServiceProvider(), registryNames);
        }

        public static async Task<ApiServiceInfo> ServeRegistriesAsync(ServiceRegistryService serviceRegistryService, string[] registryNames)
        {
            HashSet<Type> serviceTypes = new HashSet<Type>();
            ServiceRegistry dependencyRegistry = new ServiceRegistry();
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
            return await ServeServiceTypesAsync(hostBindings, dependencyRegistry, services);
        }

        public static async Task<ApiServiceInfo> ServeServiceTypesAsync(params Type[] serviceTypes)
        {
            return await ServeServiceTypesAsync(new HostBinding[] { new HostBinding()}, serviceTypes);
        }

        public static async Task<ApiServiceInfo> ServeServiceTypesAsync(HostBinding[] hostBindings, params Type[] serviceTypes)
        {
            return await ServeServiceTypesAsync(hostBindings, null, serviceTypes);
        }

        public static async Task<ApiServiceInfo> ServeServiceTypesAsync(HostBinding[] hostBindings, ServiceRegistry dependencyRegistry, params Type[] serviceTypes)
        {
            BamConf conf = BamConf.Load(BamHome.ContentPath);
            if (dependencyRegistry != null && ServiceRegistry.Default == null)
            {
                ServiceRegistry.Default = dependencyRegistry;
            }
            BamApiServer apiServer = await CreateManagedServerAsync<BamApiServer>(() => new BamApiServer(conf, Log.Default, ApiConf.Verbose)
            {
                HostBindings = new HashSet<HostBinding>(hostBindings),
                MonitorDirectories = new string[] { }
            });

            serviceTypes.Each(type=> apiServer.ServiceTypes.Add(type));
            apiServer.Start();
            return new ApiServiceInfo
            {
                Server = apiServer,
                DependencyRegistry = dependencyRegistry,
                ServiceTypes = serviceTypes,
                HostBindings = hostBindings,
                ApiConf = ApiConf
            };
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
