// Copyright (c) 2025 Jordan Sterling Farr
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging; 
using Nucleus.Abstractions.Adapters.Local; 
using System;

namespace Nucleus.Infrastructure.Adapters.Local
{
    /// <summary>
    /// Provides extension methods for setting up local adapter services in an <see cref="IServiceCollection"/>.
    /// </summary>
    /// <remarks>
    /// This allows for easy registration of the <see cref="LocalAdapter"/> and its dependencies.
    /// See <seealso cref="../../../../../Docs/Architecture/ClientAdapters/ARCHITECTURE_ADAPTERS_LOCAL.md"/> for architectural details.
    /// </remarks>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="ILocalAdapterClient"/> and its implementation <see cref="LocalAdapter"/>
        /// to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is null.</exception>
        public static IServiceCollection AddLocalAdapterServices(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Register the LocalAdapter and its interface.
            // The LocalAdapter currently takes IServiceProvider, which is generally available.
            // If LocalAdapter's dependencies change, this registration might need adjustment
            // (e.g., if it requires an HttpClientFactory or other specific services).
            services.AddScoped<ILocalAdapterClient, LocalAdapter>();

            return services;
        }
    }
}
