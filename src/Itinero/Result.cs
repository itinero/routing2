using System;

namespace Itinero
{
    /// <summary>
    /// Represents a result of some calculation and associated status information.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Result<T>
    {
        private readonly T _value;
        private readonly Func<string, Exception>? _createException;

        /// <summary>
        /// Creates a new result.
        /// </summary>
        public Result(T result)
        {
            _value = result;
            ErrorMessage = string.Empty;
            IsError = false;
        }

        /// <summary>
        /// Creates a new result.
        /// </summary>
        public Result(string errorMessage)
            : this(errorMessage, (m) => new Exception(m)) { }

        /// <summary>
        /// Creates a new result.
        /// </summary>
        public Result(string errorMessage, Func<string, Exception>? createException)
        {
            _value = default;
            _createException = createException;
            ErrorMessage = errorMessage;
            IsError = true;
        }

        /// <summary>
        /// Gets the result.
        /// </summary>
        public T Value {
            get {
                if (IsError &&
                    _createException != null) {
                    throw _createException(ErrorMessage);
                }

                return _value;
            }
        }

        /// <summary>
        /// Implicit conversion to a boolean indication success or fail.
        /// </summary>
        /// <param name="result">The result object.</param>
        /// <returns>The success or fail boolean.</returns>
        public static implicit operator bool(Result<T> result)
        {
            return !result.IsError;
        }

        /// <summary>
        /// Implicit conversion to the result object type.
        /// </summary>
        /// <param name="result">The result object.</param>
        /// <returns>The result object type.</returns>
        public static implicit operator T(Result<T> result)
        {
            return result.Value;
        }
        
        /// <summary>
        /// Implicit conversion from the result object type.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>The result object.</returns>
        public static implicit operator Result<T>(T result)
        {
            return new(result);
        }

        /// <summary>
        /// Gets the status.
        /// </summary>
        public bool IsError { get; private set; }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Converts this result, when an error to an result of another type.
        /// </summary>
        /// <returns></returns>
        public Result<TNew> ConvertError<TNew>()
        {
            if (!IsError) {
                throw new Exception("Cannot convert a result that represents more than an error.");
            }

            return new Result<TNew>(ErrorMessage, _createException);
        }

        /// <summary>
        /// Returns a description.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (IsError) {
                return $"Result<{nameof(T)}>: {ErrorMessage}";
            }

            if (Value == null) {
                return $"Result<{nameof(T)}>: null";
            }

            return $"Result<{nameof(T)}>: {Value.ToString()}";
        }
    }
}