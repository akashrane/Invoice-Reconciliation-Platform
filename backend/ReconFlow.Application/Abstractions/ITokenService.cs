using ReconFlow.Core.Entities;

namespace ReconFlow.Application.Abstractions;

public interface ITokenService
{
    string Create(User user);
}
