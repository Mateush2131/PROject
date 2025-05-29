public class Book
{
    public int BookId { get; set; }
    public string Title { get; set; }
    public string ISBN { get; set; }
    public decimal Price { get; set; }
    public string Publisher { get; set; }
    public DateTime? PublicationDate { get; set; }
}

public class BookWithAuthor : Book
{
    public int AuthorId { get; set; }
    public string AuthorFirstName { get; set; }
    public string AuthorLastName { get; set; }
}

public class Author
{
    public int AuthorId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Biography { get; set; }
}