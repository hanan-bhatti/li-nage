using System;
using System.Collections.Generic;

namespace Linage.Core.Results
{
    /// <summary>
    /// Enterprise-grade Result pattern for better error handling and outcome tracking
    /// Eliminates exception-based control flow
    /// </summary>
    /// <summary>
    /// Enterprise-grade Result pattern for better error handling and outcome tracking
    /// Eliminates exception-based control flow
    /// </summary>
    public abstract class Result
    {
        public bool IsSuccess { get; protected set; }
        public bool IsFailure => !IsSuccess;
        public string Message { get; protected set; } = string.Empty;
        public Exception Exception { get; protected set; }

        public static Success Ok() => new Success();
        public static Success<T> Ok<T>(T value) => new Success<T>(value);
        public static Failure Fail(string message, Exception ex = null) => new Failure(message, ex);
        public static Failure<T> Fail<T>(string message, Exception ex = null) => new Failure<T>(message, ex);
    }

    /// <summary>
    /// Generic Result type
    /// </summary>
    public abstract class Result<T> : Result
    {
        public T Value { get; protected set; }
    }

    /// <summary>
    /// Successful operation result
    /// </summary>
    public class Success : Result
    {
        public Success()
        {
            IsSuccess = true;
            Message = "Operation completed successfully";
        }

        public Success(string message)
        {
            IsSuccess = true;
            Message = message;
        }
    }

    /// <summary>
    /// Successful operation with value
    /// </summary>
    public class Success<T> : Result<T>
    {
        public Success(T value)
        {
            IsSuccess = true;
            Value = value;
            Message = "Operation completed successfully";
        }

        public Success(T value, string message)
        {
            IsSuccess = true;
            Value = value;
            Message = message;
        }
    }

    /// <summary>
    /// Failed operation
    /// </summary>
    public class Failure : Result
    {
        public List<string> Errors { get; } = new List<string>();

        public Failure(string message, Exception ex = null)
        {
            IsSuccess = false;
            Message = message;
            Exception = ex;
            if (!string.IsNullOrWhiteSpace(message))
                Errors.Add(message);
        }

        public Failure AddError(string error)
        {
            Errors.Add(error);
            return this;
        }
    }

    /// <summary>
    /// Failed operation with typed value (for return type consistency)
    /// </summary>
    public class Failure<T> : Result<T>
    {
        public List<string> Errors { get; } = new List<string>();

        public Failure(string message, Exception ex = null)
        {
            IsSuccess = false;
            Message = message;
            Exception = ex;
            if (!string.IsNullOrWhiteSpace(message))
                Errors.Add(message);
        }

        public Failure<T> AddError(string error)
        {
            Errors.Add(error);
            return this;
        }
    }

    /// <summary>
    /// Extension methods for Result pattern
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>
        /// Maps success result to another value
        /// </summary>
        public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> mapper)
        {
            if (result.IsFailure)
                return Result.Fail<TOut>(result.Message, result.Exception);

            try
            {
                return Result.Ok(mapper(result.Value));
            }
            catch (Exception ex)
            {
                return Result.Fail<TOut>($"Mapping operation failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Chains result operations
        /// </summary>
        public static Result<TOut> Chain<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> chain)
        {
            if (result.IsFailure)
                return Result.Fail<TOut>(result.Message, result.Exception);

            return chain(result.Value);
        }

        /// <summary>
        /// Executes action on success
        /// </summary>
        public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> action)
        {
            if (result.IsSuccess)
                action(result.Value);
            return result;
        }

        /// <summary>
        /// Executes action on failure
        /// </summary>
        public static Result<T> OnFailure<T>(this Result<T> result, Action<string> action)
        {
            if (result.IsFailure)
                action(result.Message);
            return result;
        }
    }
}
