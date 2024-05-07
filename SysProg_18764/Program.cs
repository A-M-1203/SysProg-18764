using SysProg_18764;

public class Program
{
    public static async Task Main(string[] args)
    {
        HttpServer server = new HttpServer();
        await server.Start();
    }
}