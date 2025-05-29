using Npgsql;
using System;
using System.Collections.Generic;

public class BookStoreDatabase
{
    private readonly string _connectionString;

    public BookStoreDatabase(string host, string database, string username, string password)
    {
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(database) ||
            string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Неверные параметры подключения к базе данных");

        _connectionString = $"Host={host};Database={database};Username={username};" +
                          (string.IsNullOrWhiteSpace(password) ? "" : $"Password={password};");
    }

    public bool TestConnection()
    {
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            return true;
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка подключения к базе данных", ex);
        }
    }

    public int AddBook(string title, string isbn, decimal price, string publisher = null,
                      DateTime? publicationDate = null, int authorId = 0)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Название книги не может быть пустым");
        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("ISBN не может быть пустым");
        if (price <= 0)
            throw new ArgumentException("Цена должна быть положительной");
        if (authorId <= 0)
            throw new ArgumentException("Неверный ID автора");

        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            var sql = @"INSERT INTO books (title, isbn, price, publisher, publication_date) 
                        VALUES (@title, @isbn, @price, @publisher, @publicationDate)
                        RETURNING book_id;";

            using var cmd = new NpgsqlCommand(sql, connection, transaction);
            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@isbn", isbn);
            cmd.Parameters.AddWithValue("@price", price);
            cmd.Parameters.AddWithValue("@publisher", publisher ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@publicationDate", publicationDate ?? (object)DBNull.Value);

            var bookId = (int)cmd.ExecuteScalar();

            sql = @"INSERT INTO book_authors (book_id, author_id) 
                    VALUES (@bookId, @authorId);";

            using var authorCmd = new NpgsqlCommand(sql, connection, transaction);
            authorCmd.Parameters.AddWithValue("@bookId", bookId);
            authorCmd.Parameters.AddWithValue("@authorId", authorId);
            authorCmd.ExecuteNonQuery();

            transaction.Commit();
            return bookId;
        }
        catch (PostgresException ex) when (ex.SqlState == "23503") // Ошибка внешнего ключа
        {
            transaction.Rollback();
            throw new Exception("Автор с указанным ID не существует", ex);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public List<BookWithAuthor> GetBooksWithAuthors()
    {
        try
        {
            var result = new List<BookWithAuthor>();

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var sql = @"SELECT b.book_id, b.title, b.isbn, b.price, b.publisher, b.publication_date,
                               a.author_id, a.first_name, a.last_name
                        FROM books b
                        JOIN book_authors ba ON b.book_id = ba.book_id
                        JOIN authors a ON ba.author_id = a.author_id
                        ORDER BY b.book_id;";

            using var cmd = new NpgsqlCommand(sql, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                result.Add(new BookWithAuthor
                {
                    BookId = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    ISBN = reader.GetString(2),
                    Price = reader.GetDecimal(3),
                    Publisher = reader.IsDBNull(4) ? null : reader.GetString(4),
                    PublicationDate = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                    AuthorId = reader.GetInt32(6),
                    AuthorFirstName = reader.GetString(7),
                    AuthorLastName = reader.GetString(8)
                });
            }

            if (result.Count == 0)
                throw new InvalidOperationException("В базе нет книг с авторами");

            return result;
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при получении списка книг с авторами", ex);
        }
    }

    public List<Book> GetBooksByAuthorId(int authorId)
    {
        if (authorId <= 0)
            throw new ArgumentException("Неверный ID автора");

        try
        {
            var books = new List<Book>();

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var sql = @"SELECT b.book_id, b.title, b.isbn, b.price, b.publisher, b.publication_date
                        FROM books b
                        JOIN book_authors ba ON b.book_id = ba.book_id
                        WHERE ba.author_id = @authorId
                        ORDER BY b.title;";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@authorId", authorId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                books.Add(new Book
                {
                    BookId = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    ISBN = reader.GetString(2),
                    Price = reader.GetDecimal(3),
                    Publisher = reader.IsDBNull(4) ? null : reader.GetString(4),
                    PublicationDate = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5)
                });
            }

            return books;
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при поиске книг по автору", ex);
        }
    }

    public bool UpdateBook(int bookId, string title, decimal price)
    {
        if (bookId <= 0)
            throw new ArgumentException("Неверный ID книги");
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Название книги не может быть пустым");
        if (price <= 0)
            throw new ArgumentException("Цена должна быть положительной");

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var sql = "UPDATE books SET title = @title, price = @price WHERE book_id = @bookId;";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@price", price);
            cmd.Parameters.AddWithValue("@bookId", bookId);

            return cmd.ExecuteNonQuery() > 0;
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при обновлении книги", ex);
        }
    }

    public bool DeleteBook(int bookId)
    {
        if (bookId <= 0)
            throw new ArgumentException("Неверный ID книги");

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                var sql = "DELETE FROM book_authors WHERE book_id = @bookId;";
                using var cmd = new NpgsqlCommand(sql, connection, transaction);
                cmd.Parameters.AddWithValue("@bookId", bookId);
                cmd.ExecuteNonQuery();

                sql = "DELETE FROM books WHERE book_id = @bookId;";
                using var deleteCmd = new NpgsqlCommand(sql, connection, transaction);
                deleteCmd.Parameters.AddWithValue("@bookId", bookId);
                var result = deleteCmd.ExecuteNonQuery() > 0;

                transaction.Commit();
                return result;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при удалении книги", ex);
        }
    }

    public List<Author> GetAllAuthors()
    {
        try
        {
            var authors = new List<Author>();

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var sql = "SELECT author_id, first_name, last_name, biography FROM authors ORDER BY last_name, first_name;";

            using var cmd = new NpgsqlCommand(sql, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                authors.Add(new Author
                {
                    AuthorId = reader.GetInt32(0),
                    FirstName = reader.GetString(1),
                    LastName = reader.GetString(2),
                    Biography = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }

            return authors;
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при получении списка авторов", ex);
        }
    }

    public int AddAuthor(string firstName, string lastName, string biography = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("Имя автора не может быть пустым");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Фамилия автора не может быть пустой");

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var sql = @"INSERT INTO authors (first_name, last_name, biography) 
                        VALUES (@firstName, @lastName, @biography)
                        RETURNING author_id;";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@firstName", firstName);
            cmd.Parameters.AddWithValue("@lastName", lastName);
            cmd.Parameters.AddWithValue("@biography", biography ?? (object)DBNull.Value);

            return (int)cmd.ExecuteScalar();
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при добавлении автора", ex);
        }
    }
    public List<Book> GetBooksWithoutAuthors()
    {
        var books = new List<Book>();

        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        var sql = @"SELECT b.book_id, b.title, b.isbn, b.price, b.publisher, b.publication_date
                    FROM books b
                    LEFT JOIN book_authors ba ON b.book_id = ba.book_id
                    WHERE ba.author_id IS NULL
                    ORDER BY b.title;";

        using var cmd = new NpgsqlCommand(sql, connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            books.Add(new Book
            {
                BookId = reader.GetInt32(0),
                Title = reader.GetString(1),
                ISBN = reader.GetString(2),
                Price = reader.GetDecimal(3),
                Publisher = reader.IsDBNull(4) ? null : reader.GetString(4),
                PublicationDate = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5)
            });
        }

        return books;
    }

    public bool AddAuthorToBook(int bookId, int authorId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        var sql = "INSERT INTO book_authors (book_id, author_id) VALUES (@bookId, @authorId);";

        using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@bookId", bookId);
        cmd.Parameters.AddWithValue("@authorId", authorId);

        return cmd.ExecuteNonQuery() > 0;
    }

    public bool DeleteAuthor(int authorId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            // Сначала проверим, есть ли книги у этого автора
            var checkSql = "SELECT COUNT(*) FROM book_authors WHERE author_id = @authorId;";
            using var checkCmd = new NpgsqlCommand(checkSql, connection, transaction);
            checkCmd.Parameters.AddWithValue("@authorId", authorId);
            var bookCount = (long)checkCmd.ExecuteScalar();

            if (bookCount > 0)
            {
                // Если у автора есть книги, не удаляем его
                transaction.Rollback();
                return false;
            }

            // Если книг нет, удаляем автора
            var deleteSql = "DELETE FROM authors WHERE author_id = @authorId;";
            using var deleteCmd = new NpgsqlCommand(deleteSql, connection, transaction);
            deleteCmd.Parameters.AddWithValue("@authorId", authorId);
            var result = deleteCmd.ExecuteNonQuery() > 0;

            transaction.Commit();
            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}