using System;

namespace Ambermoon
{
    public enum ExceptionScope
    {
        General,
        Application,
        Data,
        Render
    }

    public class AmbermoonException : Exception
    {
        public ExceptionScope Scope { get; }

        public AmbermoonException(ExceptionScope scope, string message, Exception innerException)
            : base($"[{scope}] {message}", innerException)
        {
            Scope = scope;
        }

        public AmbermoonException(ExceptionScope scope, string message)
            : this(scope, message, null)
        {

        }
    }
}
