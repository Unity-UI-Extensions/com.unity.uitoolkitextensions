/// Credit SimonDarksideJ

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A single social link: a platform identifier and its URL/handle. Returned by
    /// <see cref="SocialLinkContainer.GetSocials"/> to report the links a user has entered.
    /// </summary>
    [System.Serializable]
    public class SocialLink
    {
        public string platform; // platform identifier, e.g. "Instagram", "Twitter"
        public string url;      // profile URL or handle
    }
}