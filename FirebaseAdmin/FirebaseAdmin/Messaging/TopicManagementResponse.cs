using System.Collections.Generic;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// The Topic Management Response.
    /// </summary>
    public class TopicManagementResponse
    {
        private static readonly string UNKNOWNERROR = "unknown-error";

        // Server error codes as defined in https://developers.google.com/instance-id/reference/server
        // TODO: Should we handle other error codes here (e.g. PERMISSION_DENIED)?
        private static readonly IDictionary<string, string> ERRORCODES = new Dictionary<string, string>
        {
            { "INVALID_ARGUMENT", "invalid-argument" },
            { "NOT_FOUND", "registration-token-not-registered" },
            { "INTERNAL", "internal-error" },
            { "TOO_MANY_TOPICS", "too-many-topics" },
        };

        private readonly int successCount;
        private readonly IList<Error> errors;

        /// <summary>
        /// Initializes a new instance of the <see cref="TopicManagementResponse"/> class.
        /// </summary>
        /// <param name="results">The results from server.</param>
        public TopicManagementResponse(IList<IDictionary<string, object>> results)
        {
            this.errors = new List<Error>();
            int successCounts = 0;
            for (int i = 0; i < results.Count; i++)
            {
                IDictionary<string, object> result = results[i];

                if (result.Count == 0)
                {
                    successCounts++;
                }
                else
                {
                    this.errors.Add(new Error(i, (string)result["error"]));
                }
            }

            this.successCount = successCounts;
        }

        /// <summary>
        /// Gets the number of registration tokens that were successfully subscribed or unsubscribed.
        /// </summary>
        /// <returns>The success count.</returns>
        public int GetSuccessCount()
        {
            return this.successCount;
        }

        /// <summary>
        /// Gets the number of registration tokens that could not be subscribed or unsubscribed, and
        /// resulted in an error.
        /// </summary>
        /// <returns>The error count.</returns>
        public int GetFailureCount()
        {
            return this.errors.Count;
        }

        /// <summary>
        /// Gets a list of errors encountered while executing the topic management operation.
        /// </summary>
        /// <returns>A <see cref="IList{Error}"/> errors.</returns>
        public IList<Error> GetErrors()
        {
            return this.errors;
        }

        /// <summary>
        /// A topic management error.
        /// </summary>
        public class Error
        {
            private readonly int index;
            private readonly string reason;

            /// <summary>
            /// Initializes a new instance of the <see cref="Error"/> class.
            /// </summary>
            /// <param name="index">The index.</param>
            /// <param name="reason">The reason.</param>
            public Error(int index, string reason)
            {
                this.index = index;
                this.reason = ERRORCODES.ContainsKey(reason)
                  ? ERRORCODES[reason] : UNKNOWNERROR;
            }

            /// <summary>
            /// Index of the registration token to which this error is related to.
            /// </summary>
            /// <returns>The index.</returns>
            public int GetIndex()
            {
                return this.index;
            }

            /// <summary>
            /// String describing the nature of the error.
            /// </summary>
            /// <returns>The reason.</returns>
            public string GetReason()
            {
                return this.reason;
            }
        }
    }
}
