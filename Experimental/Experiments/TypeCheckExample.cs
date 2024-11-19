namespace MyNamespace;

public class TypeCheckExample {
    public static void Run() {
        Console.WriteLine(2147483647 is int);
        Console.WriteLine(2147483648 is uint);
        Console.WriteLine(21474836490 is long);
        Console.WriteLine(-2147483649 is long);
    }
}
