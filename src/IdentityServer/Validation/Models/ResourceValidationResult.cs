// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Result of validation of requested scopes and resource indicators.
    /// </summary>
    public class ResourceValidationResult
    {
        /// <summary>
        /// Ctor
        /// </summary>
        public ResourceValidationResult()
        {
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="resources"></param>
        public ResourceValidationResult(Resources resources)
        {
            Resources = resources;
            ParsedScopes = resources.ToScopeNames().Select(x => new ParsedScopeValue(x)).ToList();
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="parsedScopeValues"></param>
        public ResourceValidationResult(Resources resources, IEnumerable<ParsedScopeValue> parsedScopeValues)
        {
            Resources = resources;
            ParsedScopes = parsedScopeValues.ToList();
        }

        /// <summary>
        /// Indicates if the result was successful.
        /// </summary>
        public bool Succeeded => ParsedScopes.Any() && !InvalidScopes.Any() && !InvalidResourceIndicators.Any();

        /// <summary>
        /// The resources of the result.
        /// </summary>
        public Resources Resources { get; set; } = new Resources();

        /// <summary>
        /// The parsed scopes represented by the result.
        /// </summary>
        public ICollection<ParsedScopeValue> ParsedScopes { get; set; } = new HashSet<ParsedScopeValue>();
        
        /// <summary>
        /// The original (raw) scope values represented by the validated result.
        /// </summary>
        public IEnumerable<string> RawScopeValues => ParsedScopes.Select(x => x.RawValue);

        /// <summary>
        /// The requested resource indicators that are invalid.
        /// </summary>
        public ICollection<string> InvalidResourceIndicators { get; set; } = new HashSet<string>();

        /// <summary>
        /// The requested scopes that are invalid.
        /// </summary>
        public ICollection<string> InvalidScopes { get; set; } = new HashSet<string>();

        /// <summary>
        /// Returns new result filted by the scope values.
        /// </summary>
        /// <param name="scopeValues"></param>
        /// <returns></returns>
        public ResourceValidationResult Filter(IEnumerable<string> scopeValues)
        {
            scopeValues ??= Enumerable.Empty<string>();

            var offline = scopeValues.Contains(IdentityServerConstants.StandardScopes.OfflineAccess);

            var parsedScopesToKeep = ParsedScopes.Where(x => scopeValues.Contains(x.RawValue)).ToArray();
            var parsedScopeNamesToKeep = parsedScopesToKeep.Select(x => x.ParsedName).ToArray();

            var identityToKeep = Resources.IdentityResources.Where(x => parsedScopeNamesToKeep.Contains(x.Name));
            var apiScopesToKeep = Resources.ApiScopes.Where(x => parsedScopeNamesToKeep.Contains(x.Name));

            var apiScopesNamesToKeep = apiScopesToKeep.Select(x => x.Name).ToArray();
            var apiResourcesToKeep = Resources.ApiResources.Where(x => x.Scopes.Any(y => apiScopesNamesToKeep.Contains(y)));

            var resources = new Resources(identityToKeep, apiResourcesToKeep, apiScopesToKeep)
            {
                OfflineAccess = offline
            };

            return new ResourceValidationResult(resources, parsedScopesToKeep);
        }

        /// <summary>
        /// Filters the result by the resource indicator for issuing access tokens.
        /// </summary>
        public ResourceValidationResult FilterByResourceIndicator(string resourceIndicator)
        {
            var apiResourcesToKeep = String.IsNullOrWhiteSpace(resourceIndicator) ?
                Resources.ApiResources.Where(x => !x.RequireResourceIndicator) :
                Resources.ApiResources.Where(x => x.Name == resourceIndicator);

            var scopeNamesToKeep = apiResourcesToKeep.SelectMany(x => x.Scopes);
            var apiScopesToKeep = Resources.ApiScopes.Where(x => scopeNamesToKeep.Contains(x.Name));

            if (resourceIndicator == null)
            {
                // no resource indicator, so we also keep identity scopes (e.g. since we need those for userinfo)
                scopeNamesToKeep = scopeNamesToKeep.Union(Resources.IdentityResources.Select(x => x.Name));
            }

            var parsedScopesToKeep = ParsedScopes.Where(x => scopeNamesToKeep.Contains(x.ParsedName)).ToList();
            if (Resources.OfflineAccess)
            {
                parsedScopesToKeep.Add(new ParsedScopeValue(IdentityServerConstants.StandardScopes.OfflineAccess));
            }

            var resources = new Resources(Resources.IdentityResources, apiResourcesToKeep, apiScopesToKeep)
            {
                OfflineAccess = Resources.OfflineAccess,
            };

            return new ResourceValidationResult()
            {
                Resources = resources,
                ParsedScopes = parsedScopesToKeep
            };
        }
    }
}
