/*
 * ErrorViewModel.cs
 * Model used by the default Error page in LuxeGroom.
 * Holds the request ID for debugging and determines whether to display it.
 */

namespace LuxeGroom.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        // Returns true if there is a request ID to display
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
