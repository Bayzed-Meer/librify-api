using Librify.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Librify.Application.Auth.Exceptions;

public class InvalidCredentialsException()
    : AppException("Invalid credentials.", StatusCodes.Status401Unauthorized);
