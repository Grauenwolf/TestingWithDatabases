using System;
using System.Text;

namespace TestingWithDatabases
{
	public class VerificationStep
	{
		public VerificationStep(string? checkType, string message, Severity severity)
		{
			Message = message;
			CheckType = checkType;
			Severity = severity;
			if (severity != Severity.Message)
				StackTrace = Environment.StackTrace;
		}

		public Severity Severity { get; }
		public string? StackTrace { get; }
		public string? CheckType { get; }
		public string Message { get; }

		public override string ToString()
		{
			var result = new StringBuilder();

			if (!string.IsNullOrWhiteSpace(CheckType))
				result.AppendLine(CheckType);
			if (!string.IsNullOrWhiteSpace(Message))
				result.AppendLine(Message);
			if (!string.IsNullOrWhiteSpace(StackTrace))
				result.AppendLine(StackTrace);

			return result.ToString();
		}
	}
}
