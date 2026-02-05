namespace DocProcessor.Core.Interfaces;

public interface ICallbackService
{
    Task SendCallbackAsync(string callbackUrl, string requestId, string result);
}
