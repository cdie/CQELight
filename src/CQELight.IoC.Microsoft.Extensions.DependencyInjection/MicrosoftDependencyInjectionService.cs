﻿using System;

namespace CQELight.IoC.Microsoft.Extensions.DependencyInjection
{
    class MicrosoftDependencyInjectionService : IBootstrapperService
    {
        public BootstrapperServiceType ServiceType => BootstrapperServiceType.IoC;
        public Action<BootstrappingContext> BootstrappAction { get; internal set; }
    }
}
