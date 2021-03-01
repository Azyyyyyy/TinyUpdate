namespace TinyUpdate.Core
{
    /// <summary>
    /// The note about an <see cref="ReleaseEntry"/>
    /// </summary>
    public class ReleaseNote
    {
        public ReleaseNote(string? content, ReleaseNote type)
        {
            Content = content;
            Type = type;
        }

        /// <summary>
        /// Content to use for rendering a changelog
        /// </summary>
        public string? Content { get; }

        /// <summary>
        /// What is going to be used when rendering <see cref="Content"/> to the user
        /// </summary>
        public ReleaseNote Type { get; }
    }
}