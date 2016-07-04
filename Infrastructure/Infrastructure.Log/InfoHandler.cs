
namespace Infrastructure.Log
{
    public class InfoHandler
    {
        public static void Info(string message)
        {
            LogManager.Instance().Info(message);
            
        }        

    }
}
