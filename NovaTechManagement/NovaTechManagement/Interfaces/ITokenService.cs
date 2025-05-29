using NovaTechManagement.Models;

namespace NovaTechManagement.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(User user);
    }
}
