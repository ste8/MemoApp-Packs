using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ImagesGenerator.Configuration;

namespace ImagesGenerator.Services;

public interface IRetryPolicyService
{
    Task<T?> ExecuteAsync<T>(
        Func<Task<T?>> operation,
        string operationName,
        CancellationToken cancellationToken = default) where T : class;
}

public class RetryPolicyService : IRetryPolicyService
{
    private readonly ILogger<RetryPolicyService> _logger;
    private readonly ImageGenerationSettings _settings;
    
    public RetryPolicyService(
        IOptions<ImageGenerationSettings> settings,
        ILogger<RetryPolicyService> logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<T?> ExecuteAsync<T>(
        Func<Task<T?>> operation,
        string operationName,
        CancellationToken cancellationToken = default) where T : class
    {
        for (int attempt = 1; attempt <= _settings.MaxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (IsRetriableError(ex) && attempt < _settings.MaxRetries)
            {
                var delay = CalculateDelay(attempt, ex);
                _logger.LogWarning(
                    "Retry attempt {Attempt} for {Operation} after {DelaySeconds}s delay. Error: {Error}",
                    attempt, operationName, delay.TotalSeconds, ex.Message);
                
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex) when (attempt == _settings.MaxRetries)
            {
                _logger.LogError(ex, "Final attempt failed for {Operation}. Error type: {ExceptionType}", 
                    operationName, ex.GetType().Name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Non-retriable error for {Operation}. Error: {Error}", 
                    operationName, ex.Message);
                throw;
            }
        }
        
        return null;
    }
    
    private TimeSpan CalculateDelay(int attempt, Exception exception)
    {
        var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1) * _settings.BaseDelaySeconds);
        
        // Check for rate limiting and increase delay
        if (IsRateLimitError(exception))
        {
            baseDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt) * 2 * _settings.BaseDelaySeconds);
        }
        
        return baseDelay;
    }
    
    private static bool IsRetriableError(Exception ex)
    {
        var message = ex.Message.ToLower();
        
        // Don't retry DNS/network resolution errors
        if (message.Contains("nodename nor servname") || 
            message.Contains("no such host") ||
            message.Contains("name resolution"))
        {
            return false;
        }
        
        return message.Contains("429") || 
               message.Contains("rate") ||
               message.Contains("500") ||
               message.Contains("502") ||
               message.Contains("503") ||
               message.Contains("504") ||
               message.Contains("timeout") ||
               message.Contains("temporarily");
    }
    
    private static bool IsRateLimitError(Exception ex)
    {
        var message = ex.Message.ToLower();
        return message.Contains("429") || message.Contains("rate");
    }
}