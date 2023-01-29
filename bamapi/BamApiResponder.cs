using Bam.Net;
using Bam.Net.Logging;
using Bam.Net.Server;
using Bam.Net.Server.JsonRpc;
using Bam.Net.Server.Renderers;
using Bam.Net.ServiceProxy;
using System;
using System.Collections.Generic;

namespace Bam.Application
{
    public class BamApiResponder : HttpHeaderResponder, IInitialize<BamApiResponder>
    {
        readonly Dictionary<string, IHttpResponder> _responders;

        public BamApiResponder() : base(new BamConf(), Log.Default)
        { }

        public BamApiResponder(BamConf conf, ILogger logger, bool verbose = false)
            : base(conf, logger)
        {
            RendererFactory = new WebRendererFactory(logger);
            ServiceProxyResponder = new ServiceProxyResponder(conf, logger);
            RpcResponder = new JsonRpcResponder(conf, logger);
            _responders = new Dictionary<string, IHttpResponder>
            {
                { ServiceProxyResponder.Name, ServiceProxyResponder },
                { RpcResponder.Name, RpcResponder }
            };
            if (verbose)
            {
                WireResponseLogging();
            }
        }

        public void WireResponseLogging()
        {
            WireResponseLogging(ServiceProxyResponder, Logger);
            WireResponseLogging(RpcResponder, Logger);
        }

        public ServiceProxyResponder ServiceProxyResponder
        {
            get;
            private set;
        }

        public JsonRpcResponder RpcResponder
        {
            get;
            private set;
        }

        public override bool Respond(IHttpContext context)
        {
            if (!TryRespond(context))
            {
                SendResponse(context, new HttpStatusCodeHandler { Code = 404, DefaultResponse = "Not Found" }, new { BamServer = "Bam Api Server" } );
            }
            context.Response.Close();
            return true;
        }

        public override bool TryRespond(IHttpContext context)
        {
            return TryRespond(context, out IHttpResponder _);
        }

        public bool TryRespond(IHttpContext context, out IHttpResponder responder)
        {
            try
            {
                string requestedResponder = GetRequestedResponderName(context).Or(ServiceProxyResponder.Name);
                responder = _responders[requestedResponder];
                return responder.TryRespond(context);
            }
            catch (Exception ex)
            {
                responder = null;
                Logger.AddEntry("Bam Rpc: exception occurred trying to respond, {0}", ex, ex.Message);
                return false;
            }
        }

        public event Action<BamApiResponder> Initializing;
        public event Action<BamApiResponder> Initialized;

        public override void Initialize()
        {
            OnInitializing();
            base.Initialize();
            OnInitialized();
        }
        protected void OnInitializing()
        {
            Initializing?.Invoke(this);
        }
        protected void OnInitialized()
        {
            Initialized?.Invoke(this);
        }
    }
}
