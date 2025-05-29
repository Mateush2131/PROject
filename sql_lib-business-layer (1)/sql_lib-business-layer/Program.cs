using System;
using System.Collections.Generic;

class Program
{
    private static BookStoreService _bookStoreService;

    static void Main(string[] args)
    {
        try
        {
            // Инициализация сервиса
            var db = new BookStoreDatabase("localhost", "Lib", "postgres", "");
            _bookStoreService = new BookStoreService(db);

            // Проверка подключения
            if (!db.TestConnection())
            {
                Console.WriteLine("Не удалось подключиться к базе данных");
                return;
            }

            bool exitRequested = false;
            while (!exitRequested)
            {
                Console.Clear();
                DisplayMainMenu();
                
                var input = Console.ReadLine();
                Console.Clear();

                try
                {
                    switch (input)
                    {
                        case "1": ShowAllBooksWithAuthors(); break;
                        case "2": AddNewBook(); break;
                        case "3": AddNewAuthor(); break;
                        case "4": SearchBooksByAuthor(); break;
                        case "5": UpdateBookInfo(); break;
                        case "6": DeleteBook(); break;
                        case "7": ShowAllAuthors(); break;
                        case "8": DeleteAuthor(); break;
                        case "9": ShowBooksWithoutAuthors(); break;
                        case "10": ManageCustomers(); break;
                        case "11": ManageOrders(); break;
                        case "12": exitRequested = true; break;
                        default:
                            throw new InvalidOperationException("Некорректный пункт меню. Введите число от 1 до 12.");
                    }
                }
                catch (Exception ex)
                {
                    DisplayError(ex.Message);
                }

                if (!exitRequested)
                {
                    Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                    Console.ReadKey();
                }
            }
        }
        catch (Exception ex)
        {
            DisplayError($"Критическая ошибка: {ex.Message}", true);
            Console.ReadKey();
        }
    }

    #region Main Menu
    private static void DisplayMainMenu()
    {
        Console.WriteLine("=== Система управления книжным магазином ===");
        Console.WriteLine("1. Показать все книги с авторами");
        Console.WriteLine("2. Добавить книгу");
        Console.WriteLine("3. Добавить автора");
        Console.WriteLine("4. Найти книгу по автору");
        Console.WriteLine("5. Обновить информацию о книге");
        Console.WriteLine("6. Удалить книгу");
        Console.WriteLine("7. Показать всех авторов");
        Console.WriteLine("8. Удалить автора");
        Console.WriteLine("9. Показать книги без авторов");
        Console.WriteLine("10. Управление клиентами");
        Console.WriteLine("11. Управление заказами");
        Console.WriteLine("12. Выход");
        Console.Write("Выберите действие: ");
    }
    #endregion

    #region Book Management
    private static void ShowAllBooksWithAuthors()
{
    try
    {
        var books = _bookStoreService.GetAllBooksWithAuthors();
        if (books.Count == 0)
        {
            Console.WriteLine("No books found.");
            return;
        }

        PrintBooksTableWithAuthors(books);
    }
    catch (Exception ex)
    {
        DisplayError(ex.Message);
    }
}

    private static void AddNewBook()
    {
        Console.WriteLine("=== Добавление новой книги ===");

        // Получаем список авторов для справки
        var authors = _bookStoreService.GetAllAuthors();
        PrintAuthorsTable(authors);

        // Ввод данных книги
        var title = ReadNonEmptyString("Название: ", "Название книги не может быть пустым");
        var isbn = ReadNonEmptyString("ISBN: ", "ISBN не может быть пустым");
        var price = ReadDecimal("Цена: ", "Цена должна быть положительным числом");
        var publisher = ReadOptionalString("Издательство (необязательно): ");
        var pubDate = ReadOptionalDate("Дата публикации (ГГГГ-ММ-ДД, необязательно): ");
        var authorId = ReadInt("ID автора: ", "Некорректный ID автора");

        // Добавление книги
        var book = _bookStoreService.AddBook(title, isbn, price, publisher, pubDate, authorId);
        Console.WriteLine($"\nКнига успешно добавлена с ID: {book.BookId}");
        PrintBookDetails(book);
    }

    private static void UpdateBookInfo()
    {
        Console.WriteLine("=== Обновление информации о книге ===");
        var books = _bookStoreService.GetAllBooksWithAuthors();
        PrintBooksTableWithAuthors(books);

        var bookId = ReadInt("\nВведите ID книги для обновления: ", "Некорректный ID книги");
        var title = ReadNonEmptyString("Новое название: ", "Название книги не может быть пустым");
        var price = ReadDecimal("Новая цена: ", "Цена должна быть положительным числом");

        var updatedBook = _bookStoreService.UpdateBook(bookId, title, price);
        Console.WriteLine("\nИнформация о книге успешно обновлена:");
        PrintBookDetails(updatedBook);
    }

    private static void DeleteBook()
    {
        Console.WriteLine("=== Удаление книги ===");
        var books = _bookStoreService.GetAllBooksWithAuthors();
        PrintBooksTableWithAuthors(books);

        var bookId = ReadInt("\nВведите ID книги для удаления: ", "Некорректный ID книги");
        _bookStoreService.DeleteBook(bookId);
        Console.WriteLine("\nКнига успешно удалена.");
    }

    private static void SearchBooksByAuthor()
    {
        Console.WriteLine("=== Поиск книг по автору ===");
        var authors = _bookStoreService.GetAllAuthors();
        PrintAuthorsTable(authors);

        var authorId = ReadInt("\nВведите ID автора: ", "Некорректный ID автора");
        var books = _bookStoreService.GetBooksByAuthor(authorId);
        
        Console.WriteLine($"\nНайдено {books.Count} книг:");
        PrintBooksTable(books);
    }

    private static void ShowBooksWithoutAuthors()
    {
        var books = _bookStoreService.GetBooksWithoutAuthors();
        
        if (books.Count == 0)
        {
            Console.WriteLine("Все книги имеют авторов.");
            return;
        }

        Console.WriteLine("=== Книги без авторов ===");
        PrintBooksTable(books);

        Console.Write("\nХотите добавить автора к книге? (y/n): ");
        if (Console.ReadLine()?.ToLower() == "y")
        {
            var bookId = ReadInt("Введите ID книги: ", "Некорректный ID книги");
            
            var authors = _bookStoreService.GetAllAuthors();
            PrintAuthorsTable(authors);
            
            var authorId = ReadInt("Введите ID автора: ", "Некорректный ID автора");
            
            if (_bookStoreService.AddAuthorToBook(bookId, authorId))
            {
                Console.WriteLine("Автор успешно добавлен к книге!");
            }
            else
            {
                Console.WriteLine("Не удалось добавить автора к книге.");
            }
        }
    }
    #endregion

    #region Author Management
    private static void ShowAllAuthors()
    {
        var authors = _bookStoreService.GetAllAuthors();
        PrintAuthorsTable(authors);
    }

    private static void AddNewAuthor()
    {
        Console.WriteLine("=== Добавление нового автора ===");

        var firstName = ReadNonEmptyString("Имя: ", "Имя автора не может быть пустым");
        var lastName = ReadNonEmptyString("Фамилия: ", "Фамилия автора не может быть пустой");
        var bio = ReadOptionalString("Биография (необязательно): ");

        var author = _bookStoreService.AddAuthor(firstName, lastName, bio);
        Console.WriteLine($"\nАвтор успешно добавлен с ID: {author.AuthorId}");
        PrintAuthorDetails(author);
    }

    private static void DeleteAuthor()
    {
        Console.WriteLine("=== Удаление автора ===");
        var authors = _bookStoreService.GetAllAuthors();
        PrintAuthorsTable(authors);

        var authorId = ReadInt("\nВведите ID автора для удаления: ", "Некорректный ID автора");
        
        try
        {
            _bookStoreService.DeleteAuthor(authorId);
            Console.WriteLine("\nАвтор успешно удален.");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"\nОшибка: {ex.Message}");
            Console.WriteLine("Вы можете:");
            Console.WriteLine("1. Удалить все книги автора и затем удалить автора");
            Console.WriteLine("2. Переназначить книги другому автору");
            Console.Write("Выберите действие (1/2): ");
            
            var choice = Console.ReadLine();
            if (choice == "1")
            {
                // Удаление всех книг автора
                var books = _bookStoreService.GetBooksByAuthor(authorId);
                foreach (var book in books)
                {
                    _bookStoreService.DeleteBook(book.BookId);
                }
                _bookStoreService.DeleteAuthor(authorId);
                Console.WriteLine("Автор и все его книги удалены.");
            }
            else if (choice == "2")
            {
                // Переназначение книг другому автору
                var newAuthorId = ReadInt("Введите ID нового автора: ", "Некорректный ID автора");
                var books = _bookStoreService.GetBooksByAuthor(authorId);
                
                foreach (var book in books)
                {
                    _bookStoreService.AddAuthorToBook(book.BookId, newAuthorId);
                }
                
                _bookStoreService.DeleteAuthor(authorId);
                Console.WriteLine("Книги переназначены, автор удален.");
            }
        }
    }
    #endregion

    #region Customer Management
    private static void ManageCustomers()
    {
        bool backToMain = false;
        while (!backToMain)
        {
            Console.Clear();
            Console.WriteLine("=== Управление клиентами ===");
            Console.WriteLine("1. Показать всех клиентов");
            Console.WriteLine("2. Добавить клиента");
            Console.WriteLine("3. Найти клиента по ID");
            Console.WriteLine("4. Вернуться в главное меню");
            Console.Write("Выберите действие: ");

            var input = Console.ReadLine();
            Console.Clear();

            try
            {
                switch (input)
                {
                    case "1": ShowAllCustomers(); break;
                    case "2": AddNewCustomer(); break;
                    case "3": FindCustomerById(); break;
                    case "4": backToMain = true; break;
                    default:
                        throw new InvalidOperationException("Некорректный пункт меню.");
                }
            }
            catch (Exception ex)
            {
                DisplayError(ex.Message);
            }

            if (!backToMain)
            {
                Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                Console.ReadKey();
            }
        }
    }

    private static void ShowAllCustomers()
    {
        // В реальной реализации нужно добавить метод GetAllCustomers в сервис
        Console.WriteLine("Функция показа всех клиентов будет реализована в следующей версии.");
    }

    private static void AddNewCustomer()
    {
        Console.WriteLine("=== Добавление нового клиента ===");

        var firstName = ReadNonEmptyString("Имя: ", "Имя клиента не может быть пустым");
        var lastName = ReadNonEmptyString("Фамилия: ", "Фамилия клиента не может быть пустой");
        var email = ReadOptionalString("Email (необязательно): ");
        var phone = ReadOptionalString("Телефон (необязательно): ");
        var address = ReadOptionalString("Адрес (необязательно): ");

        if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phone))
        {
            throw new ArgumentException("Необходимо указать либо email, либо телефон");
        }

        var customer = _bookStoreService.AddCustomer(firstName, lastName, email, phone, address);
        Console.WriteLine($"\nКлиент успешно добавлен с ID: {customer.CustomerId}");
        PrintCustomerDetails(customer);
    }

    private static void FindCustomerById()
    {
        var customerId = ReadInt("Введите ID клиента: ", "Некорректный ID клиента");
        var customer = _bookStoreService.GetCustomer(customerId);
        PrintCustomerDetails(customer);
    }
    #endregion

    #region Order Management
    private static void ManageOrders()
    {
        bool backToMain = false;
        while (!backToMain)
        {
            Console.Clear();
            Console.WriteLine("=== Управление заказами ===");
            Console.WriteLine("1. Показать все заказы");
            Console.WriteLine("2. Создать новый заказ");
            Console.WriteLine("3. Найти заказ по ID");
            Console.WriteLine("4. Обновить статус заказа");
            Console.WriteLine("5. Отменить заказ");
            Console.WriteLine("6. Вернуться в главное меню");
            Console.Write("Выберите действие: ");

            var input = Console.ReadLine();
            Console.Clear();

            try
            {
                switch (input)
                {
                    case "1": ShowAllOrders(); break;
                    case "2": CreateNewOrder(); break;
                    case "3": FindOrderById(); break;
                    case "4": UpdateOrderStatus(); break;
                    case "5": CancelOrder(); break;
                    case "6": backToMain = true; break;
                    default:
                        throw new InvalidOperationException("Некорректный пункт меню.");
                }
            }
            catch (Exception ex)
            {
                DisplayError(ex.Message);
            }

            if (!backToMain)
            {
                Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                Console.ReadKey();
            }
        }
    }

    private static void ShowAllOrders()
    {
        var fromDate = ReadOptionalDate("Начиная с даты (ГГГГ-ММ-ДД, необязательно): ");
        var toDate = ReadOptionalDate("Заканчивая датой (ГГГГ-ММ-ДД, необязательно): ");
        
        var orders = _bookStoreService.GetAllOrders(fromDate, toDate);
        
        if (orders.Count == 0)
        {
            Console.WriteLine("Заказы не найдены.");
            return;
        }

        Console.WriteLine($"Найдено {orders.Count} заказов:");
        foreach (var order in orders)
        {
            PrintOrderDetails(order);
            Console.WriteLine(new string('-', 50));
        }
    }

    private static void CreateNewOrder()
    {
        Console.WriteLine("=== Создание нового заказа ===");
        
        // Выбор клиента
        // В реальной реализации нужно сначала показать список клиентов
        var customerId = ReadInt("ID клиента: ", "Некорректный ID клиента");
        
        // Показать доступные книги
        var books = _bookStoreService.GetAllBooksWithAuthors();
        PrintBooksTableWithAuthors(books);
        
        // Создание списка товаров
        var items = new List<OrderItem>();
        bool addingItems = true;
        
        while (addingItems)
        {
            var bookId = ReadInt("ID книги (0 для завершения): ", "Некорректный ID книги");
            
            if (bookId == 0)
            {
                addingItems = false;
                continue;
            }
            
            var quantity = ReadInt("Количество: ", "Количество должно быть положительным числом", minValue: 1);
            
            items.Add(new OrderItem { BookId = bookId, Quantity = quantity });
        }

        if (items.Count == 0)
        {
            throw new InvalidOperationException("Заказ должен содержать хотя бы один товар");
        }
        
        var requiredDate = ReadFutureDate("Желаемая дата получения (ГГГГ-ММ-ДД): ");
        
        var order = _bookStoreService.CreateOrder(customerId, requiredDate, items);
        Console.WriteLine($"\nЗаказ успешно создан с ID: {order.OrderId}");
        PrintOrderDetails(order);
    }

    private static void FindOrderById()
    {
        var orderId = ReadInt("Введите ID заказа: ", "Некорректный ID заказа");
        var order = _bookStoreService.GetOrder(orderId);
        PrintOrderDetails(order);
    }

    private static void UpdateOrderStatus()
    {
        var orderId = ReadInt("Введите ID заказа: ", "Некорректный ID заказа");
        
        Console.WriteLine("Доступные статусы:");
        foreach (var status in Enum.GetValues(typeof(OrderStatus)))
        {
            Console.WriteLine($"- {status}");
        }
        
        var newStatus = ReadEnum<OrderStatus>("Новый статус: ", "Некорректный статус");
        _bookStoreService.UpdateOrderStatus(orderId, newStatus);
        
        Console.WriteLine("\nСтатус заказа успешно обновлен:");
        PrintOrderDetails(_bookStoreService.GetOrder(orderId));
    }

    private static void CancelOrder()
    {
        var orderId = ReadInt("Введите ID заказа: ", "Некорректный ID заказа");
        _bookStoreService.CancelOrder(orderId);
        Console.WriteLine("\nЗаказ успешно отменен:");
        PrintOrderDetails(_bookStoreService.GetOrder(orderId));
    }
    #endregion

    #region Display Methods
    private static void PrintBooksTableWithAuthors(List<BookWithAuthor> books)
    {
        Console.WriteLine("\nСписок книг в магазине (с авторами):");
        Console.WriteLine(new string('-', 110));
        Console.WriteLine("| {0,-5} | {1,-25} | {2,-15} | {3,-10} | {4,-15} | {5,-20} |",
                         "ID", "Название", "ISBN", "Цена", "Издательство", "Автор");
        Console.WriteLine(new string('-', 110));

        foreach (var book in books)
        {
            Console.WriteLine("| {0,-5} | {1,-25} | {2,-15} | {3,-10:C2} | {4,-15} | {5,-20} |",
                            book.BookId,
                            Truncate(book.Title, 25),
                            book.ISBN,
                            book.Price,
                            Truncate(book.Publisher ?? "-", 15),
                            book.AuthorId == 0 ? "Нет автора" : $"{book.AuthorFirstName} {book.AuthorLastName}");
        }
        Console.WriteLine(new string('-', 110));
    }

    private static void PrintBooksTable(List<Book> books)
    {
        Console.WriteLine("\nСписок книг:");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine("| {0,-5} | {1,-30} | {2,-15} | {3,-10} | {4,-15} |",
                         "ID", "Название", "ISBN", "Цена", "Издательство");
        Console.WriteLine(new string('-', 80));

        foreach (var book in books)
        {
            Console.WriteLine("| {0,-5} | {1,-30} | {2,-15} | {3,-10:C2} | {4,-15} |",
                            book.BookId,
                            Truncate(book.Title, 30),
                            book.ISBN,
                            book.Price,
                            Truncate(book.Publisher ?? "-", 15));
        }
        Console.WriteLine(new string('-', 80));
    }

    private static void PrintAuthorsTable(List<Author> authors)
    {
        Console.WriteLine("\nСписок авторов:");
        Console.WriteLine(new string('-', 70));
        Console.WriteLine("| {0,-5} | {1,-20} | {2,-20} | {3,-20} |",
                         "ID", "Имя", "Фамилия", "Биография");
        Console.WriteLine(new string('-', 70));

        foreach (var author in authors)
        {
            Console.WriteLine("| {0,-5} | {1,-20} | {2,-20} | {3,-20} |",
                            author.AuthorId,
                            Truncate(author.FirstName, 20),
                            Truncate(author.LastName, 20),
                            Truncate(author.Biography ?? "-", 20));
        }
        Console.WriteLine(new string('-', 70));
    }

    private static void PrintBookDetails(BookWithAuthor book)
    {
        Console.WriteLine("\nИнформация о книге:");
        Console.WriteLine(new string('-', 50));
        Console.WriteLine($"ID: {book.BookId}");
        Console.WriteLine($"Название: {book.Title}");
        Console.WriteLine($"ISBN: {book.ISBN}");
        Console.WriteLine($"Цена: {book.Price:C2}");
        Console.WriteLine($"Издательство: {book.Publisher ?? "-"}");
        Console.WriteLine($"Дата публикации: {book.PublicationDate?.ToString("d") ?? "-"}");
        Console.WriteLine($"Автор: {(book.AuthorId == 0 ? "Нет автора" : $"{book.AuthorFirstName} {book.AuthorLastName}")}");
        Console.WriteLine(new string('-', 50));
    }

    private static void PrintAuthorDetails(Author author)
    {
        Console.WriteLine("\nИнформация об авторе:");
        Console.WriteLine(new string('-', 50));
        Console.WriteLine($"ID: {author.AuthorId}");
        Console.WriteLine($"Имя: {author.FirstName} {author.LastName}");
        Console.WriteLine($"Биография: {author.Biography ?? "-"}");
        Console.WriteLine(new string('-', 50));
    }

    private static void PrintCustomerDetails(Customer customer)
    {
        Console.WriteLine("\nИнформация о клиенте:");
        Console.WriteLine(new string('-', 50));
        Console.WriteLine($"ID: {customer.CustomerId}");
        Console.WriteLine($"Имя: {customer.FirstName} {customer.LastName}");
        Console.WriteLine($"Email: {customer.Email ?? "-"}");
        Console.WriteLine($"Телефон: {customer.Phone ?? "-"}");
        Console.WriteLine($"Адрес: {customer.Address ?? "-"}");
        Console.WriteLine(new string('-', 50));
    }

    private static void PrintOrderDetails(Order order)
    {
        Console.WriteLine("\nИнформация о заказе:");
        Console.WriteLine(new string('-', 50));
        Console.WriteLine($"ID: {order.OrderId}");
        Console.WriteLine($"Клиент: {order.Customer.FirstName} {order.Customer.LastName}");
        Console.WriteLine($"Дата заказа: {order.OrderDate:d}");
        Console.WriteLine($"Желаемая дата: {order.RequiredDate:d}");
        Console.WriteLine($"Статус: {order.Status}");
        Console.WriteLine($"Общая сумма: {order.TotalAmount:C2}");
        
        Console.WriteLine("\nТовары:");
        foreach (var item in order.OrderItems)
        {
            Console.WriteLine($"- {item.Book.Title} (ID: {item.BookId}), " +
                            $"Количество: {item.Quantity}, " +
                            $"Цена: {item.UnitPrice:C2}, " +
                            $"Сумма: {item.Quantity * item.UnitPrice:C2}");
        }
        Console.WriteLine(new string('-', 50));
    }

    private static void DisplayError(string message, bool isCritical = false)
    {
        var color = isCritical ? ConsoleColor.DarkRed : ConsoleColor.Red;
        Console.ForegroundColor = color;
        Console.WriteLine($"Ошибка: {message}");
        Console.ResetColor();
    }

    private static string Truncate(string value, int maxLength)
    {
        return string.IsNullOrEmpty(value)
            ? value
            : value.Length <= maxLength
                ? value
                : value.Substring(0, maxLength - 3) + "...";
    }
    #endregion

    #region Input Helpers
    private static string ReadNonEmptyString(string prompt, string errorMessage)
    {
        Console.Write(prompt);
        var input = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException(errorMessage);
        }
        
        return input.Trim();
    }

    private static string ReadOptionalString(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine()?.Trim();
    }

    private static int ReadInt(string prompt, string errorMessage, int minValue = 1)
    {
        Console.Write(prompt);
        if (!int.TryParse(Console.ReadLine(), out var result) || result < minValue)
        {
            throw new ArgumentException(errorMessage);
        }
        return result;
    }

    private static decimal ReadDecimal(string prompt, string errorMessage)
    {
        Console.Write(prompt);
        if (!decimal.TryParse(Console.ReadLine(), out var result) || result <= 0)
        {
            throw new ArgumentException(errorMessage);
        }
        return result;
    }

    private static DateTime? ReadOptionalDate(string prompt)
    {
        Console.Write(prompt);
        var input = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }
        
        if (!DateTime.TryParse(input, out var result))
        {
            throw new ArgumentException("Некорректный формат даты. Используйте ГГГГ-ММ-ДД.");
        }
        
        return result;
    }

    private static DateTime ReadFutureDate(string prompt)
    {
        var date = ReadOptionalDate(prompt);
        
        if (!date.HasValue)
        {
            throw new ArgumentException("Дата обязательна.");
        }
        
        if (date.Value < DateTime.Today)
        {
            throw new ArgumentException("Дата не может быть в прошлом.");
        }
        
        return date.Value;
    }

    private static T ReadEnum<T>(string prompt, string errorMessage) where T : struct
    {
        Console.Write(prompt);
        if (!Enum.TryParse(Console.ReadLine(), true, out T result))
        {
            throw new ArgumentException(errorMessage);
        }
        return result;
    }
    #endregion
}