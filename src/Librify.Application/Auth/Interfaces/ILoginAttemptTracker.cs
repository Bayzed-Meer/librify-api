namespace Librify.Application.Auth.Interfaces;

public interface ILoginAttemptTracker
{
    bool IsBlocked(string email);
    void RecordFailure(string email);
    void Reset(string email);
}
