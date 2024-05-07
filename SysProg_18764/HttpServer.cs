using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace SysProg_18764;

public sealed class HttpServer : IHttpServer
{
    private long requestCount = 0;
    private readonly HttpListener _httpListener = new HttpListener();
    private readonly ConcurrentDictionary<string, byte[]> _cd = new ConcurrentDictionary<string, byte[]>();

    public void AddURI(string uri)
    {
        try
        {
            _httpListener.Prefixes.Add(uri);
        }
        catch (ArgumentNullException e)
        {
            Console.WriteLine("Error message: {0}", e.Message);
        }
        catch (ArgumentException e) 
        {
            Console.WriteLine("Error message: {0}", e.Message);
        }
        catch(ObjectDisposedException e)
        {
            Console.WriteLine("Error message: {0}", e.Message);
        }
        catch(HttpListenerException e)
        {
            Console.WriteLine("Error code: {0}", e.ErrorCode);
            Console.WriteLine("Error message: {0}", e.Message);
        }
    }

    public void ShowRequestBody(HttpListenerRequest request)
    {
        if (request == null)
            throw new ArgumentNullException($"{nameof(request)}", "HttpListenerRequest is null.");

        if (!request.HasEntityBody)
        {
            Console.WriteLine("No client data was sent with the request.\n");
            Console.WriteLine("-----------------------------------------------------------------------------------------------");
            return;
        }
        Stream body = request.InputStream;
        Encoding encoding = request.ContentEncoding;
        StreamReader reader = new StreamReader(body, encoding);

        // Convert the data to a string and display it on the console.
        string s = reader.ReadToEnd();
        Console.WriteLine(s + "\n");
        Console.WriteLine("------------------------------------------------------------------------------------------------------");
        body.Close();
        reader.Close();
    }

    public void ShowRequestData(HttpListenerRequest request)
    {
        try
        {
            ShowRequestHeader(request);
            ShowRequestBody(request);
        }
        catch(ArgumentNullException e)
        {
            Console.WriteLine("{0}", e.Message);
        }
    }

    public void ShowRequestHeader(HttpListenerRequest request)
    {
        if (request == null)
            throw new ArgumentNullException($"{nameof(request)}", "HttpListenerRequest is null.");

        Console.WriteLine("Request {0}", ++requestCount);
        Console.Write("{0} {1} HTTP/{2}",
        request.HttpMethod,
        request.RawUrl,
        request.ProtocolVersion);

        Console.WriteLine();
        Console.WriteLine("Host: {0}", request.UserHostName);
        Console.Write("Accept: ");
        if (request.AcceptTypes != null)
        {
            foreach (var type in request.AcceptTypes)
            {
                Console.Write("{0} ", type);
            }
            Console.WriteLine();
        }

        Console.Write("Accept-language: ");
        if (request.UserLanguages != null)
        {
            foreach (var language in request.UserLanguages)
            {
                Console.Write("{0} ", language);
            }
            Console.WriteLine();
        }

        Console.WriteLine("User-Agent: {0}", request.UserAgent);
        Console.WriteLine("Content-Length: {0}", request.ContentLength64);
        Console.WriteLine();
    }

    public async Task Start()
    {
        string basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Files"));

        if (!Directory.Exists(basePath) || !Directory.EnumerateFileSystemEntries(basePath).Any())
        {
            Console.WriteLine("Direktorijum ne postoji ili je prazan.");
            return;
        }

        AddURI("http://localhost:8080/");

        try
        {
            _httpListener.Start();
            Console.WriteLine("Listening...\n");
        }
        catch(HttpListenerException e)
        {
            Console.WriteLine("Error code: {0}", e.ErrorCode);
            Console.WriteLine("Error message: {0}", e.Message);
        }
        catch(ObjectDisposedException e)
        {
            Console.WriteLine("Error message: {0}", e.Message);
        }

        basePath += "\\";

        while (true)
        {
            HttpListenerContext context = await _httpListener.GetContextAsync();
            await Task.Run(() => HandleRequest(context, basePath));
        }
    }

    public void Stop()
    {
        _httpListener?.Stop();
    }

    public void HandleRequest(HttpListenerContext context, string basePath)
    {
        ThreadPool.QueueUserWorkItem((state) =>
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            ShowRequestData(request);
            try
            {
                string filename = request.RawUrl!.Substring(1);

                string filepath = Path.Combine(basePath, filename);
                if (File.Exists(filepath))
                {
                    byte[] encryptedBytes = EncryptFile(filepath);

                    response.StatusCode = (int)HttpStatusCode.OK;
                    response.ContentType = "application/octet-stream";
                    response.ContentLength64 = encryptedBytes.Length;
                    response.OutputStream.Write(encryptedBytes, 0, encryptedBytes.Length);
                    response.OutputStream.Close();
                    Console.WriteLine($"{filename} encrypted.");
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.OutputStream.Close();
                    Console.WriteLine($"{filename} doesn't exist.");
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.OutputStream.Close();
                Console.WriteLine($"Error while processing request: {ex.Message}");
            }
            finally
            {
                response.Close();
            }
        });
    }

    public byte[] EncryptFile(string filepath)
    {

        byte[] fileBytes = File.ReadAllBytes(filepath);

        byte[] encryptedBytes;
        using (SHA256 sha256 = SHA256.Create())
        {
            encryptedBytes = sha256.ComputeHash(fileBytes);
        }

        return encryptedBytes;
    }
}