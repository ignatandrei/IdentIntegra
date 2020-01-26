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