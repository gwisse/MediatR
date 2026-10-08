namespace MediatR.Abstractions;

/// <summary>
/// Represents the next delegate in a request pipeline.
/// </summary>
/// <typeparam name="TResponse">The type of response produced by the pipeline.</typeparam>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();