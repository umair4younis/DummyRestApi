using Example.OPUS.Exceptions;
using System;
using System.Threading.Tasks;


namespace Example.Tests
{
    public class UnreliableService
    {
        private readonly Random _random = new Random();
        private int _callCount = 0;

        public async Task<string> CallAsync(bool forceFail = false)
        {
            _callCount++;

            await Task.Delay(100 + _random.Next(400));

            if (forceFail || _callCount % 3 == 0)
            {
                throw HttpStatusException.ServiceUnavailable($"Simulated failure on call #{_callCount} (503)");
            }

            return $"Success on call #{_callCount}";
        }
    }
}
