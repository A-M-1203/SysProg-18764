using System.Net;

namespace SysProg_18764;

public interface IHttpServer
{
    Task Start();
    void AddURI(string uri);
    void HandleRequest(HttpListenerContext context, string basePath);
    byte[] EncryptFile(string file);
    void ShowRequestData(HttpListenerRequest request);
    void ShowRequestHeader(HttpListenerRequest request);
    void ShowRequestBody(HttpListenerRequest request);
    void Stop();
}