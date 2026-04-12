using Puma.MDE.OPUS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace Puma.MDE.OPUS.Utilities
{
    internal static class OpusMessageTrailContext
    {
        private sealed class Scope : IDisposable
        {
            private readonly Action _onDispose;
            private bool _disposed;

            public Scope(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _onDispose?.Invoke();
            }
        }

        private static readonly AsyncLocal<List<string>> _completedEndpointMessages = new AsyncLocal<List<string>>();

        public static IDisposable BeginScope()
        {
            List<string> previous = _completedEndpointMessages.Value;
            _completedEndpointMessages.Value = new List<string>();
            return new Scope(() => _completedEndpointMessages.Value = previous);
        }

        public static void AddCompletedEndpointSuccess(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (_completedEndpointMessages.Value == null)
            {
                _completedEndpointMessages.Value = new List<string>();
            }

            string clean = message.Trim();
            List<string> messages = _completedEndpointMessages.Value;

            if (messages.Count > 0 && string.Equals(messages[messages.Count - 1], clean, StringComparison.Ordinal))
            {
                return;
            }

            messages.Add(clean);
        }

        public static List<string> SnapshotCompletedEndpointMessages()
        {
            return _completedEndpointMessages.Value == null
                ? new List<string>()
                : new List<string>(_completedEndpointMessages.Value);
        }

        public static void PrefixCompletedBeforeTrail(OpusOperationResult result)
        {
            if (result == null)
            {
                return;
            }

            List<string> completed = SnapshotCompletedEndpointMessages();
            if (completed.Count == 0)
            {
                return;
            }

            List<string> existingTrail = result.FriendlyMessages ?? new List<string>();
            List<string> reordered = new List<string>();

            reordered.AddRange(completed);
            reordered.AddRange(existingTrail);

            result.FriendlyMessages = reordered;
            result.FriendlyMessage = reordered.FirstOrDefault() ?? result.FriendlyMessage;
        }
    }
}
