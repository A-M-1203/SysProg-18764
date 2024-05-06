using System.Net;

namespace SysProg_18764;

public interface IHttpServer
{
    void Start();
    void AddURI(string uri);
    void ShowRequestData(HttpListenerRequest request);
    void ShowRequestHeader(HttpListenerRequest request);
    void ShowRequestBody(HttpListenerRequest request);
    void Stop();
}