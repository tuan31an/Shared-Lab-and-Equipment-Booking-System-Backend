using System.Linq.Expressions;
using LabBooking.Application.Common;
using LabBooking.Domain.Common;
using LabBooking.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace LabBooking.Tests;

public class FakeSender : ISender
{
    private readonly Dictionary<Type, Func<object, object>> _handlers = new();

    public List<object> Sent { get; } = new();

    public FakeSender Register<TRequest>(object response) where TRequest : class
    {
        _handlers[typeof(TRequest)] = _ => response;
        return this;
    }

    public FakeSender Register<TRequest>(Func<TRequest, object> handler) where TRequest : class
    {
        _handlers[typeof(TRequest)] = request => handler((TRequest)request);
        return this;
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        Sent.Add(request);
        if (!_handlers.TryGetValue(request.GetType(), out var handler))
            throw new InvalidOperationException($"No handler registered for {request.GetType().Name}.");
        return Task.FromResult((TResponse)handler(request));
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        Sent.Add(request);
        if (!_handlers.TryGetValue(request.GetType(), out var handler))
            throw new InvalidOperationException($"No handler registered for {request.GetType().Name}.");
        return Task.FromResult<object?>(handler(request));
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        Sent.Add(request);
        if (!_handlers.TryGetValue(request.GetType(), out var handler))
            throw new InvalidOperationException($"No handler registered for {request.GetType().Name}.");
        handler(request);
        return Task.CompletedTask;
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Streaming is not exercised in unit tests.");

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Streaming is not exercised in unit tests.");
}

public class FakeRepository<T> : IRepository<T> where T : BaseEntity
{
    public List<T> Items { get; } = [];

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Items.FirstOrDefault(e => e.Id == id));

    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => Task.FromResult(Items.AsQueryable().FirstOrDefault(predicate));

    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<T>>(Items.ToList());

    public Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<T>>(
            predicate == null ? Items.ToList() : Items.AsQueryable().Where(predicate).ToList());

    public Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct = default)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Max(pageSize, 1);
        var items = Items
            .OrderByDescending(e => e.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return Task.FromResult(new PagedResult<T>(items, Items.Count));
    }

    public Task AddAsync(T entity, CancellationToken ct = default)
    {
        Items.Add(entity);
        return Task.CompletedTask;
    }

    public void Update(T entity) { }

    public void Remove(T entity) => Items.Remove(entity);
}

public class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SaveCount++;
        return Task.FromResult(0);
    }
}

public class FakeCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; }
    public string? Role { get; set; }
}

internal static class TestConfig
{
    public static IConfiguration Build(params (string Key, string Value)[] pairs)
    {
        var dict = pairs.ToDictionary(p => p.Key, p => (string?)p.Value);
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    public static IConfiguration Empty() => new ConfigurationBuilder().AddInMemoryCollection().Build();
}
