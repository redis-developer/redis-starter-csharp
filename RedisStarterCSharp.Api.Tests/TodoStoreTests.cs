using Microsoft.Extensions.ObjectPool;
using RedisStarterCSharp.Api.Components.Todos;
using StackExchange.Redis;
using Xunit;

namespace RedisStarterCSharp.Api.Tests;

public class TodosStoreTests : IAsyncLifetime
{
    IConnectionMultiplexer redis;
    private readonly TodosStore _store;
    public TodosStoreTests()
    {
        redis = ConnectionMultiplexer.Connect("localhost");
        _store = new TodosStore(redis);
    }

    public async ValueTask InitializeAsync()
    {
        _store.DropIndex();
        _store.CreateIndexIfNotExists();
        await _store.DeleteAllAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _store.DeleteAllAsync();
        _store.DropIndex();
        _store.CreateIndexIfNotExists();
    }

    [Fact]
    public async Task CRUDForASingleTodo()
    {
        var sampleTodo = new CreateTodoDTO
        {
            Name = "Take out the trash",
        };

        var createdTodo = await _store.CreateAsync(sampleTodo);

        Assert.NotNull(createdTodo);
        Assert.NotNull(createdTodo.Value);
        Assert.Equal(sampleTodo.Name, createdTodo.Value.Name);

        var readResult = await _store.OneAsync(createdTodo.Id);

        Assert.NotNull(readResult);
        Assert.Equal(sampleTodo.Name, readResult.Name);

        var update = new UpdateTodoDTO
        {
            Status = TodoStatus.InProgress
        };

        var updateResult = await _store.UpdateAsync(createdTodo.Id, update);

        Assert.NotNull(updateResult);
        Assert.Equal(sampleTodo.Name, updateResult.Name);
        Assert.Equal(TodoStatus.InProgress, updateResult.Status);
        Assert.NotNull(updateResult.CreatedDate);
        Assert.NotNull(updateResult.UpdatedDate);
        Assert.NotNull(createdTodo.Value.CreatedDate);

        Assert.True(DateTime.Compare((DateTime)updateResult.CreatedDate, (DateTime)createdTodo.Value.CreatedDate) == 0);
        Assert.True(DateTime.Compare((DateTime)updateResult.CreatedDate, (DateTime)updateResult.UpdatedDate) < 0);

        await _store.DeleteAsync(createdTodo.Id);
    }

    [Fact]
    public async Task CreateAndReadMultipleTodos()
    {
        var todos = new string[]{
            "Take out the trash",
            "Vacuum downstairs",
            "Fold the laundry"
        };

        foreach (var name in todos) {
            await _store.CreateAsync(new CreateTodoDTO{
                Name = name
            });
        }

        var allTodos = await _store.AllAsync();

        Assert.NotNull(allTodos);
        Assert.NotNull(allTodos.Documents);
        Assert.Equal(allTodos.Total, allTodos.Documents.Count);
        Assert.Equal(todos.Length, allTodos.Total);

        foreach (var todo in allTodos.Documents) {
            Assert.NotNull(todo);
            Assert.NotNull(todo.Value);
            Assert.Contains(todo.Value.Name, todos);
        }
    }
}