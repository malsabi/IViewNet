using System;
namespace IViewNet.Common.Models
{
    public class StartListenerResult
    {
        public string Message { get; set; }
        public string Type { get; set; }
        public DateTime TimeStamp { get; set; }
        public bool IsOperationSuccess { get; set; }

        public StartListenerResult(string Message, string Type, DateTime TimeStamp, bool IsOperationSuccess)
        {
            this.Message = Message;
            this.Type = Type;
            this.TimeStamp = TimeStamp;
            this.IsOperationSuccess = IsOperationSuccess;
        }

        public override string ToString()
        {
            return string.Format("[{0}\t{1}]\t\t{2}", TimeStamp.ToString("dd-MM-yyyy HH:mm:ss.ffff"), Type.ToUpper(), Message);
        }
    }
}