using Microsoft.AspNetCore.Mvc;

namespace Components.Todos;

[ApiController]
[Route("api/todos")]
public class TodosController : ControllerBase
{
    private ITodosStore _store;

    public TodosController(ITodosStore store)
    {
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
        try
        {
            var result = await _store.OneAsync(id);

            return Ok(result);
        }
        catch (TodoNotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost]
    [ProducesResponseType<TodoDocument>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync(CreateTodoDTO input)
    {
        try
        {
            var result = await _store.CreateAsync(input);

            return Created("/todos/{id}", result);
        }
        catch (InvalidTodoException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPatch("{id}")]
    [ProducesResponseType<Todo>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAsync(string id, UpdateTodoDTO input)
    {
        try
        {
            var result = await _store.UpdateAsync(id, input);

            return Ok(result);
        }
        catch (TodoNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (InvalidTodoException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task DeleteAsync(string id)
    {
        await _store.DeleteAsync(id);
    }
}
