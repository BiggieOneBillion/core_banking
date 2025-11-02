using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;


namespace CoreBanking.Test.APP.Common.Behaviours;



public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling command {CommandName} with payload {@Payload}", requestName, request);

        var timer = Stopwatch.StartNew();
        var response = await next();
        timer.Stop();

        _logger.LogInformation("Command {CommandName} handled in {ElapsedMilliseconds}ms", requestName, timer.ElapsedMilliseconds);

        return response;
    }
}
