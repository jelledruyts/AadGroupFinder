namespace GroupFinder.Web.Models
{
    // See https://github.com/Microsoft/api-guidelines/blob/master/Guidelines.md#710-response-formats.
    public class ErrorResponse
    {
        public Error Error { get; set; }

        public ErrorResponse(Error error)
        {
            this.Error = error;
        }
    }
}