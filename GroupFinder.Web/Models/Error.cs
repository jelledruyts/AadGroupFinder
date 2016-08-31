using System.Collections.Generic;

namespace GroupFinder.Web.Models
{
    public class Error
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string Target { get; set; }
        public IList<Error> Details { get; set; }

        public Error(string code, string message)
        {
            this.Code = code;
            this.Message = message;
        }
    }
}