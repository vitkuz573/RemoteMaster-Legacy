using Microsoft.AspNetCore.SignalR.Client;

namespace RemoteMaster.Client;

public class RetryPolicy : IRetryPolicy
{
    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        return TimeSpan.FromSeconds(3);
    }
}