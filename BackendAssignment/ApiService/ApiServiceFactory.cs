using System;
using System.Collections.Concurrent;

namespace CleoAssignment.ApiService;

public static class ApiServiceFactory
{
    private static readonly ConcurrentDictionary<Type, object> _instances = new ConcurrentDictionary<Type, object>();

    public static IApiService<T> CreateApiService<T>(ThrottleSettings throttleSettings,
                                                     IResourceProvider<T> resourceProvider,
                                                     ITimeProvider timeProvider)
    {
        return  (IApiService<T>) _instances.GetOrAdd(typeof(T), _ => new ThrottlingApiServiceDecorator<T>(new ApiService<T>(resourceProvider), throttleSettings,timeProvider ));
       
    }
}
