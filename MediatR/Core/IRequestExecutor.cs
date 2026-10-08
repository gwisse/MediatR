namespace MediatR.Core;

internal interface IRequestExecutor
{
    Task<object?> Execute(object request, CancellationToken cancellationToken);
}