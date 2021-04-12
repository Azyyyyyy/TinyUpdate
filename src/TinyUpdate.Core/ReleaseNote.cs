namespace TinyUpdate.Core
{
    /// <summary>
    /// The note about an <see cref="ReleaseEntry"/>
    /// </summary>
    public class ReleaseNote
    {
        public ReleaseNote(string? content, NoteType type)
        {
            Content = content;
            Type = type;
        }

        /// <summary>
        /// Content to use for rendering the changelog
        /// </summary>
        public string? Content { get; }

        /// <summary>
        /// What technology to use when rendering <see cref="Content"/>
        /// </summary>
        public NoteType Type { get; }
    }
}