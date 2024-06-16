using CleoAssignment.ApiService.Dto;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CleoAssignment.ApiService;

public class ApiService<TResource> : IApiService<TResource>
{
    private readonly IResourceProvider<TResource> _resourceProvider;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _resourceLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
    private readonly ConcurrentDictionary<string, TResource> _resourceCache = new ConcurrentDictionary<string, TResource>();

    public ApiService(
        IResourceProvider<TResource> resourceProvider
        )
    {
        _resourceProvider = resourceProvider;
    }
    public async Task<GetResponse<TResource>> GetResource(GetRequest request)
    {
        
        var semaphore = _resourceLocks.GetOrAdd(request.ResourceId, new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();

        try
        {
            if (_resourceCache.TryGetValue(request.ResourceId, out var cachedResource))
                return new GetResponse<TResource>(true, cachedResource, null);
          
            var resource =  _resourceProvider.GetResource(request.ResourceId);
            _resourceCache[request.ResourceId] = resource;
           
            return new GetResponse<TResource>(true, resource, ErrorType.None);
        }
        catch (Exception ex)
        {
            return new GetResponse<TResource>(false, default, ErrorType.SomethingWrong);
        }
        finally
        {
            semaphore.Release();
        }
        
    }

    public async Task<AddOrUpdateResponse> AddOrUpdateResource(AddOrUpdateRequest<TResource> request)
    {
        
        var semaphore = _resourceLocks.GetOrAdd(request.ResourceId, new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        
        try
        {
            _resourceProvider.AddOrUpdateResource(request.ResourceId, request.Resource);
            
            if (_resourceCache.TryGetValue(request.ResourceId, out var cachedResource))
                _resourceCache[request.ResourceId] = request.Resource;
            
           
            return new AddOrUpdateResponse(true, ErrorType.None);
        }
        catch (Exception ex)
        {
            return new AddOrUpdateResponse(false, ErrorType.ResourceUpdateFailed);
        }
        finally
        {
            semaphore.Release();
        }
    }
}
