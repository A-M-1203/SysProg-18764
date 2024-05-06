using System.Net;
using System.Text;

namespace SysProg_18764;

public sealed class HttpServer : IHttpServer
{
    private long requestCount = 0;
    private readonly HttpListener _httpListener = new HttpListener();
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

    public void Start()
    {
        string basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Files"));

        if (!Directory.Exists(basePath) || !Directory.EnumerateFileSystemEntries(basePath).Any())
        {
            Console.WriteLine("Direktorijum ne postoji ili je prazan.");
            return;
        }

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

        while (true)
        {
            // GetContext method blocks while waiting for a request.
            HttpListenerContext context = _httpListener.GetContext();
            HttpListenerRequest request = context.Request;

            ShowRequestData(request);

            string? rawUrl = request.RawUrl;
            string[] filePaths;
            if (rawUrl != null)
            {
                int index = rawUrl.IndexOf('/');
                if (index >= 0)
                {
                    rawUrl = rawUrl.Substring(0, index) + rawUrl.Substring(index + 1);
                }
                filePaths = rawUrl.Split('/');

                for (int i = 0; i < filePaths.Length; i++)
                {
                    Console.WriteLine(filePaths[i]);
                    filePaths[i] = "\\" + filePaths[i];
                    Console.WriteLine(filePaths[i]);

                    if (File.Exists(basePath + filePaths[i]))
                    {
                        Console.WriteLine($"File {basePath + filePaths[i]} exists in the directory. Caching it for further requests.\n");
                    }
                    else
                    {
                        Console.WriteLine($"File {basePath + filePaths[i]} does not exist in the directory.\n");
                    }
                }
            }
        }
    }

    public void Stop()
    {
        _httpListener?.Stop();
    }
}