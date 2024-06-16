using CleoAssignment.ApiService.Dto;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace CleoAssignment.ApiService;

public class ThrottlingApiServiceDecorator<T> : IApiService<T>
{
    private readonly IApiService<T> _innerService;
    private readonly ITimeProvider _timeProvider;
    private readonly ThrottleSettings _throttleSettings;
    private readonly ConcurrentDictionary<string, DateTime> _bannedIps = new ConcurrentDictionary<string, DateTime>();
    private readonly ConcurrentDictionary<string, (int count, DateTime startTime)> _requestCounts = new ConcurrentDictionary<string, (int count, DateTime startTime)>();

    public ThrottlingApiServiceDecorator(IApiService<T> innerService, ThrottleSettings throttleSettings, ITimeProvider timeProvider)
    {
        _innerService = innerService;
        _throttleSettings = throttleSettings;
        _timeProvider = timeProvider;
    }
    
    private async Task<TResult> ThrottleAsync<TResult>(Func<Task<TResult>> action,string ipAddress)
    {
        var now = _timeProvider.UtcNow;

        if (_bannedIps.TryGetValue(ipAddress, out var banExpiration))
        {
            if (now < banExpiration)
            {
                return (TResult)Activator.CreateInstance(typeof(TResult),false, ErrorType.IpBanned);
            }
            else
            {
                _bannedIps.TryRemove(ipAddress, out _);
            }
        }

        var (count, startTime) = _requestCounts.GetOrAdd(ipAddress, (0, now));
        
        if ((now - startTime) > _throttleSettings.ThrottleInterval)
        {
            _requestCounts[ipAddress] = (1, now);
        }
        else
        {
            count++;
            if (count > _throttleSettings.MaxRequestsPerIp)
            {
                _bannedIps[ipAddress] = now + _throttleSettings.BanTimeOut;
                return  (TResult)Activator.CreateInstance(typeof(TResult),false, ErrorType.ThrottleLimitExceeded);
            }
            _requestCounts[ipAddress] = (count, startTime);
        }

        return await action();
    }

    public async Task<GetResponse<T>> GetResource(GetRequest request)
    {
        return await ThrottleAsync(() => _innerService.GetResource(request), request.IpAddress);
    }

    public async Task<AddOrUpdateResponse> AddOrUpdateResource(AddOrUpdateRequest<T> request)
    {
        return await ThrottleAsync(() => _innerService.AddOrUpdateResource(request), request.IpAddress);
    }
}
