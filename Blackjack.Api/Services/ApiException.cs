namespace Blackjack.Api.Services;

// A problem caused by the request (bad bet, wrong round, ...). ApiExceptionHandler turns it
// into an HTTP error response with this status code and message.
public class ApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public static ApiException BadRequest(string message) => new(StatusCodes.Status400BadRequest, message);

    public static ApiException NotFound(string message) => new(StatusCodes.Status404NotFound, message);

    public static ApiException Conflict(string message) => new(StatusCodes.Status409Conflict, message);
}
