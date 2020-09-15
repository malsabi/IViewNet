using System;
namespace IViewNet.Common.Models
{
    public class ShutdownResult
    {
        public string Message { get; set; }
        public string Type { get; set; }
        public DateTime TimeStamp { get; set; }
        public bool IsOperationSuccess { get; set; }

        public ShutdownResult(string Message, string Type, DateTime TimeStamp, bool IsOperationSuccess)
        {
            this.Message = Message;
            this.Type = Type;
            this.TimeStamp = TimeStamp;
            this.IsOperationSuccess = IsOperationSuccess;
        }
    }
}