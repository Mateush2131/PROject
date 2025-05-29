using System;
using System.Collections.Generic;

public class BookStoreService
{
    private readonly BookStoreDatabase _database;

    public BookStoreService(BookStoreDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    // Book Operations
    public List<BookWithAuthor> GetAllBooksWithAuthors()
    {
        var books = _database.GetBooksWithAuthors();
        if (books.Count == 0) throw new InvalidOperationException("No books found");
        return books;
    }

    public BookWithAuthor AddBook(string title, string isbn, decimal price,
                                string publisher, DateTime? pubDate, int authorId)
    {
        ValidateBook(title, isbn, price);
        var bookId = _database.AddBook(title, isbn, price, publisher, pubDate, authorId);
        return GetBookById(bookId);
    }

    public BookWithAuthor UpdateBook(int bookId, string title, decimal price)
    {
        ValidateBook(title, null, price);
        if (!_database.UpdateBook(bookId, title, price))
            throw new KeyNotFoundException("Book not found");
        return GetBookById(bookId);
    }

    // Author Operations
    public List<Author> GetAllAuthors()
    {
        var authors = _database.GetAllAuthors();
        if (authors.Count == 0) throw new InvalidOperationException("No authors found");
        return authors;
    }

    public Author AddAuthor(string firstName, string lastName, string bio = null)
    {
        ValidateAuthor(firstName, lastName);
        var authorId = _database.AddAuthor(firstName, lastName, bio);
        return GetAuthorById(authorId);
    }

    // Helper methods
    private BookWithAuthor GetBookById(int bookId)
    {
        var books = _database.GetBooksWithAuthors();
        return books.Find(b => b.BookId == bookId) ?? throw new KeyNotFoundException("Book not found");
    }

    private Author GetAuthorById(int authorId)
    {
        var authors = _database.GetAllAuthors();
        return authors.Find(a => a.AuthorId == authorId) ?? throw new KeyNotFoundException("Author not found");
    }

    private void ValidateBook(string title, string isbn, decimal price)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required");
        if (isbn != null && string.IsNullOrWhiteSpace(isbn)) throw new ArgumentException("ISBN is required");
        if (price <= 0) throw new ArgumentException("Price must be positive");
    }

    private void ValidateAuthor(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("First name is required");
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("Last name is required");
    }
}
