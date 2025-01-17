using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;
using StackExchange.Redis;

namespace RedisStarterCSharp.Api.Components.Todos;

public interface ITodosStore
{
    Task<TodoResults> AllAsync();
    Task<TodoResults> SearchAsync(string? name, string? status);

    /// <exception cref="TodoNotFoundException">is thrown if the todo does not exist</exception>
    Task<Todo> OneAsync(string id);

    /// <exception cref="InvalidTodoException">is thrown if the todo is invalid and not created</exception>
    Task<TodoDocument?> CreateAsync(CreateTodoDTO input);


    /// <exception cref="TodoNotFoundException">is thrown if the todo does not exist</exception>
    /// <exception cref="InvalidTodoException">is thrown if the todo is invalid and not updated</exception>
    Task<Todo?> UpdateAsync(string id, UpdateTodoDTO input);
    Task DeleteAsync(string id);
    Task DeleteAllAsync();
    void CreateIndexIfNotExists();
    void DropIndex();
}

public class TodosStore : ITodosStore
{
    private const string TodosIndex = "todos-idx";
    private const string TodoPrefix = "todos:";
    private readonly IDatabase _redis;

    public TodosStore(IConnectionMultiplexer muxer)
    {
        _redis = muxer.GetDatabase();
        CreateIndexIfNotExists();
    }

    private string FormatId(string id)
    {
        if (id.StartsWith(TodoPrefix))
        {
            return id;
        }

        return $"{TodoPrefix}{id}";
    }

    private bool HaveIndex()
    {
        var result = _redis.FT()._List();

        return result.Any(i => i.ToString() == TodosIndex);
    }

    public void CreateIndexIfNotExists()
    {
        if (HaveIndex())
        {
            return;
        }

        var schema = new Schema()
            .AddTextField(new FieldName("$.name", "name"))
            .AddTextField(new FieldName("$.status", "status"));

        _redis.FT().Create(
            TodosIndex,
            new FTCreateParams()
                .On(IndexDataType.JSON)
                .Prefix(TodoPrefix),
            schema
        );
    }

    public void DropIndex()
    {
        if (!HaveIndex())
        {
            return;
        }

        _redis.FT().DropIndex(TodosIndex);
    }

    private TodoResults DeserializeTodoResults(SearchResult result)
    {
        var documents = new List<TodoDocument>();

        foreach (var doc in result.Documents)
        {
            var properties = doc.GetProperties();

            documents.Add(new TodoDocument
            {
                Id = doc.Id,
            });
        }

        var response = new TodoResults
        {
            Documents = result.Documents.Select(t => new TodoDocument
            {
                Id = t.Id,
                Value = JsonSerializer.Deserialize<Todo>(t["json"].ToString()),
            }).ToList(),
            Total = result.TotalResults,
        };

        return response;
    }

    [HttpGet]
    public async Task<TodoResults> AllAsync()
    {
        var query = new Query("*");
        var result = await _redis.FT().SearchAsync(TodosIndex, query);

        return DeserializeTodoResults(result);
    }

    public async Task<TodoResults> SearchAsync(string? name, string? status)
    {
        var searches = new List<string>();

        if (name != null && name.Length > 0)
        {
            searches.Add($"@name:({name})");
        }

        if (status != null && status.Length > 0)
        {
            searches.Add($"@status:{status}");
        }

        var query = new Query(string.Join(" ", searches));
        var result = await _redis.FT().SearchAsync(TodosIndex, query);

        return DeserializeTodoResults(result);
    }

    public async Task<Todo> OneAsync(string id)
    {
        var result = await _redis.JSON().GetAsync(FormatId(id));

        if (result.IsNull)
        {
            throw new TodoNotFoundException(id);
        }

        var todo = JsonSerializer.Deserialize<Todo>(result.ToString()) ?? throw new TodoNotFoundException(id);

        return todo;
    }

    public async Task<TodoDocument?> CreateAsync(CreateTodoDTO input)
    {
        var now = DateTime.UtcNow;
        var id = input.Id is null ? Guid.NewGuid().ToString() : input.Id;

        var todoDocument = new TodoDocument
        {
            Id = FormatId(id),
            Value = new Todo
            {
                Name = input.Name,
                Status = TodoStatus.Todo,
                CreatedDate = now,
                UpdatedDate = now
            }
        };

        var ok = await _redis.JSON().SetAsync(FormatId(id), "$", todoDocument.Value);

        if (!ok)
        {
            throw new InvalidTodoException("failed to create todo");
        }

        return todoDocument;
    }

    public async Task<Todo?> UpdateAsync(string id, UpdateTodoDTO input)
    {
        switch (input.Status)
        {
            case TodoStatus.Todo:
            case TodoStatus.InProgress:
            case TodoStatus.Complete:
                break;
            default:
                throw new InvalidTodoException($"invalid status \"{input.Status}\"");
        }

        id = FormatId(id);
        var todo = await OneAsync(id);

        if (todo is null)
        {
            throw new TodoNotFoundException(id);
        }

        todo.Status = input.Status;
        todo.UpdatedDate = DateTime.UtcNow;

        var ok = await _redis.JSON().SetAsync(id, "$", todo);

        if (!ok)
        {
            throw new InvalidTodoException($"failed to update todo \"{id}\"");
        }

        return todo;
    }

    public async Task DeleteAsync(string id)
    {
        await _redis.JSON().DelAsync(FormatId(id));
    }

    public async Task DeleteAllAsync()
    {
        var todos = await AllAsync();

        await _redis.KeyDeleteAsync([.. todos.Documents.Select(t => (RedisKey)t.Id)]);
    }
}
