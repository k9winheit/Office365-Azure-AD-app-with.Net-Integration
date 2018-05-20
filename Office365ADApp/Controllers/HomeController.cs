using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Office365.Discovery;
using Microsoft.Office365.OutlookServices;
using System.Threading.Tasks;
using Office365ADApp.Models;
using Office365ADApp.Utils;

namespace Office365ADApp.Controllers
{
   [AllowAnonymous]
    [Authorize]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
	        {
                ViewBag.Name = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value;
                ViewBag.ObjectId = ClaimsPrincipal.Current. FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                ViewBag.Provider = ClaimsPrincipal.Current.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name").Value;
                ViewBag.GivenName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.GivenName).Value;
                ViewBag.FullName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.GivenName).Value + " " + ClaimsPrincipal.Current.FindFirst(ClaimTypes.Surname).Value;

               // RedirectToAction("whateverAction", "whateverController");
            }
           
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public async Task<ActionResult> Contact()
        {
            List<MyContact> myContacts = new List<MyContact>();

            var signInUserId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            var userObjectId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

            AuthenticationContext authContext = new AuthenticationContext(SettingsHelper.Authority, new ADALTokenCache(signInUserId));

            try
            {
                DiscoveryClient discClient = new DiscoveryClient(SettingsHelper.DiscoveryServiceEndpointUri,
                    async () =>
                    {
                        var authResult = await authContext.AcquireTokenSilentAsync(SettingsHelper.DiscoveryServiceResourceId, new ClientCredential(SettingsHelper.ClientId, SettingsHelper.AppKey), new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));

                        return authResult.AccessToken;
                    });

                var dcr = await discClient.DiscoverCapabilityAsync("Contacts");

                OutlookServicesClient exClient = new OutlookServicesClient(dcr.ServiceEndpointUri,
                    async () =>
                    {
                        var authResult = await authContext.AcquireTokenSilentAsync(dcr.ServiceResourceId, new ClientCredential(SettingsHelper.ClientId, SettingsHelper.AppKey), new UserIdentifier(userObjectId, UserIdentifierType.UniqueId));

                        return authResult.AccessToken;
                    });

                var contactsResult = await exClient.Me.Contacts.ExecuteAsync();
                var contactsResult1 = await exClient.Me.ExecuteAsync();
                var sr = contactsResult1.DisplayName;
                do
                {
                    var contacts = contactsResult.CurrentPage;
                    foreach (var contact in contacts)
                    {
                        myContacts.Add(new MyContact { Name = contact.DisplayName });
                    }

                    contactsResult = await contactsResult.GetNextPageAsync();

                } while (contactsResult != null);
            }
            catch (AdalException exception)
            {
                //handle token acquisition failure
                if (exception.ErrorCode == AdalError.FailedToAcquireTokenSilently)
                {
                    authContext.TokenCache.Clear();

                    //handle token acquisition failure
                }
            }

            return View(myContacts);
        }
    }
}