using System;

namespace Infrastructure.Log
{
    public class ErrorHandler
    {

        public static Exception Error(Exception ex)
        {
            LogManager.Instance().Error(ex);
            return ex;
        }


    }
}
