using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace StockTraderBroker.Authorization
{
    public class HasScopeHandler : AuthorizationHandler<HasScopeRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HasScopeRequirement requirement)
        {
            // If user does not have the scope claim, get out of here
            if (!context.User.HasClaim(c => c.Type == "scope" && c.Issuer == requirement.Issuer))
                return Task.CompletedTask;

            // Split the scopes string into an array
            var scopes = context.User.FindAll(c => c.Type == "scope" && c.Issuer == requirement.Issuer).ToList();

            // Succeed if the scope array contains the required scope
            if (!string.IsNullOrEmpty(scopes.Find(s => s.Value == requirement.Scope)?.Value))
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
