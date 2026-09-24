using Microsoft.AspNetCore.Mvc;
using SimpleBooksApi.Models;

namespace SimpleBooksApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private static readonly List<Book> Books = new()
    {
        new Book { Id = 1, Title = "Clean Code", Author = "Robert C. Martin", Year = 2008 },
        new Book { Id = 2, Title = "Design Patterns", Author = "GoF", Year = 1994 }
    };

    [HttpGet]
    public ActionResult<IEnumerable<Book>> GetAll()
    {
        return Ok(Books);
    }

    [HttpGet("{id}")]
    public ActionResult<Book> GetById(int id)
    {
        var book = Books.FirstOrDefault(b => b.Id == id);
        if (book is null)
            return NotFound(new { message = "Book not found." });

        return Ok(book);
    }

    [HttpPost]
    public ActionResult<Book> Create([FromBody] Book book)
    {
        book.Id = Books.Count == 0 ? 1 : Books.Max(b => b.Id) + 1;
        Books.Add(book);
        return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
    }

    [HttpPut("{id}")]
    public IActionResult Update(int id, [FromBody] Book updatedBook)
    {
        var existingBook = Books.FirstOrDefault(b => b.Id == id);
        if (existingBook is null)
            return NotFound(new { message = "Book not found." });

        existingBook.Title = updatedBook.Title;
        existingBook.Author = updatedBook.Author;
        existingBook.Year = updatedBook.Year;

        return Ok(new { message = "Book updated successfully." });
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var existingBook = Books.FirstOrDefault(b => b.Id == id);
        if (existingBook is null)
            return NotFound(new { message = "Book not found." });

        Books.Remove(existingBook);
        return Ok(new { message = "Book deleted successfully." });
    }
}
