using Microsoft.AspNetCore.Mvc;

namespace Components.Todos;

[ApiController]
[Route("todos")]
public class TodosController : ControllerBase
{
    private ITodosStore _store;

    public TodosController(ITodosStore store) {
        _store = store;
    }

    [HttpGet]
    public async Task<TodoResults> AllAsync()
    {
        return await _store.AllAsync();
    }

    [HttpGet("search")]
    public async Task<TodoResults> SearchAsync(string? name, string? status)
    {
        return await _store.SearchAsync(name, status);
    }

    [HttpGet("{id}")]
    [ProducesResponseType<Todo>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OneAsync(string id)
    {
        var result = await _store.OneAsync(id);

        return result is null ? NotFound(null) : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType<TodoDocument>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync(CreateTodoDTO input)
    {
        var result = await _store.CreateAsync(input);

        return result is null ? BadRequest() : Created("/todos/{id}", result);
    }

    [HttpPatch("{id}")]
    [ProducesResponseType<Todo>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAsync(string id, UpdateTodoDTO input)
    {
        switch (input.Status) {
            case TodoStatus.Todo:
            case TodoStatus.InProgress:
            case TodoStatus.Complete:
                break;
            default:
                return BadRequest($"invalid status {input.Status}");
        }

        var result = await _store.UpdateAsync(id, input);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task DeleteAsync(string id)
    {
        await _store.DeleteAsync(id);
    }
}
