using BCrypt.Net;

Console.WriteLine("=== BCrypt Password Generator ===");
Console.WriteLine();

string password = "12345";
string hash = BCrypt.Net.BCrypt.HashPassword(password);

Console.WriteLine("Password: " + password);
Console.WriteLine();
Console.WriteLine("Generated Hash:");
Console.WriteLine(hash);
Console.WriteLine();
Console.WriteLine("Copy the hash above and use it in SQL!");
Console.WriteLine();
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
