using SysProg_18764;

HttpServer server = new HttpServer();
server.AddURI("http://localhost:8080/");
server.Start();