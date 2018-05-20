using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Office365ADApp.Startup))]
namespace Office365ADApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
