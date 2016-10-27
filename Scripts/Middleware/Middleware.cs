public interface IMiddleware {
    void SendMessage(string data);
    string ReadMessage();
    void Close();
}