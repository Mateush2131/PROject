# sql_lib


Зависимость:

csharp
private readonly BookStoreDatabase _database;
Класс получает экземпляр базы данных через конструктор (Dependency Injection).

Операции с книгами:

GetAllBooksWithAuthors() - возвращает все книги с информацией об авторах

AddBook() - добавляет новую книгу с валидацией

UpdateBook() - обновляет информацию о книге

Операции с авторами:

GetAllAuthors() - возвращает всех авторов

AddAuthor() - добавляет нового автора с валидацией

Вспомогательные методы:

GetBookById() и GetAuthorById() - получают объекты по ID

ValidateBook() и ValidateAuthor() - проверяют входные данные

Особенности:
Использует проверки на null и валидацию входных данных

Генерирует исключения при ошибках (InvalidOperationException, KeyNotFoundException, ArgumentException)

Возвращает составные объекты (BookWithAuthor), содержащие информацию и о книге, и об авторе

Следует принципу единственной ответственности (SRP)
