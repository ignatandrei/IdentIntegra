using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IdentityServer4.Quickstart.UI;
using Microsoft.AspNetCore.Identity;
using StsServer.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using IdentityServer4.Events;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using IdentityModel;

namespace StsServer.Quickstart.Account
{
    public class TestController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IEventService _events;

        public TestController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _interaction = interaction;
            _clientStore = clientStore;
            _schemeProvider = schemeProvider;
            _events = events;
        }
        public async Task<bool> AuthorizeBlind()
        {
            //TODO: 
            //in browser call https://localhost:44364/test/authorizeblind
            //then call https://localhost:44364/
            #region initialize user, claims
            string provider = "Windows";
            string providerUserId = "Andrei";//does not matter
            var localSignInProps = new AuthenticationProperties();//see what you need

            //copied after AutoProvisionUserAsync
            var user = new ApplicationUser
            {
                UserName = Guid.NewGuid().ToString(),
            };
            var filtered = new List<Claim>(){
                new Claim(JwtClaimTypes.Name, "Radu Ignat"),
                new Claim(JwtClaimTypes.Email, "asd@asd.com")
            };
            //TODO: add any other claims
            #endregion

            var identityResult = await _userManager.CreateAsync(user);
            if (!identityResult.Succeeded) throw new Exception(identityResult.Errors.First().Description);

            identityResult = await _userManager.AddClaimsAsync(user, filtered);
            if (!identityResult.Succeeded) throw new Exception(identityResult.Errors.First().Description);

            identityResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerUserId, provider));
            if (!identityResult.Succeeded) throw new Exception(identityResult.Errors.First().Description);

            // issue authentication cookie for user
            // we must issue the cookie maually, and can't use the SignInManager because
            // it doesn't expose an API to issue additional claims from the login workflow
            var principal = await _signInManager.CreateUserPrincipalAsync(user);

            var name = principal.FindFirst(JwtClaimTypes.Name)?.Value ?? user.Id;
            await _events.RaiseAsync(new UserLoginSuccessEvent(provider, providerUserId, user.Id, name));
            await HttpContext.SignInAsync(user.Id, name, provider, localSignInProps, filtered.ToArray());
            
            return true;
        }
        public async Task<bool> AuthorizeDB(LoginInputModel model)
        {
            if (!ModelState.IsValid)
                return false;

            var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberLogin, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(model.Username);
                await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName));
            }
            return result.Succeeded;
        }
        [Authorize(Policy="MyGroupPolicy")]
        public IActionResult Index()
        {
            //this.User.Claims.First(it=>it.Value == "views").pr
            return Content(" you can see this because you are authorized to see MyGroupPolicy");
        }

        [Authorize(Policy = "AzurePOL")]
        public IActionResult Index1()
        {
            //this.User.Claims.First(it=>it.Value == "views").pr
            return Content(" azure policy enabled");
        }
    }
}