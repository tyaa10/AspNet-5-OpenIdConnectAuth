#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using OpenIdConnectAuth.Data;

namespace OpenIdConnectAuth.Services
{
    public class UserService
    {
        private readonly AuthDbContext _context;
        public UserService(AuthDbContext context)
        {
            _context = context;
        }

        public AppUser? GetUserByExternalProvider(string provider, string nameIdentifier)
        {
            var appUser = _context.AppUsers
                .Where(a => a.Provider == provider)
                .FirstOrDefault(a => a.NameIdentifier == nameIdentifier);
            return appUser;
        }

        public AppUser GetUserById(int id)
        {
            var appUser = _context.AppUsers.Find(id);
            return appUser;
        }

        public bool TryValidateUser(string username, string password, out List<Claim> claims)
        {
            claims = new List<Claim>();
            var appUser = _context.AppUsers
                .Where(a => a.Username == username).FirstOrDefault(a => a.Password == password);
            if (appUser is null)
            {
                return false;
            } else
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, username));
                claims.Add(new Claim("username", username));
                claims.Add(new Claim(ClaimTypes.GivenName, appUser.Firstname));
                claims.Add(new Claim(ClaimTypes.Surname, appUser.Lastname));
                claims.Add(new Claim(ClaimTypes.Email, appUser.Email));
                claims.Add(new Claim(ClaimTypes.MobilePhone, appUser.Mobile));
                foreach (var r in appUser.RoleList)
                {
                    claims.Add(new Claim(ClaimTypes.Role, r));
                }
                return true;
            }
        }

        public AppUser AddNewUser(string provider, List<Claim> claims)
        {
            var appUser = new AppUser();
            appUser.Provider = provider;
            appUser.NameIdentifier = claims.GetClaim(ClaimTypes.NameIdentifier);  
            appUser.Username = claims.GetClaim("username");
            appUser.Firstname = claims.GetClaim(ClaimTypes.GivenName);
            appUser.Lastname = claims.GetClaim(ClaimTypes.Surname);
            var name = claims.GetClaim("name");
            if (string.IsNullOrEmpty(appUser.Firstname))
            {
                appUser.Firstname = name?.Split(' ').First();
            }
            if (string.IsNullOrEmpty(appUser.Lastname))
            {
                var nameSplit = name?.Split(' ');
                if (nameSplit.Length > 1)
                {
                    appUser.Lastname = name?.Split(' ').Last();
                }
            }
            appUser.Email = claims.GetClaim(ClaimTypes.Email);
            appUser.Mobile = claims.GetClaim(ClaimTypes.MobilePhone);
            appUser.Roles = "NewUser";
            var entity = _context.AppUsers.Add(appUser);
            _context.SaveChanges();
            return entity.Entity;
         }
    }

    public static class Extensions
    {
        public static string? GetClaim(this List<Claim> claims, string name)
        {
            return claims.FirstOrDefault(c => c.Type == name)?.Value;
        }
    }
}