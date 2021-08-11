namespace SubscriptionBundlePopups.Payloads
{
    /// <summary>
    /// For pass click action to client package (like book2life) when subscription info clicked.
    /// </summary>
    public class SubscriptionInfoClickedPayload
    {
        public readonly string ButtonUrl;

        public SubscriptionInfoClickedPayload(string buttonUrl)
        {
            ButtonUrl = buttonUrl;
        }
    }
}