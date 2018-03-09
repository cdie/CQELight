﻿using Autofac;
using CQELight.Abstractions.IoC.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace CQELight.IoC.Autofac
{

    class AutofacScopeFactory : IScopeFactory
    {

        #region Members

        readonly IContainer _container;

        #endregion

        #region Ctor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="autofacContainer">Autofac container.</param>
        public AutofacScopeFactory(IContainer autofacContainer)
        {
            _container = autofacContainer ?? throw new ArgumentNullException(nameof(autofacContainer),
                "AutofacScopeFactory.ctor() : Autofac container should be provided.");
        }

        #endregion

        #region IScopeFactory methods

        /// <summary>
        /// Create a new scope.
        /// </summary>
        /// <returns>New instance of scope.</returns>
        public IScope CreateScope()
        {
            Action<ContainerBuilder> autoRegisterAction = s => s.RegisterModule<AutoRegisterModule>();
            return new AutofacScope(_container.BeginLifetimeScope(autoRegisterAction));
        }

        #endregion

    }
}
