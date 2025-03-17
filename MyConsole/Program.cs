// See https://aka.ms/new-console-template for more information

public class MyConsole
{
    static void Main(string[] args)
    {
        int c = Add2Number(10, 3);
        Console.WriteLine($"Total: {c}");
        var person = new { Name = "John", Age = 25 };
        Console.WriteLine($"Name: {person.Name}, Age: {person.Age}");
    }

    private static int Add2Number(int a, int b)
    {
        int c = a + b;
        return c;
    }
}