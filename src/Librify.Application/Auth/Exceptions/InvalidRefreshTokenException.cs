using Librify.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Librify.Application.Auth.Exceptions;

public class InvalidRefreshTokenException()
    : AppException("Invalid or expired refresh token.", StatusCodes.Status401Unauthorized);
