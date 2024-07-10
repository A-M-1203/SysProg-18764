using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

class Program
{
    private static readonly LRUCache<string, byte[]> fileCache = new LRUCache<string, byte[]>(5); // Keš sa kapacitetom od 5 stavki
    private static readonly object cacheLock = new object();

    static void Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Any, 8080);
        server.Start();
        Console.WriteLine("Listening...");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            ThreadPool.QueueUserWorkItem(HandleClient, client);
        }
    }

    private static void HandleClient(object clientObj)
    {
        TcpClient client = (TcpClient)clientObj;
        try
        {
            using (NetworkStream stream = client.GetStream())
            using (StreamReader reader = new StreamReader(stream))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                string request = reader.ReadLine();
                if (request.StartsWith("GET"))
                {
                    Console.WriteLine($"Request: {request}");
                    string[] tokens = request.Split(' ');
                    string fileName = tokens[1].TrimStart('/');

                    byte[] cachedFile;
                    lock (cacheLock)
                    {
                        fileCache.TryGet(fileName, out cachedFile);
                    }

                    if (cachedFile != null)
                    {
                        var thread = new Thread(() =>
                        {
                            SendResponse(writer, cachedFile, "application/octet-stream");
                        });
                        thread.Start();
                        thread.Join();
                    }
                    else if (File.Exists(fileName))
                    {
                        byte[] fileData = File.ReadAllBytes(fileName);
                        var thread = new Thread(() =>
                        {
                            byte[] hashFile = GenerateHash(fileData);
                            lock (cacheLock)
                            {
                                fileCache[fileName] = hashFile;
                            }
                            SendResponse(writer, hashFile, "application/octet-stream");
                        });
                        thread.Start();
                        thread.Join();
                    }
                    else
                    {
                        SendErrorResponse(writer, "File not found");
                    }
                }
                else
                {
                    SendErrorResponse(writer, "Method Not Allowed");
                }
            }
        }
        catch (Exception ec)
        {
            Console.WriteLine($"Error: {ec.Message}");
        }
        finally
        {
            client.Close();
        }
    }

    private static void SendResponse(BinaryWriter writer, byte[] fileData, string contentType)
    {
        writer.Write(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\n"));
        writer.Write(Encoding.ASCII.GetBytes($"Content-Type: {contentType}\r\n"));
        writer.Write(Encoding.ASCII.GetBytes($"Content-Length: {fileData.Length}\r\n"));
        writer.Write(Encoding.ASCII.GetBytes("\r\n"));
        writer.Write(fileData);
    }

    private static void SendErrorResponse(BinaryWriter writer, string message)
    {
        writer.Write(Encoding.ASCII.GetBytes("HTTP/1.1 400 Bad Request\r\n"));
        writer.Write(Encoding.ASCII.GetBytes("Content-Type: text/plain\r\n"));
        writer.Write(Encoding.ASCII.GetBytes($"Content-Length: {message.Length}\r\n"));
        writer.Write(Encoding.ASCII.GetBytes("\r\n"));
        writer.Write(Encoding.ASCII.GetBytes(message));
    }

    private static byte[] GenerateHash(byte[] fileData)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(fileData);
        }
    }
}

public class LRUCache<K, V>
{
    private readonly int capacity;
    private readonly LinkedList<K> lruList = new LinkedList<K>();
    private readonly Dictionary<K, V> cache = new Dictionary<K, V>();

    public LRUCache(int capacity)
    {
        this.capacity = capacity;
    }

    public void Add(K key, V value)
    {
        if (cache.ContainsKey(key))
        {
            lruList.Remove(key);
            lruList.AddFirst(key);
            cache[key] = value;
            return;
        }

        if (cache.Count >= capacity)
        {
            var lruKey = lruList.Last.Value;
            lruList.RemoveLast();
            cache.Remove(lruKey);
        }

        cache[key] = value;
        lruList.AddFirst(key);
    }

    public bool TryGet(K key, out V value)
    {
        if (cache.TryGetValue(key, out value))
        {
            lruList.Remove(key);
            lruList.AddFirst(key);
            return true;
        }
        return false;
    }

    public V this[K key]
    {
        get => cache[key];
        set => Add(key, value);
    }
}
