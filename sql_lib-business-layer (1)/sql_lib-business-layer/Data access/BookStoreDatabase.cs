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

    public int AddCustomer(string firstName, string lastName, string email, string phone, string address)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Имя и фамилия клиента обязательны");
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Необходимо указать email или телефон");

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var sql = @"INSERT INTO customers (first_name, last_name, email, phone, address) 
                        VALUES (@firstName, @lastName, @email, @phone, @address)
                        RETURNING customer_id;";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@firstName", firstName);
            cmd.Parameters.AddWithValue("@lastName", lastName);
            cmd.Parameters.AddWithValue("@email", email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@phone", phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@address", address ?? (object)DBNull.Value);

            return (int)cmd.ExecuteScalar();
        }
        catch (PostgresException ex) when (ex.SqlState == "23505") // Ошибка уникальности
        {
            throw new Exception("Клиент с таким email или телефоном уже существует", ex);
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при добавлении клиента", ex);
        }
    }

    public Customer GetCustomerById(int customerId)
    {
        if (customerId <= 0)
            throw new ArgumentException("Неверный ID клиента");

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var sql = "SELECT * FROM customers WHERE customer_id = @customerId;";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@customerId", customerId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Customer
                {
                    CustomerId = reader.GetInt32(0),
                    FirstName = reader.GetString(1),
                    LastName = reader.GetString(2),
                    Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Phone = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Address = reader.IsDBNull(5) ? null : reader.GetString(5)
                };
            }

            throw new Exception("Клиент не найден");
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при получении информации о клиенте", ex);
        }
    }

    // Методы для работы с заказами
    public int CreateOrder(int customerId, DateTime requiredDate, List<OrderItem> items)
    {
        if (customerId <= 0)
            throw new ArgumentException("Неверный ID клиента");
        if (items == null || items.Count == 0)
            throw new ArgumentException("Заказ должен содержать хотя бы одну книгу");

        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            // 1. Создаем заказ
            var orderSql = @"INSERT INTO orders (customer_id, order_date, required_date, status, total_amount)
                            VALUES (@customerId, @orderDate, @requiredDate, @status, @totalAmount)
                            RETURNING order_id;";

            decimal totalAmount = 0;
            foreach (var item in items)
            {
                // Получаем текущую цену книги
                var priceSql = "SELECT price FROM books WHERE book_id = @bookId;";
                using var priceCmd = new NpgsqlCommand(priceSql, connection, transaction);
                priceCmd.Parameters.AddWithValue("@bookId", item.BookId);
                var price = (decimal)priceCmd.ExecuteScalar();
                totalAmount += price * item.Quantity;
            }

            using var orderCmd = new NpgsqlCommand(orderSql, connection, transaction);
            orderCmd.Parameters.AddWithValue("@customerId", customerId);
            orderCmd.Parameters.AddWithValue("@orderDate", DateTime.Now);
            orderCmd.Parameters.AddWithValue("@requiredDate", requiredDate);
            orderCmd.Parameters.AddWithValue("@status", OrderStatus.Pending.ToString());
            orderCmd.Parameters.AddWithValue("@totalAmount", totalAmount);

            var orderId = (int)orderCmd.ExecuteScalar();

            // 2. Добавляем элементы заказа
            foreach (var item in items)
            {
                // Проверяем доступное количество
                var quantitySql = "SELECT available_quantity FROM books WHERE book_id = @bookId FOR UPDATE;";
                using var quantityCmd = new NpgsqlCommand(quantitySql, connection, transaction);
                quantityCmd.Parameters.AddWithValue("@bookId", item.BookId);
                var availableQuantity = (int)quantityCmd.ExecuteScalar();

                if (availableQuantity < item.Quantity)
                {
                    transaction.Rollback();
                    throw new Exception($"Недостаточно книг в наличии (ID книги: {item.BookId}, доступно: {availableQuantity}, запрошено: {item.Quantity})");
                }

                // Получаем текущую цену книги
                var priceSql = "SELECT price FROM books WHERE book_id = @bookId;";
                using var priceCmd = new NpgsqlCommand(priceSql, connection, transaction);
                priceCmd.Parameters.AddWithValue("@bookId", item.BookId);
                var price = (decimal)priceCmd.ExecuteScalar();

                // Добавляем элемент заказа
                var itemSql = @"INSERT INTO order_items (order_id, book_id, quantity, unit_price)
                               VALUES (@orderId, @bookId, @quantity, @unitPrice);";
                using var itemCmd = new NpgsqlCommand(itemSql, connection, transaction);
                itemCmd.Parameters.AddWithValue("@orderId", orderId);
                itemCmd.Parameters.AddWithValue("@bookId", item.BookId);
                itemCmd.Parameters.AddWithValue("@quantity", item.Quantity);
                itemCmd.Parameters.AddWithValue("@unitPrice", price);
                itemCmd.ExecuteNonQuery();

                // Уменьшаем количество доступных книг
                var updateSql = "UPDATE books SET available_quantity = available_quantity - @quantity WHERE book_id = @bookId;";
                using var updateCmd = new NpgsqlCommand(updateSql, connection, transaction);
                updateCmd.Parameters.AddWithValue("@quantity", item.Quantity);
                updateCmd.Parameters.AddWithValue("@bookId", item.BookId);
                updateCmd.ExecuteNonQuery();
            }

            transaction.Commit();
            return orderId;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw new Exception("Ошибка при создании заказа", ex);
        }
    }

    public Order GetOrderById(int orderId)
    {
        if (orderId <= 0)
            throw new ArgumentException("Неверный ID заказа");

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var order = new Order();
            var sql = @"SELECT o.order_id, o.customer_id, o.order_date, o.required_date, 
                                o.shipped_date, o.status, o.total_amount,
                                c.first_name, c.last_name
                         FROM orders o
                         JOIN customers c ON o.customer_id = c.customer_id
                         WHERE o.order_id = @orderId;";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@orderId", orderId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                throw new Exception("Заказ не найден");

            order.OrderId = reader.GetInt32(0);
            order.CustomerId = reader.GetInt32(1);
            order.OrderDate = reader.GetDateTime(2);
            order.RequiredDate = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3);
            order.ShippedDate = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4);
            order.Status = (OrderStatus)Enum.Parse(typeof(OrderStatus), reader.GetString(5));
            order.TotalAmount = reader.GetDecimal(6);
            order.Customer = new Customer
            {
                CustomerId = reader.GetInt32(1),
                FirstName = reader.GetString(7),
                LastName = reader.GetString(8)
            };

            // Получаем элементы заказа
            reader.Close();
            order.OrderItems = GetOrderItems(orderId, connection);

            return order;
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при получении информации о заказе", ex);
        }
    }

    private List<OrderItem> GetOrderItems(int orderId, NpgsqlConnection connection)
    {
        var items = new List<OrderItem>();

        var sql = @"SELECT oi.order_item_id, oi.book_id, oi.quantity, oi.unit_price,
                           b.title, b.isbn
                    FROM order_items oi
                    JOIN books b ON oi.book_id = b.book_id
                    WHERE oi.order_id = @orderId;";

        using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@orderId", orderId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new OrderItem
            {
                OrderItemId = reader.GetInt32(0),
                BookId = reader.GetInt32(1),
                Quantity = reader.GetInt32(2),
                UnitPrice = reader.GetDecimal(3),
                Book = new Book
                {
                    BookId = reader.GetInt32(1),
                    Title = reader.GetString(4),
                    ISBN = reader.GetString(5)
                }
            });
        }

        return items;
    }

    public bool UpdateOrderStatus(int orderId, OrderStatus newStatus)
    {
        if (orderId <= 0)
            throw new ArgumentException("Неверный ID заказа");

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var sql = "UPDATE orders SET status = @status WHERE order_id = @orderId;";

            if (newStatus == OrderStatus.Shipped)
            {
                sql = "UPDATE orders SET status = @status, shipped_date = @shippedDate WHERE order_id = @orderId;";
            }

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@status", newStatus.ToString());
            cmd.Parameters.AddWithValue("@orderId", orderId);

            if (newStatus == OrderStatus.Shipped)
            {
                cmd.Parameters.AddWithValue("@shippedDate", DateTime.Now);
            }

            return cmd.ExecuteNonQuery() > 0;
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при обновлении статуса заказа", ex);
        }
    }

    public List<Order> GetCustomerOrders(int customerId)
    {
        if (customerId <= 0)
            throw new ArgumentException("Неверный ID клиента");

        try
        {
            var orders = new List<Order>();

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var sql = @"SELECT o.order_id, o.order_date, o.required_date, 
                                o.shipped_date, o.status, o.total_amount
                         FROM orders o
                         WHERE o.customer_id = @customerId
                         ORDER BY o.order_date DESC;";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@customerId", customerId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var order = new Order
                {
                    OrderId = reader.GetInt32(0),
                    OrderDate = reader.GetDateTime(1),
                    RequiredDate = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                    ShippedDate = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                    Status = (OrderStatus)Enum.Parse(typeof(OrderStatus), reader.GetString(4)),
                    TotalAmount = reader.GetDecimal(5)
                };
                orders.Add(order);
            }

            // Для каждого заказа получаем элементы
            foreach (var order in orders)
            {
                order.OrderItems = GetOrderItems(order.OrderId, connection);
            }

            return orders;
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при получении списка заказов клиента", ex);
        }
    }

    // Дополнительные методы для работы с заказами
    public bool CancelOrder(int orderId)
    {
        if (orderId <= 0)
            throw new ArgumentException("Неверный ID заказа");

        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            // 1. Проверяем текущий статус заказа
            var statusSql = "SELECT status FROM orders WHERE order_id = @orderId FOR UPDATE;";
            using var statusCmd = new NpgsqlCommand(statusSql, connection, transaction);
            statusCmd.Parameters.AddWithValue("@orderId", orderId);
            var currentStatus = statusCmd.ExecuteScalar()?.ToString();

            if (currentStatus == null)
                throw new Exception("Заказ не найден");

            if (currentStatus == OrderStatus.Shipped.ToString() ||
                currentStatus == OrderStatus.Delivered.ToString())
            {
                transaction.Rollback();
                throw new Exception("Невозможно отменить уже отправленный или доставленный заказ");
            }

            // 2. Возвращаем книги на склад
            var itemsSql = "SELECT book_id, quantity FROM order_items WHERE order_id = @orderId;";
            using var itemsCmd = new NpgsqlCommand(itemsSql, connection, transaction);
            itemsCmd.Parameters.AddWithValue("@orderId", orderId);

            using var reader = itemsCmd.ExecuteReader();
            var items = new List<OrderItem>();
            while (reader.Read())
            {
                items.Add(new OrderItem
                {
                    BookId = reader.GetInt32(0),
                    Quantity = reader.GetInt32(1)
                });
            }
            reader.Close();

            foreach (var item in items)
            {
                var updateSql = "UPDATE books SET available_quantity = available_quantity + @quantity WHERE book_id = @bookId;";
                using var updateCmd = new NpgsqlCommand(updateSql, connection, transaction);
                updateCmd.Parameters.AddWithValue("@quantity", item.Quantity);
                updateCmd.Parameters.AddWithValue("@bookId", item.BookId);
                updateCmd.ExecuteNonQuery();
            }

            // 3. Обновляем статус заказа
            var updateStatusSql = "UPDATE orders SET status = @status WHERE order_id = @orderId;";
            using var updateStatusCmd = new NpgsqlCommand(updateStatusSql, connection, transaction);
            updateStatusCmd.Parameters.AddWithValue("@status", OrderStatus.Cancelled.ToString());
            updateStatusCmd.Parameters.AddWithValue("@orderId", orderId);
            var result = updateStatusCmd.ExecuteNonQuery() > 0;

            transaction.Commit();
            return result;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw new Exception("Ошибка при отмене заказа", ex);
        }
    }

    public List<Order> GetAllOrders(DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var orders = new List<Order>();

            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var sql = @"SELECT o.order_id, o.customer_id, o.order_date, o.required_date, 
                               o.shipped_date, o.status, o.total_amount,
                               c.first_name, c.last_name
                        FROM orders o
                        JOIN customers c ON o.customer_id = c.customer_id
                        WHERE 1=1";

            if (fromDate.HasValue)
                sql += " AND o.order_date >= @fromDate";
            if (toDate.HasValue)
                sql += " AND o.order_date <= @toDate";

            sql += " ORDER BY o.order_date DESC;";

            using var cmd = new NpgsqlCommand(sql, connection);
            if (fromDate.HasValue)
                cmd.Parameters.AddWithValue("@fromDate", fromDate.Value);
            if (toDate.HasValue)
                cmd.Parameters.AddWithValue("@toDate", toDate.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var order = new Order
                {
                    OrderId = reader.GetInt32(0),
                    CustomerId = reader.GetInt32(1),
                    OrderDate = reader.GetDateTime(2),
                    RequiredDate = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                    ShippedDate = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
                    Status = (OrderStatus)Enum.Parse(typeof(OrderStatus), reader.GetString(5)),
                    TotalAmount = reader.GetDecimal(6),
                    Customer = new Customer
                    {
                        CustomerId = reader.GetInt32(1),
                        FirstName = reader.GetString(7),
                        LastName = reader.GetString(8)
                    }
                };
                orders.Add(order);
            }

            // Для каждого заказа получаем элементы
            foreach (var order in orders)
            {
                order.OrderItems = GetOrderItems(order.OrderId, connection);
            }

            return orders;
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при получении списка заказов", ex);
        }
    }
}