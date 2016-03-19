using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(SalesforceIntegration.Startup))]
namespace SalesforceIntegration
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
