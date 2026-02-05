namespace ip_connect.Common
{
    public class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public Dictionary<string, string[]>? Errors { get; set; }

        public static ErrorResponse Create(string message)
        {
            return new ErrorResponse { Error = message };
        }

        public static ErrorResponse CreateValidationError(Dictionary<string, string[]> errors)
        {
            return new ErrorResponse 
            { 
                Error = "Validation failed",
                Errors = errors 
            };
        }
    }
}