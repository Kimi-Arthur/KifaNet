namespace MyNamespace;

public class ExceptionHandlingExample {
    public static void Run() {
        var i = 5;

        try {
            throw new IOException();
        } catch (Exception ex) {
            switch (ex) {
                case TaskCanceledException or IOException when i > 10:
                    Console.WriteLine("Specific exception with i > 10");
                    break;
                case TaskCanceledException or IOException:
                    Console.WriteLine("Specific exception");
                    break;
                default:
                    Console.WriteLine("Unhandled exception: " + ex.Message);
                    break;
            }
        }
    }
}
