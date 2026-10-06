using Blackjack.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Blackjack.Api;

// Turns expected errors into tidy HTTP responses (ProblemDetails JSON) instead of a 500.
public class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    private const int SqlUniqueIndexViolation = 2601;
    private const int SqlUniqueConstraintViolation = 2627;

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        (int Status, string Detail)? error = exception switch
        {
            ApiException api => (api.StatusCode, api.Message),

            // Another request changed the same row first (see Wallet.RowVersion).
            DbUpdateConcurrencyException =>
                (StatusCodes.Status409Conflict, "Something changed at the same time. Please refresh and try again."),

            // A unique index said no, e.g. a second active round or a taken name.
            DbUpdateException { InnerException: SqlException { Number: SqlUniqueIndexViolation or SqlUniqueConstraintViolation } } =>
                (StatusCodes.Status409Conflict, "That clashes with something that already exists. Please refresh and try again."),

            _ => null
        };

        if (error is null)
        {
            return false;
        }

        context.Response.StatusCode = error.Value.Status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = { Status = error.Value.Status, Detail = error.Value.Detail }
        });
    }
}
