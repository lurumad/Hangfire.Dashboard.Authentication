using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace HangfireDashboardIdentityServer4
{
    public class DashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize([NotNull] DashboardContext context)
        {
            return context.GetHttpContext().User.Identity.IsAuthenticated;
        }
    }
}
