namespace IViewNet.Common.Models
{
    public class EstablishConnectionResult
    {
        public bool IsOperationSuccess { get; set; }
        public string Message { get; set; }

        public EstablishConnectionResult(bool IsOperationSuccess, string Message)
        {
            this.IsOperationSuccess = IsOperationSuccess;
            this.Message = Message;
        }
    }
}