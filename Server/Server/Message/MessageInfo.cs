
namespace Server
{
    public class MessageInfo
    {
        MessageBuffer mMessageBuffer;
        Session mSession;

        public MessageBuffer Buffer { get { return mMessageBuffer; } }
        public Session Session { get { return mSession; } }

        public MessageInfo(MessageBuffer message, Session client)
        {
            mSession = client;
            mMessageBuffer = message;
        }
    }
}
