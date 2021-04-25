namespace TinyUpdate.Core
{
    /// <summary>
    /// What rendering technology is being used to parse the <see cref="ReleaseNote.Content"/> into something that the user will see
    /// </summary>
    public enum NoteType
    {
        /// <summary>
        /// We are using Markdown for rendering
        /// </summary>
        Markdown,

        /// <summary>
        /// We are using HTML for rendering
        /// </summary>
        Html,

        /// <summary>
        /// We are using plain text for rendering
        /// </summary>
        Plain,

        /// <summary>
        /// We don't know what is being used for rendering
        /// </summary>
        Other
    }
}