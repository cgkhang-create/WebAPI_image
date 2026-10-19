using Microsoft.AspNetCore.Identity;

namespace WebAPI_image.Repositories
{
    public interface ITokenRepository
    {
        string CreateJWTToken(IdentityUser user, List<string> roles);
    }
}