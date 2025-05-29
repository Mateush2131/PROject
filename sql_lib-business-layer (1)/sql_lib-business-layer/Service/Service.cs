using System;
using System.Collections.Generic;
using System.Linq;

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
        try
        {
            var books = _database.GetBooksWithAuthors();
            if (books.Count == 0)
                throw new InvalidOperationException("No books found in database");
            return books;
        }
        catch (Exception ex)
        {
            throw new BookStoreException("Failed to retrieve books with authors", ex);
        }
    }

    public BookWithAuthor GetBookById(int bookId)
    {
        ValidateBookId(bookId);

        try
        {
            var books = _database.GetBooksWithAuthors();
            var book = books.FirstOrDefault(b => b.BookId == bookId);

            if (book == null)
                throw new NotFoundException($"Book with ID {bookId} not found");

            return book;
        }
        catch (Exception ex)
        {
            throw new BookStoreException($"Failed to retrieve book with ID {bookId}", ex);
        }
    }

    public List<Book> GetBooksByAuthor(int authorId)
    {
        ValidateAuthorId(authorId);

        try
        {
            var books = _database.GetBooksByAuthorId(authorId);
            if (books.Count == 0) throw new NotFoundException($"No books found for author ID {authorId}");
            return books;
        }
        catch (Exception ex)
        {
            throw new BookStoreException($"Failed to retrieve books for author ID {authorId}", ex);
        }
    }

    public List<Book> GetBooksWithoutAuthors()
    {
        try
        {
            return _database.GetBooksWithoutAuthors();
        }
        catch (Exception ex)
        {
            throw new BookStoreException("Failed to retrieve books without authors", ex);
        }
    }

    public BookWithAuthor AddBook(string title, string isbn, decimal price,
                                string publisher = null, DateTime? publicationDate = null, int authorId = 0)
    {
        ValidateBook(title, isbn, price);
        ValidateAuthorId(authorId);

        try
        {
            var bookId = _database.AddBook(title, isbn, price, publisher, publicationDate, authorId);
            return GetBookById(bookId);
        }
        catch (Exception ex)
        {
            throw new BookStoreException("Failed to add new book", ex);
        }
    }

    public BookWithAuthor UpdateBook(int bookId, string title, decimal price)
    {
        ValidateBookId(bookId);
        ValidateBook(title, null, price);

        try
        {
            if (!_database.UpdateBook(bookId, title, price))
                throw new NotFoundException($"Book with ID {bookId} not found");

            return GetBookById(bookId);
        }
        catch (Exception ex)
        {
            throw new BookStoreException($"Failed to update book with ID {bookId}", ex);
        }
    }

    public void DeleteBook(int bookId)
    {
        ValidateBookId(bookId);

        try
        {
            if (!_database.DeleteBook(bookId))
                throw new NotFoundException($"Book with ID {bookId} not found");
        }
        catch (Exception ex)
        {
            throw new BookStoreException($"Failed to delete book with ID {bookId}", ex);
        }
    }

    public bool AddAuthorToBook(int bookId, int authorId)
    {
        ValidateBookId(bookId);
        ValidateAuthorId(authorId);

        try
        {
            return _database.AddAuthorToBook(bookId, authorId);
        }
        catch (Exception ex)
        {
            throw new BookStoreException($"Failed to add author {authorId} to book {bookId}", ex);
        }
    }

    // Author Operations
    public List<Author> GetAllAuthors()
    {
        try
        {
            var authors = _database.GetAllAuthors();
            if (authors.Count == 0) throw new InvalidOperationException("No authors found in database");
            return authors;
        }
        catch (Exception ex)
        {
            throw new BookStoreException("Failed to retrieve authors", ex);
        }
    }

    public Author GetAuthor(int authorId)
    {
        ValidateAuthorId(authorId);

        try
        {
            var authors = _database.GetAllAuthors();
            var author = authors.FirstOrDefault(a => a.AuthorId == authorId);
            if (author == null) throw new NotFoundException($"Author with ID {authorId} not found");
            return author;
        }
        catch (Exception ex)
        {
            throw new BookStoreException($"Failed to retrieve author with ID {authorId}", ex);
        }
    }

    public Author AddAuthor(string firstName, string lastName, string biography = null)
    {
        ValidateAuthor(firstName, lastName);

        try
        {
            var authorId = _database.AddAuthor(firstName, lastName, biography);
            return GetAuthor(authorId);
        }
        catch (Exception ex)
        {
            throw new BookStoreException("Failed to add new author", ex);
        }
    }

    public void DeleteAuthor(int authorId)
    {
        ValidateAuthorId(authorId);

        try
        {
            if (!_database.DeleteAuthor(authorId))
                throw new InvalidOperationException($"Cannot delete author with ID {authorId} because they have books assigned");
        }
        catch (Exception ex)
        {
            throw new BookStoreException($"Failed to delete author with ID {authorId}", ex);
        }
    }

    // Customer Operations
    public Customer AddCustomer(string firstName, string lastName, string email = null, string phone = null, string address = null)
    {
        ValidateCustomer(firstName, lastName, email, phone);

        try
        {
            var customerId = _database.AddCustomer(firstName, lastName, email, phone, address);
            return GetCustomer(customerId);
        }
        catch (Exception ex)
        {
            throw new BookStoreException("Failed to add new customer", ex);
        }
    }

    public Customer GetCustomer(int customerId)
    {
        ValidateCustomerId(customerId);

        try
        {
            return _database.GetCustomerById(customerId);
        }
        catch (Exception ex)
        {
            throw new BookStoreException($"Failed to retrieve customer with ID {customerId}", ex);
        }
    }

    // Order Operations
    public Order CreateOrder(int customerId, DateTime requiredDate, List<OrderItem> items)
    {
        ValidateCustomerId(customerId);
        ValidateOrderItems(items);
        ValidateRequiredDate(requiredDate);

        try
        {
            var orderId = _database.CreateOrder(customerId, requiredDate, items);
            return GetOrder(orderId);
        }
        catch (Exception ex)
        {
            throw new BookStoreException("Failed to create new order", ex);
        }
    }

    public Order GetOrder(int orderId)
    {
        ValidateOrderId(orderId);

        try
        {
            return _database.GetOrderById(orderId);
        }
        catch (Exception ex)
        {
            throw new BookStoreException($"Failed to retrieve order with ID {orderId}", ex);
        }
    }

    public List<Order> GetCustomerOrders(int customerId)
    {
        ValidateCustomerId(customerId);

        try
        {
            var orders = _database.GetCustomerOrders(customerId);
            if (orders.Count == 0) throw new NotFoundException($"No orders found for customer ID {customerId}");
            return orders;
        }
        catch (Exception ex)
        {
            throw new BookStoreException($"Failed to retrieve orders for customer ID {customerId}", ex);
        }
    }

    public List<Order> GetAllOrders(DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var orders = _database.GetAllOrders(fromDate, toDate);
            return orders; // Возвращаем пустой список, если нет заказов
        }
        catch (Exception ex)
        {
            throw new BookStoreException("Failed to retrieve orders", ex);
        }
    }

    public void UpdateOrderStatus(int orderId, OrderStatus newStatus)
    {
        ValidateOrderId(orderId);

        try
        {
            if (!_database.UpdateOrderStatus(orderId, newStatus))
                throw new NotFoundException($"Order with ID {orderId} not found");
        }
        catch (Exception ex)
        {
            throw new BookStoreException($"Failed to update status for order ID {orderId}", ex);
        }
    }

    public void CancelOrder(int orderId)
    {
        ValidateOrderId(orderId);

        try
        {
            if (!_database.CancelOrder(orderId))
                throw new InvalidOperationException($"Cannot cancel order with ID {orderId}");
        }
        catch (Exception ex)
        {
            throw new BookStoreException($"Failed to cancel order with ID {orderId}", ex);
        }
    }

    // Validation methods
    private void ValidateBookId(int bookId)
    {
        if (bookId <= 0) throw new ArgumentException("Invalid book ID");
    }

    private void ValidateAuthorId(int authorId)
    {
        if (authorId <= 0) throw new ArgumentException("Invalid author ID");
    }

    private void ValidateCustomerId(int customerId)
    {
        if (customerId <= 0) throw new ArgumentException("Invalid customer ID");
    }

    private void ValidateOrderId(int orderId)
    {
        if (orderId <= 0) throw new ArgumentException("Invalid order ID");
    }

    private void ValidateBook(string title, string isbn, decimal price)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Book title is required");
        if (isbn != null && string.IsNullOrWhiteSpace(isbn)) throw new ArgumentException("ISBN is required");
        if (price <= 0) throw new ArgumentException("Price must be positive");
    }

    private void ValidateAuthor(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("Author first name is required");
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("Author last name is required");
    }

    private void ValidateCustomer(string firstName, string lastName, string email, string phone)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("Customer first name is required");
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("Customer last name is required");
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Either email or phone must be provided");
    }

    private void ValidateOrderItems(List<OrderItem> items)
    {
        if (items == null || items.Count == 0) throw new ArgumentException("Order must contain at least one item");
        foreach (var item in items)
        {
            if (item.BookId <= 0) throw new ArgumentException("Invalid book ID in order items");
            if (item.Quantity <= 0) throw new ArgumentException("Quantity must be positive");
        }
    }

    private void ValidateRequiredDate(DateTime requiredDate)
    {
        if (requiredDate < DateTime.Today) throw new ArgumentException("Required date cannot be in the past");
    }
}

// Custom exceptions
public class BookStoreException : Exception
{
    public BookStoreException(string message) : base(message) { }
    public BookStoreException(string message, Exception inner) : base(message, inner) { }
}

public class NotFoundException : BookStoreException
{
    public NotFoundException(string message) : base(message) { }
}