﻿using Bam.Net;
using Bam.Net.Application;
using Bam.Net.Configuration;
using Bam.Net.CoreServices;
using Bam.Net.Data.Repositories;
using Bam.Net.Logging;
using Bam.Net.ServiceProxy.Encryption;
using Bam.Net.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Application
{
    public class BamApiServiceRegistry : ApplicationServiceRegistry
    {
        public static BamApiServiceRegistry Create(BamApiOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            BamApiServiceRegistry bamApiServiceRegistry = ForConfiguration(options.ApiConf);
            options.ConfigureDependencies(bamApiServiceRegistry);
            return bamApiServiceRegistry;
        }

        [ServiceRegistryLoader]
        public static BamApiServiceRegistry ForConfiguration(ApiConf config)
        {
            BamApiServiceRegistry bamApiServiceRegistry = new BamApiServiceRegistry();
            bamApiServiceRegistry.CombineWith(Configure(appRegistry =>
            {
                ApiHmacKeyResolver hmacKeyResolver = new ApiHmacKeyResolver();
                appRegistry
                    .For<IApiHmacKeyProvider>().Use(hmacKeyResolver)
                    .For<IApiHmacKeyResolver>().Use(hmacKeyResolver)
                    .For<IApplicationNameProvider>().Use(DefaultConfigurationApplicationNameProvider.Instance)
                    .For<DataProvider>().Use(DataProvider.Current)
                    .For<ApiConf>().Use(config)
                    .For<ISecureChannelSessionDataManager>().Use<SecureChannelSessionDataManager>();
            }));

            return bamApiServiceRegistry;
        }
    }
}
