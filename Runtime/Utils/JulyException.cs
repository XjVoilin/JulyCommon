using System;

namespace JulyCommon
{
    /// <summary>
    /// 框架自定义异常类
    /// </summary>
    public class JulyException : Exception
    {
        /// <summary>
        /// 错误码
        /// </summary>
        public FrameworkErrorCode ErrorCode { get; }

        /// <summary>
        /// 创建无参异常
        /// </summary>
        public JulyException()
        {
            ErrorCode = FrameworkErrorCode.Unknown;
        }

        /// <summary>
        /// 创建带消息的异常
        /// </summary>
        public JulyException(string message) : base(message)
        {
            ErrorCode = FrameworkErrorCode.Unknown;
        }

        /// <summary>
        /// 创建带消息和内部异常的异常
        /// </summary>
        public JulyException(string message, Exception innerException) : base(message, innerException)
        {
            ErrorCode = FrameworkErrorCode.Unknown;
        }

        /// <summary>
        /// 创建带错误码的异常
        /// </summary>
        public JulyException(FrameworkErrorCode errorCode, string message = null)
            : base(message ?? errorCode.ToDefaultMessage())
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// 创建带错误码和内部异常的异常
        /// </summary>
        public JulyException(FrameworkErrorCode errorCode, string message, Exception innerException)
            : base(message ?? errorCode.ToDefaultMessage(), innerException)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// 从 FrameworkResult 创建异常
        /// </summary>
        public static JulyException FromResult(FrameworkResult result)
        {
            return new JulyException(result.ErrorCode, result.Message, result.Exception);
        }

        /// <summary>
        /// 从 FrameworkResult&lt;T&gt; 创建异常
        /// </summary>
        public static JulyException FromResult<T>(FrameworkResult<T> result)
        {
            return new JulyException(result.ErrorCode, result.Message, result.Exception);
        }

        public override string ToString()
        {
            return $"[JulyException] Code: {ErrorCode} ({(int)ErrorCode}), Message: {Message}";
        }
    }
}
