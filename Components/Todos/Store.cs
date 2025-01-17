using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;
using StackExchange.Redis;

namespace Components.Todos;

public interface ITodosStore
{
    Task<TodoResults> AllAsync();
    Task<TodoResults> SearchAsync(string? name, string? status);
    Task<Todo?> OneAsync(string id);
    Task<TodoDocument?> CreateAsync(CreateTodoDTO input);
    Task<Todo?> UpdateAsync(string id, UpdateTodoDTO input);
    Task DeleteAsync(string id);
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

    private void CreateIndexIfNotExists()
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

    public async Task<Todo?> OneAsync(string id)
    {
        var result = await _redis.JSON().GetAsync(FormatId(id));

        if (result.IsNull)
        {
            return null;
        }

        var todo = JsonSerializer.Deserialize<Todo>(result.ToString());

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

        return ok ? todoDocument : null;
    }

    public async Task<Todo?> UpdateAsync(string id, UpdateTodoDTO input)
    {
        id = FormatId(id);
        var todo = await OneAsync(id);

        if (todo is null)
        {
            return null;
        }

        todo.Status = input.Status;
        todo.UpdatedDate = DateTime.UtcNow;

        var ok = await _redis.JSON().SetAsync(id, "$", todo);

        return ok ? todo : null;
    }

    public async Task DeleteAsync(string id)
    {
        await _redis.JSON().DelAsync(FormatId(id));
    }
}
