using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenIdConnectAuth.Models;
using OpenIdConnectAuth.Services;

namespace OpenIdConnectAuth.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserService _userService;

        public HomeController(ILogger<HomeController> logger, UserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        [Authorize(Roles = "Admin")]
        public IActionResult Secured()
        {
            return View();
        }
        [HttpGet("login")]
        public IActionResult Login(string returnUrl)
        {
            ViewData["returnUrl"] = returnUrl;
            return View();
        }
        [HttpGet("login/{provider}")]
        public IActionResult LoginExternal([FromRoute]string provider, [FromQuery]string returnUrl)
        {
            if (User != null && User.Identities.Any(identity => identity.IsAuthenticated))
            {
                RedirectToAction("", "Home");
            }
            returnUrl = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
            var authenticationProperties = new AuthenticationProperties {RedirectUri = returnUrl};
            return new ChallengeResult(provider, authenticationProperties);
        }
        [HttpPost("login")]
        public async Task<IActionResult> Validate(string username, string password, string returnUrl)
        {
            returnUrl = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
            ViewData["returnUrl"] = returnUrl;
            if (_userService.TryValidateUser(username, password, out List<Claim> claims))
            {
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                var items = new Dictionary<string, string>
                {
                    {".AuthScheme", CookieAuthenticationDefaults.AuthenticationScheme}
                };
                var properties = new AuthenticationProperties(items);
                await HttpContext.SignInAsync(claimsPrincipal, properties);
                return Redirect(returnUrl);
            }
            TempData["Error"] = "Error: username or password is invalid.";
            return View("login");
        }
        public async Task<IActionResult> Logout()
        {
            var scheme = User.Claims.FirstOrDefault(claim => claim.Type == ".AuthScheme")?.Value;
            if (scheme == "GoogleOpenID")
            {
                await HttpContext.SignOutAsync();
                return Redirect(@"https://accounts.google.com/Logout?&continue=https://appengine.google.com/_ah/logout?continue=https://localhost:5001");
            }
            return new SignOutResult(new[] {CookieAuthenticationDefaults.AuthenticationScheme, scheme});
        }
        [HttpGet("denied")]
        public IActionResult Denied()
        {
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}