namespace Amber
{
    public enum ExceptionScope
    {
        Application,
        Data,
        Render
    }

    public class AmberException : Exception
    {
        public ExceptionScope Scope { get; }

        public AmberException(ExceptionScope scope, string message, Exception? innerException)
            : base($"[{scope}] {message}", innerException)
        {
            Scope = scope;
        }

        public AmberException(ExceptionScope scope, string message)
            : this(scope, message, null)
        {

        }
    }
}
