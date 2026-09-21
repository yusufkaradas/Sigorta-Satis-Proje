namespace Kasko.DataAccess.Auditing;

public interface ICurrentUserProvider
{
    string? GetCurrentUser();
}
