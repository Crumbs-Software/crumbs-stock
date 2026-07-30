using System;
using System.Security.Claims;
using System.Collections.Generic;

namespace crumbs.stock.Extensions
{
    /// <summary>
    /// Extensions for extracting user information from ClaimsPrincipal.
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal claimsPrincipal)
        {
            var claim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _ = claim ?? throw new KeyNotFoundException("Claim NameIdentifier not found");
            return new Guid(claim);
        }

        public static Guid GetSubscriptionId(this ClaimsPrincipal claimsPrincipal)
        {
            var claim = claimsPrincipal.FindFirst("SubscriptionId")?.Value;
            _ = claim ?? throw new KeyNotFoundException("Claim SubscriptionId not found");
            return new Guid(claim);
        }
    }
}
