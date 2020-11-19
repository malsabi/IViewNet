# IViewNet

### About
An advanced library used for interprocess communication which provides a Server/Client (Sockets) and Process Pipelining.

### Creating a Server Listener
NetConfig is the network configuration for server/client which contains all of the needed properties to set up the server or client.

If you want to use the default configuration for server call the method 
```csharp
NetConfig.CreateDefaultServerConfig();
```
If you want to use the default configuration for client call the method
```csharp
NetConfig.CreateDefaultClientConfig();
```
Setting the server
```csharp
NetConfig ServerConfig = NetConfig.CreateDefaultServerConfig();
Server MyServer = new Server(ServerConfig);
MyServer.OnClientConnect += ParentServer_OnClientConnect;
MyServer.OnClientSend += ParentServer_OnClientSend;
MyServer.OnClientReceive += ParentServer_OnClientReceive;
MyServer.OnClientDisconnect += ParentServer_OnClientDisconnect;
MyServer.OnClientException += ParentServer_OnClientException;
MyServer.OnClientBlackList += ParentServer_OnClientBlackList;
MyServer.PacketManager = CreateCommands();
StartListenerResult ListenerResult = MyServer.StartListener();
if (ListenerResult.IsOperationSuccess)
{
    StartAcceptorResult AcceptorResult = MyServer.StartAcceptor();
    if (AcceptorResult.IsOperationSuccess)
    {
        IsParentRunning = true;
        SetOnParentCreated(string.Format("{0}\n{1}", ListenerResult.Message, AcceptorResult.Message));
    }
    else
    {
        SetOnException(AcceptorResult.Message);
    }
}
else
{
    SetOnException(ListenerResult.Message);
}

