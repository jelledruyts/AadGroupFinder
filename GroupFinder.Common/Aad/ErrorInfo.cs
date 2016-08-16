using System.Diagnostics;

namespace GroupFinder.Common.Aad
{
    [DebuggerDisplay("Error: {Code}")]
    internal class ErrorInfo
    {
        public string Code { get; set; }
        public ErrorMessage Message { get; set; }
    }
}