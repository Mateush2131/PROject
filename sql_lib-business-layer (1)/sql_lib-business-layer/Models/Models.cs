using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;

public class Book
{
    public int BookId { get; set; }
    public string Title { get; set; }
    public string ISBN { get; set; }
    public decimal Price { get; set; }
    public string Publisher { get; set; }
    public DateTime? PublicationDate { get; set; }
    public int AvailableQuantity { get; set; } // Количество доступных экземпляров
    public List<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
    public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
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
    public List<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
}

// Связь многие-ко-многим между Book и Author
public class BookAuthor
{
    public int BookId { get; set; }
    public Book Book { get; set; }
    public int AuthorId { get; set; }
    public Author Author { get; set; }
}

public class Customer
{
    public int CustomerId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public List<Order> Orders { get; set; } = new List<Order>();
}

public class Order
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? RequiredDate { get; set; } // Дата, к которой нужен заказ
    public DateTime? ShippedDate { get; set; } // Дата отправки
    public OrderStatus Status { get; set; } // Статус заказа
    public decimal TotalAmount { get; set; }
    public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int OrderItemId { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }
    public int BookId { get; set; }
    public Book Book { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; } // Цена на момент заказа
}

public enum OrderStatus
{
    Pending,    // В ожидании
    Processing, // В обработке
    Shipped,    // Отправлен
    Delivered,  // Доставлен
    Cancelled   // Отменен
}

public class LibraryContext : DbContext
{
    public DbSet<Book> Books { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<BookAuthor> BookAuthors { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=localhost;Database=Library;Username=postgres;Password=your_password");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Настройка связи многие-ко-многим между Book и Author
        modelBuilder.Entity<BookAuthor>()
            .HasKey(ba => new { ba.BookId, ba.AuthorId });

        modelBuilder.Entity<BookAuthor>()
            .HasOne(ba => ba.Book)
            .WithMany(b => b.BookAuthors)
            .HasForeignKey(ba => ba.BookId);

        modelBuilder.Entity<BookAuthor>()
            .HasOne(ba => ba.Author)
            .WithMany(a => a.BookAuthors)
            .HasForeignKey(ba => ba.AuthorId);
    }
}