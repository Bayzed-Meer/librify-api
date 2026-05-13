using Librify.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Librify.Application.Auth.Exceptions;

public class DuplicateEmailException(string email)
    : AppException($"Email '{email}' is already in use.", StatusCodes.Status409Conflict)
{
    public string Email { get; } = email;
}
