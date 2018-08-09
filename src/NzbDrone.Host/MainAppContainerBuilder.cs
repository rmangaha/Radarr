﻿using System.Collections.Generic;
using Nancy.Bootstrapper;
using NzbDrone.Api;
using NzbDrone.Common.Composition;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http.Dispatchers;
using NzbDrone.SignalR;

namespace Radarr.Host
{
    public class MainAppContainerBuilder : ContainerBuilderBase
    {
        public static IContainer BuildContainer(StartupContext args)
        {
            var assemblies = new List<string>
                             {
                                 "Radarr.Host",
                                 "NzbDrone.Common",
                                 "NzbDrone.Core",
                                 "NzbDrone.Api",
                                 "NzbDrone.SignalR"
                             };

            if (OsInfo.IsWindows)
            {
                assemblies.Add("NzbDrone.Windows");
            }

            else
            {
                assemblies.Add("NzbDrone.Mono");
            }

            return new MainAppContainerBuilder(args, assemblies.ToArray()).Container;
        }

        private MainAppContainerBuilder(StartupContext args, string[] assemblies)
            : base(args, assemblies)
        {
            AutoRegisterImplementations<NzbDronePersistentConnection>();

            Container.Register<INancyBootstrapper, NancyBootstrapper>();
            Container.Register<IHttpDispatcher, FallbackHttpDispatcher>();
        }
    }
}