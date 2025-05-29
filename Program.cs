using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        // Инициализация подключения к базе данных
        var db = new BookStoreDatabase("localhost", "Library", "postgres", "");

        try
        {
            // Проверка подключения к базе данных
            if (!db.TestConnection())
            {
                Console.WriteLine("Не удалось подключиться к базе данных");
                return;
            }

            bool exitRequested = false;
            while (!exitRequested)
            {
                Console.Clear();
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
                Console.WriteLine("10. Выход");
                Console.Write("Выберите действие: ");

                var input = Console.ReadLine();
                Console.Clear();

                try
                {
                    switch (input)
                    {
                        case "1":
                            PrintBooksTableWithAuthors(db.GetBooksWithAuthors());
                            break;
                        case "2":
                            AddNewBook(db);
                            break;
                        case "3":
                            AddNewAuthor(db);
                            break;
                        case "4":
                            SearchBooksByAuthor(db);
                            break;
                        case "5":
                            UpdateBookInfo(db);
                            break;
                        case "6":
                            DeleteBook(db);
                            break;
                        case "7":
                            ShowAllAuthors(db);
                            break;
                        case "8":
                            DeleteAuthor(db);
                            break;
                        case "9":
                            ShowBooksWithoutAuthors(db);
                            break;
                        case "10":
                            exitRequested = true;
                            continue;
                        default:
                            throw new InvalidOperationException("Некорректный пункт меню. Введите число от 1 до 10.");
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Ошибка: {ex.Message}");
                    Console.ResetColor();
                }

                Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                Console.ReadKey();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Критическая ошибка: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }
    }

    #region Вспомогательные методы для отображения данных

    static void ShowBooksWithoutAuthors(BookStoreDatabase db)
    {
        try
        {
            var books = db.GetBooksWithoutAuthors();
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
                Console.Write("Введите ID книги: ");
                if (int.TryParse(Console.ReadLine(), out var bookId))
                {
                    Console.WriteLine("Доступные авторы:");
                    ShowAllAuthors(db);

                    Console.Write("Введите ID автора: ");
                    if (int.TryParse(Console.ReadLine(), out var authorId))
                    {
                        if (db.AddAuthorToBook(bookId, authorId))
                        {
                            Console.WriteLine("Автор успешно добавлен к книге!");
                        }
                        else
                        {
                            Console.WriteLine("Не удалось добавить автора к книге.");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при работе с книгами без авторов", ex);
        }
    }

    static void DeleteAuthor(BookStoreDatabase db)
    {
        try
        {
            Console.WriteLine("=== Удаление автора ===");
            var authors = db.GetAllAuthors();
            if (authors.Count == 0)
            {
                throw new InvalidOperationException("В базе нет авторов.");
            }

            ShowAllAuthors(db);

            Console.Write("\nВведите ID автора для удаления: ");
            if (!int.TryParse(Console.ReadLine(), out var authorId))
            {
                throw new ArgumentException("Некорректный ID автора.");
            }

            if (db.DeleteAuthor(authorId))
            {
                Console.WriteLine("\nАвтор успешно удален.");
                ShowAllAuthors(db);
            }
            else
            {
                Console.WriteLine("\nАвтор с указанным ID не найден или не может быть удален (есть связанные книги).");
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при удалении автора", ex);
        }
    }

    static void ShowAllAuthors(BookStoreDatabase db)
    {
        try
        {
            var authors = db.GetAllAuthors();
            if (authors.Count == 0)
            {
                throw new InvalidOperationException("В базе нет авторов.");
            }

            Console.WriteLine("Список авторов:");
            Console.WriteLine(new string('-', 50));
            Console.WriteLine("| {0,-5} | {1,-20} | {2,-20} |", "ID", "Имя", "Фамилия");
            Console.WriteLine(new string('-', 50));

            foreach (var author in authors)
            {
                Console.WriteLine("| {0,-5} | {1,-20} | {2,-20} |",
                                author.AuthorId,
                                author.FirstName,
                                author.LastName);
            }
            Console.WriteLine(new string('-', 50));
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при получении списка авторов", ex);
        }
    }

    static void AddNewAuthor(BookStoreDatabase db)
    {
        try
        {
            Console.WriteLine("=== Добавление нового автора ===");

            Console.Write("Имя: ");
            var firstName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("Имя автора не может быть пустым.");

            Console.Write("Фамилия: ");
            var lastName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Фамилия автора не может быть пустой.");

            Console.Write("Биография (необязательно): ");
            var bio = Console.ReadLine();

            var authorId = db.AddAuthor(firstName, lastName, bio);
            Console.WriteLine($"\nАвтор успешно добавлен с ID: {authorId}");
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при добавлении автора", ex);
        }
    }

    static void AddNewBook(BookStoreDatabase db)
    {
        try
        {
            Console.WriteLine("=== Добавление новой книги ===");

            var authors = db.GetAllAuthors();
            if (authors.Count == 0)
                throw new InvalidOperationException("Сначала нужно добавить авторов!");

            Console.WriteLine("\nСписок доступных авторов:");
            ShowAllAuthors(db);

            Console.Write("\nНазвание: ");
            var title = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Название книги не может быть пустым.");

            Console.Write("ISBN: ");
            var isbn = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(isbn))
                throw new ArgumentException("ISBN не может быть пустым.");

            Console.Write("Цена: ");
            if (!decimal.TryParse(Console.ReadLine(), out var price) || price <= 0)
                throw new ArgumentException("Цена должна быть положительным числом.");

            Console.Write("Издательство (необязательно): ");
            var publisher = Console.ReadLine();

            Console.Write("Дата публикации (ГГГГ-ММ-ДД, необязательно): ");
            DateTime? pubDate = null;
            var dateInput = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(dateInput))
            {
                if (!DateTime.TryParse(dateInput, out var tempDate))
                    throw new ArgumentException("Некорректный формат даты. Используйте ГГГГ-ММ-ДД.");
                pubDate = tempDate;
            }

            Console.Write("ID автора: ");
            if (!int.TryParse(Console.ReadLine(), out var authorId) || !authors.Exists(a => a.AuthorId == authorId))
                throw new ArgumentException("Некорректный ID автора.");

            var bookId = db.AddBook(title, isbn, price, publisher, pubDate, authorId);
            Console.WriteLine($"\nКнига успешно добавлена с ID: {bookId}");
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при добавлении книги", ex);
        }
    }

    static void SearchBooksByAuthor(BookStoreDatabase db)
    {
        try
        {
            Console.WriteLine("=== Поиск книг по автору ===");

            var authors = db.GetAllAuthors();
            if (authors.Count == 0)
                throw new InvalidOperationException("В базе нет авторов.");

            ShowAllAuthors(db);
            Console.Write("\nВведите ID автора: ");
            if (!int.TryParse(Console.ReadLine(), out var authorId))
                throw new ArgumentException("Некорректный ID автора.");

            var author = authors.Find(a => a.AuthorId == authorId);
            if (author == null)
                throw new KeyNotFoundException("Автор с таким ID не найден.");

            var books = db.GetBooksByAuthorId(authorId);
            if (books.Count > 0)
            {
                Console.WriteLine($"\nКниги автора {author.FirstName} {author.LastName}:");
                PrintBooksTable(books);
            }
            else
            {
                throw new InvalidOperationException("\nУ этого автора нет книг в базе.");
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при поиске книг", ex);
        }
    }

    static void UpdateBookInfo(BookStoreDatabase db)
    {
        try
        {
            Console.WriteLine("=== Обновление информации о книге ===");

            var books = db.GetBooksWithAuthors();
            if (books.Count == 0)
                throw new InvalidOperationException("В базе нет книг.");

            PrintBooksTableWithAuthors(books);

            Console.Write("\nВведите ID книги для обновления: ");
            if (!int.TryParse(Console.ReadLine(), out var bookId))
                throw new ArgumentException("Некорректный ID книги.");

            Console.Write("Новое название: ");
            var title = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Название книги не может быть пустым.");

            Console.Write("Новая цена: ");
            if (!decimal.TryParse(Console.ReadLine(), out var price) || price <= 0)
                throw new ArgumentException("Цена должна быть положительным числом.");

            if (db.UpdateBook(bookId, title, price))
            {
                Console.WriteLine("\nИнформация о книге успешно обновлена.");
                PrintBooksTableWithAuthors(db.GetBooksWithAuthors());
            }
            else
            {
                throw new KeyNotFoundException("Книга с указанным ID не найдена.");
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при обновлении книги", ex);
        }
    }

    static void DeleteBook(BookStoreDatabase db)
    {
        try
        {
            Console.WriteLine("=== Удаление книги ===");

            var books = db.GetBooksWithAuthors();
            if (books.Count == 0)
                throw new InvalidOperationException("В базе нет книг.");

            PrintBooksTableWithAuthors(books);

            Console.Write("\nВведите ID книги для удаления: ");
            if (!int.TryParse(Console.ReadLine(), out var bookId))
                throw new ArgumentException("Некорректный ID книги.");

            if (db.DeleteBook(bookId))
            {
                Console.WriteLine("\nКнига успешно удалена.");
                PrintBooksTableWithAuthors(db.GetBooksWithAuthors());
            }
            else
            {
                throw new KeyNotFoundException("Книга с указанным ID не найдена.");
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Ошибка при удалении книги", ex);
        }
    }

    static void PrintBooksTableWithAuthors(List<BookWithAuthor> books)
    {
        if (books.Count == 0)
            throw new InvalidOperationException("В базе данных нет книг.");

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

    static void PrintBooksTable(List<Book> books)
    {
        if (books.Count == 0)
            throw new InvalidOperationException("Нет книг для отображения.");

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

    static string Truncate(string value, int maxLength)
    {
        return string.IsNullOrEmpty(value)
            ? value
            : value.Length <= maxLength
                ? value
                : value.Substring(0, maxLength - 3) + "...";
    }

    #endregion
}