using Microsoft.EntityFrameworkCore;



namespace BookStore
{
	public class Book
	{
		public int Id { get; set; }
		public string Author { get; set; } = string.Empty;
		public string Title { get; set; } = string.Empty;
		public DateTime Year { get; set; }
		public int Count { get; set; }
	}

	public class BookStoreContext : DbContext
	{
		public DbSet<Book> Books { get; set; } = null!;

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseNpgsql("Host=localhost;Database=bookstore;Username=postgres;Password=yourpassword");
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Book>().HasData(
				new Book { Id = 1, Author = "Author 1", Title = "Book 1", Year = new DateTime(2020, 1, 1), Count = 10 },
				new Book { Id = 2, Author = "Author 2", Title = "Book 2", Year = new DateTime(2021, 5, 10), Count = 5 }
			);
		}
	}

	internal class Program
	{
		private static void Main(string[] args)
		{
			using var context = new BookStoreContext();
			//context.Database.Migrate(); // Apply migrations
			Console.WriteLine("Available commands: get, buy, restock");
			string? a = Console.ReadLine();
			if (a.Length == 0)
			{
				return;
			}
			switch (a.ToLower())
			{
				case "get":
					HandleGetCommand(args.Skip(1).ToArray());
					break;

				case "buy":
					HandleBuyCommand(args.Skip(1).ToArray());
					break;

				case "restock":
					HandleRestockCommand(args.Skip(1).ToArray());
					break;

				default:
					Console.WriteLine("Unknown command.");
					break;
			}
		}

		private static void HandleGetCommand(string[] args)
		{
			string? title = GetFlagValue(args, "--title");
			string? author = GetFlagValue(args, "--author");
			string? date = GetFlagValue(args, "--date");
			string? orderBy = GetFlagValue(args, "--order-by");

			using var context = new BookStoreContext();
			var query = context.Books.AsQueryable();

			if (!string.IsNullOrEmpty(title))
				query = query.Where(b => b.Title.Contains(title, StringComparison.OrdinalIgnoreCase));

			if (!string.IsNullOrEmpty(author))
				query = query.Where(b => b.Author.Contains(author, StringComparison.OrdinalIgnoreCase));

			if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsedDate))
				query = query.Where(b => b.Year.Date == parsedDate.Date);
			else if (!string.IsNullOrEmpty(date))
			{
				Console.WriteLine("Invalid date format. Use yyyy-MM-dd.");
				return;
			}

			if (!string.IsNullOrEmpty(orderBy))
			{
				query = orderBy.ToLower() switch
				{
					"title" => query.OrderBy(b => b.Title),
					"author" => query.OrderBy(b => b.Author),
					"date" => query.OrderBy(b => b.Year),
					"count" => query.OrderBy(b => b.Count),
					_ => throw new ArgumentException("Invalid order-by field")
				};
			}

			var books = query.ToList();
			foreach (var book in books)
			{
				Console.WriteLine($"{book.Id} | {book.Title} | {book.Author} | {book.Year:yyyy-MM-dd} | {book.Count}");
			}

			if (!books.Any())
			{
				Console.WriteLine("No books found matching the criteria.");
			}
		}
		private static void HandleBuyCommand(string[] args)
		{
			int? id = GetFlagValueAsInt(args, "--id");
			if (id == null)
			{
				Console.WriteLine("Please specify a valid --id flag.");
				return;
			}

			using var context = new BookStoreContext();
			var book = context.Books.FirstOrDefault(b => b.Id == id);

			if (book == null)
			{
				Console.WriteLine("Book not found.");
				return;
			}

			if (book.Count > 0)
			{
				book.Count--;
				context.SaveChanges();
				Console.WriteLine($"Book '{book.Title}' bought successfully.");
			}
			else
			{
				Console.WriteLine("Book is out of stock.");
			}
		}

		private static void HandleRestockCommand(string[] args)
		{
			int? id = GetFlagValueAsInt(args, "--id");
			int? count = GetFlagValueAsInt(args, "--count");

			using var context = new BookStoreContext();

			if (id.HasValue)
			{
				var book = context.Books.FirstOrDefault(b => b.Id == id.Value);
				if (book != null)
				{
					book.Count += count ?? new Random().Next(1, 10);
					context.SaveChanges();
					Console.WriteLine($"Book '{book.Title}' restocked successfully.");
				}
				else
				{
					Console.WriteLine("Book not found.");
				}
			}
			else
			{
				var randomBook = context.Books.OrderBy(b => Guid.NewGuid()).FirstOrDefault();
				if (randomBook != null)
				{
					randomBook.Count += new Random().Next(1, 10);
					context.SaveChanges();
					Console.WriteLine($"Book '{randomBook.Title}' restocked successfully.");
				}
				else
				{
					Console.WriteLine("No books available to restock.");
				}
			}
		}

		private static string? GetFlagValue(string[] args, string flag)
		{
			var arg = args.FirstOrDefault(a => a.StartsWith(flag));
			return arg?.Split('=')[1];
		}

		private static int? GetFlagValueAsInt(string[] args, string flag)
		{
			var value = GetFlagValue(args, flag);
			return int.TryParse(value, out var result) ? result : null;
		}
	}
}