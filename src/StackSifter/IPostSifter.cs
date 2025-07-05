namespace StackSifter;

public interface IPostSifter
{
    /// <summary>
    /// Returns true if the post matches the configured criteria.
    /// </summary>
    bool IsMatch(Feed.Post post);
}
