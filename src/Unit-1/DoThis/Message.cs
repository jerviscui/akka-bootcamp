namespace WinTail;

public class Message
{
    #region Neutral/system messages

    public class ContinueProcessing
    {
    }

    #endregion

    #region Success messages

    public class InputSuccess
    {
        public string Region { get; private set; }

        public InputSuccess(string region)
        {
            Region = region;
        }
    }

    #endregion

    #region Error messages

    public abstract class InputError
    {
        protected InputError(string error)
        {
            Error = error;
        }

        public string Error { get; private set; }
    }

    public class NullInputError : InputError
    {
        /// <inheritdoc />
        public NullInputError(string error) : base(error)
        {
        }
    }

    public class ValidationError : InputError
    {
        /// <inheritdoc />
        public ValidationError(string error) : base(error)
        {
        }
    }

    #endregion
}
