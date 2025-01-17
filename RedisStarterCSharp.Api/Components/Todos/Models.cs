using System.Text.Json.Serialization;

namespace RedisStarterCSharp.Api.Components.Todos;

public static class TodoStatus
{
    public const string Todo = "todo";
    public const string InProgress = "in progress";
    public const string Complete = "complete";
}

public class Todo
{

    [JsonPropertyName("name")]
    required public string Name { get; set; }

    [JsonPropertyName("status")]
    required public string Status { get; set; }

    [JsonPropertyName("created_date")]
    public DateTime? CreatedDate { get; set; }

    [JsonPropertyName("updated_date")]
    public DateTime? UpdatedDate { get; set; }
}

public class CreateTodoDTO
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    required public string Name { get; set; }
}

public class UpdateTodoDTO
{
    [JsonPropertyName("status")]
    required public string Status { get; set; }
}

public class TodoDocument
{
    [JsonPropertyName("id")]
    required public string Id { get; set; }

    [JsonPropertyName("value")]
    public Todo? Value { get; set; }
}

public class TodoResults
{
    [JsonPropertyName("total")]
    required public long Total { get; set; }

    [JsonPropertyName("documents")]
    required public List<TodoDocument> Documents { get; set; }
}

public class TodoNotFoundException : Exception
{
    public TodoNotFoundException(string id) : base($"Todo \"{id}\" not found")
    {
    }
}

public class InvalidTodoException : Exception
{
    public InvalidTodoException(string message) : base(message)
    {
    }
}