using System;

namespace SvgEditor.Core.Shared
{
    internal static class Result
    {
        public static Result<T> Success<T>(T value) => Result<T>.Success(value);
        public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
        public static Result<T> Failure<T>(Exception exception) => Result<T>.Failure(exception);

        public static Result<T> Try<T>(Func<T> buildValue, string errorMessage = null)
        {
            try
            {
                return Success(buildValue());
            }
            catch (Exception exception)
            {
                return Failure<T>(errorMessage ?? exception.Message);
            }
        }
    }

    internal readonly struct Result<T>
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public T Value { get; }
        public string Error { get; }

        private Result(bool isSuccess, T value, string error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public static Result<T> Success(T value) => new(true, value, null);
        public static Result<T> Failure(string error) => new(false, default, error);
        public static Result<T> Failure(Exception exception) => Failure(exception.Message);

        public Result<TNext> Map<TNext>(Func<T, TNext> map)
        {
            return IsSuccess
                ? Result<TNext>.Success(map(Value))
                : Result<TNext>.Failure(Error);
        }

        public Result<TNext> Bind<TNext>(Func<T, Result<TNext>> bind)
        {
            return IsSuccess
                ? bind(Value)
                : Result<TNext>.Failure(Error);
        }

        public Result<T> Ensure(Func<T, bool> predicate, string error)
        {
            if (!IsSuccess)
            {
                return this;
            }

            return predicate(Value)
                ? this
                : Failure(error);
        }

        public T GetValueOrDefault(T defaultValue = default)
        {
            return IsSuccess ? Value : defaultValue;
        }
    }

    internal readonly struct Unit
    {
        public static readonly Unit Default = new();
    }
}
