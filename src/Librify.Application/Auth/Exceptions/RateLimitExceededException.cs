using Librify.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Librify.Application.Auth.Exceptions;

public class RateLimitExceededException()
    : AppException("Too many failed login attempts. Try again later.", StatusCodes.Status429TooManyRequests);
